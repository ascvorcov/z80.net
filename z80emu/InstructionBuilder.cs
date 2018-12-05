namespace z80emu
{
    using System;
    using word = System.UInt16;

    delegate State Handler(Memory memory);

    [Flags]
    public enum BlockMode
    {
        None = 0,
        Increment = 1,
        Decrement = 0,
        Once = 2,
        Repeat = 0,

        IO = Increment|Once,
        IR = Increment|Repeat,
        DO = Decrement|Once,
        DR = Decrement|Repeat
    }

    [Flags]
    enum State
    {
        Next = 0,
        NextSlow,
        Repeat,
        RepeatSlow,
    }

    interface IInstruction
    {
        State Execute(Memory m);
    }

    class InstructionBuilderComposite : IInstruction
    {
        private byte offset;
        private readonly WordRegister pc;
        private readonly IInstruction[] inner;
        private readonly IInstruction fallback;

        public InstructionBuilderComposite(byte offset, WordRegister pc, IInstruction[] inner, IInstruction fallback = null)
        {
            this.offset = offset;
            this.pc = pc;
            this.inner = inner;
            this.fallback = fallback;
        }

        public State Execute(Memory m)
        {
            var instruction = m.ReadByte((word)(pc.Value + this.offset));
            var op = this.inner[instruction] ?? fallback;
            return op.Execute(m);
        }
    }

    class InstructionBuilder : IInstruction
    {
        private byte timeFast;
        private byte timeSlow;
        private byte size;
        private string label;
        private Handler handler;

        private readonly WordRegister PC;
        private readonly FlagsRegister Flags;
        private readonly ByteRegister R;
        private readonly IClock clock;
        private readonly CPU cpu;

        public InstructionBuilder(CPU cpu, WordRegister pc, FlagsRegister flags, ByteRegister r, IClock clock)
        {
            this.cpu = cpu;
            this.PC = pc;
            this.Flags = flags;
            this.R = r;
            this.clock = clock;
        }

        public State Execute(Memory mem)
        {
            var state = this.handler.Invoke(mem);
            var rDelta = this.size == 1 ? 1 : 2;
            this.R.Value = (byte)((this.R.Value + rDelta) & 0x7F); // increment only 7 bits
            switch (state)
            {
                case State.Next:
                    this.clock.Tick(this.timeFast);
                    this.PC.Value += this.size;
                    break;
                case State.NextSlow:
                    this.clock.Tick(this.timeSlow);
                    this.PC.Value += this.size;
                    break;
                case State.Repeat:
                    this.clock.Tick(this.timeFast);
                    break;
                case State.RepeatSlow:
                    this.clock.Tick(this.timeSlow);
                    break;
            }

            return state;
        }

        // number of t-states required to execute instruction, fast and slow path
        public InstructionBuilder Time(byte slow, byte fast = 0)
        {
            this.timeFast = fast == 0 ? slow : fast;
            this.timeSlow = slow;
            return this;
        }

        // instruction size, in bytes
        public InstructionBuilder Size(byte sz)
        {
            this.size = sz;
            return this;
        }

        public InstructionBuilder Handler(Handler h)
        {
            this.handler = h;
            return this;
        }

        public InstructionBuilder Label(string lbl)
        {
            this.label = lbl;
            return this;
        }

        public InstructionBuilder Nop()
        {
            this.handler = m => 
            {
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder Halt(CPU cpu)
        {
            this.handler = m => 
            {
                cpu.Halted = true;
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder Load<T>(IPointerReference<T> dst, IReference<T> src)
        {
            this.handler = m =>
            {
                dst.Get().Write(m, src.Read(m));
                return State.Next;
            };

            return this;
        }

        public InstructionBuilder Load<T>(IReference<T> dst, IPointerReference<T> src)
        {
            this.handler = m =>
            {
                // 10 t-states
                dst.Write(m, src.Get().Read(m));// flags not affected
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder Load<T>(IReference<T> dst, IReference<T> src)
        {
            this.handler = m =>
            {
                // 7 t-states
                dst.Write(m, src.Read(m));
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder LoadIR(IReference<byte> dst, IReference<byte> src, CPU cpu)
        {
            // special flags-affecting extended version of load used by I and R registers;
            this.handler = m =>
            {
                var v = src.Read(m);
                var f = Flags;
                f.Sign = v > 0x7F;
                f.Zero = v == 0;
                f.HalfCarry = false;
                f.ParityOverflow = cpu.IFF2;
                f.AddSub = false;
                f.SetUndocumentedFlags(v);
                dst.Write(m, v);
                return 0;
            }; 
            return this;

        }

        public InstructionBuilder BlockLoad(ByteRegister a, WordRegister de, WordRegister hl, WordRegister counter, BlockMode mode)
        {
            this.handler = m =>
            {
                var data = m.ReadByte(hl.Value);
                m.WriteByte(de.Value, data);

                counter.Decrement();
                if (mode.HasFlag(BlockMode.Increment))
                {
                    hl.Increment();
                    de.Increment();
                }
                else
                {
                    hl.Decrement();
                    de.Decrement();
                }

                var n = (byte)(a.Value + data);
                Flags.HalfCarry = false;
                Flags.ParityOverflow = counter.Value != 0;
                Flags.AddSub = false;
                Flags.Flag3 = (n & 0b1000) != 0; // bit 3
                Flags.Flag5 = (n & 0b0010) != 0; // bit 1!

                if (mode.HasFlag(BlockMode.Once))
                    return State.Next;
                if (!Flags.ParityOverflow) 
                    return State.Next;
                return State.RepeatSlow;
            }; 
            return this;
        }

        public InstructionBuilder BlockCompare(ByteRegister a, WordRegister hl, WordRegister counter, BlockMode mode)
        {
            this.handler = m =>
            {
                var v1 = a.Value;
                var v2 = m.ReadByte(hl.Value);
                counter.Decrement();
                if (mode.HasFlag(BlockMode.Increment))
                    hl.Increment();
                else
                    hl.Decrement();

                var f = Flags;
                byte res = (byte)(v1 - v2);
                bool h = IsHalfBorrow(v1, v2);
                var n = (byte)(v1 - v2 - (h ? 1 : 0));

                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = h;
                f.ParityOverflow = counter.Value != 0;
                f.AddSub = true;
                f.Flag3 = (n & 0b1000) != 0; // bit 3
                f.Flag5 = (n & 0b0010) != 0; // bit 1!

                if (mode.HasFlag(BlockMode.Once))
                    return State.Next;
                if (counter.Value == 0 || v1 == v2)
                    return State.Next;
                return State.RepeatSlow;
            }; return this;
        }

        public InstructionBuilder BlockOutput(Port port, WordRegister hl, WordRegister bc, BlockMode mode)
        {
            this.handler = m =>
            {
                var B = bc.High;
                var C = bc.Low;
                var device = port.Get(C.Value);
                var v = m.ReadByte(hl.Value);
                device.Write(B.Value, v);
                
                if (mode.HasFlag(BlockMode.Increment))
                    hl.Increment();
                else
                    hl.Decrement();
                B.Decrement();

                var f = Flags;
                f.Zero = B.Value == 0;

                // undocumented
                var k = hl.Low.Value + v;
                f.Carry = k > 255;
                f.HalfCarry = k > 255;
                f.Sign = (B.Value & 0x80) != 0; 
                f.Parity = EvenParity((byte)((k & 7) ^ B.Value));
                f.AddSub = (v & 0x80) != 0;
                f.SetUndocumentedFlags(B.Value);

                if (mode.HasFlag(BlockMode.Once))
                    return State.Next;
                if (f.Zero) 
                    return State.Next;
                return State.RepeatSlow;
            };
            return this;
        }

        public InstructionBuilder BlockInput(Port port, WordRegister hl, WordRegister bc, BlockMode mode)
        {
            this.handler = m =>
            {
                var B = bc.High;
                var C = bc.Low;
                var device = port.Get(C.Value);
                var v = device.Read(B.Value);
                m.WriteByte(hl.Value, v);
                bool increment = mode.HasFlag(BlockMode.Increment);
                if (increment)
                    hl.Increment();
                else
                    hl.Decrement();
                B.Decrement();

                var f = Flags;
                f.Zero = B.Value == 0;
                f.AddSub = true;

                // undocumented
                var k = (increment ? C.Value + 1 : C.Value - 1) + v;
                f.Carry = k > 255;
                f.HalfCarry = k > 255;
                f.Sign = (B.Value & 0x80) != 0; 
                f.Parity = EvenParity((byte)((k & 7) ^ B.Value));
                f.SetUndocumentedFlags(B.Value);

                if (mode.HasFlag(BlockMode.Once))
                    return State.Next;
                if (f.Zero) 
                    return State.Next;
                return State.RepeatSlow;
            };
            return this;
        }

        public InstructionBuilder RotateDigitRight(ByteRegister regA, WordRegister hl)
        {
            this.handler = m =>
            {
                byte h = m.ReadByte(hl.Value);
                byte a = regA.Value;

                byte loh = (byte)(h & 0x0F);
                byte loa = (byte)(a & 0x0F);
                byte hia = (byte)(a & 0xF0);

                a = (byte)(hia | loh);
                h = (byte)((h >> 4) | (loa << 4));
                
                var f = Flags;
                f.Sign = a > 0x7F;
                f.Zero = a == 0;
                f.HalfCarry = false;
                f.ParityOverflow = EvenParity(a);
                f.AddSub = false;
                f.SetUndocumentedFlags(a);

                regA.Value = a;
                m.WriteByte(hl.Value, h);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder RotateDigitLeft(ByteRegister regA, WordRegister hl)
        {
            this.handler = m =>
            {
                byte h = m.ReadByte(hl.Value);
                byte a = regA.Value;

                byte loa = (byte)(a & 0x0F);
                byte hia = (byte)(a & 0xF0);

                a = (byte)(hia | (h >> 4));
                h = (byte)((h << 4) | loa);

                var f = Flags;
                f.Sign = a > 0x7F;
                f.Zero = a == 0;
                f.HalfCarry = false;
                f.ParityOverflow = EvenParity(a);
                f.AddSub = false;
                f.SetUndocumentedFlags(a);

                regA.Value = a;
                m.WriteByte(hl.Value, h);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder RotateLeft(IReference<byte> reg, IReference<byte> aux = null, bool extended = true)
        {
            this.handler = m =>
            {
                // 4 t-states
                byte value = reg.Read(m);
                FlagsRegister f = this.Flags;

                byte oldCarry = f.Carry ? (byte)1 : (byte)0;
                bool highestBit = (value & 0x80) != 0;
                value <<= 1;
                value |= oldCarry;

                if (extended)
                {
                    f.Sign = value > 0x7F;
                    f.Zero = value == 0;
                    f.ParityOverflow = EvenParity(value);
                }

                f.Carry = highestBit;
                f.AddSub = false;
                f.HalfCarry = false;
                f.SetUndocumentedFlags(value);

                reg.Write(m, value);
                aux?.Write(m, value);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder RotateRight(IReference<byte> reg, IReference<byte> aux = null, bool extended = true)
        {
            this.handler = m =>
            {
                // 4 t-states
                byte value = reg.Read(m);
                FlagsRegister f = this.Flags;

                byte oldCarry = f.Carry ? (byte)0x80 : (byte)0;
                bool lowestBit = (value & 1) != 0;
                value >>= 1;
                value |= oldCarry;

                if (extended)
                {
                    f.Sign = value > 0x7F;
                    f.Zero = value == 0;
                    f.ParityOverflow = EvenParity(value);
                }

                f.Carry = lowestBit;
                f.AddSub = false;
                f.HalfCarry = false;
                f.SetUndocumentedFlags(value);

                reg.Write(m, value);
                aux?.Write(m, value);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder RotateLeftCarry(IReference<byte> reg, IReference<byte> aux = null, bool extended = true)
        {
            this.handler = m =>
            {
                // 4 t-states
                byte value = reg.Read(m);
                byte carry = (byte)(value >> 7);
                value <<= 1;
                value |= carry;
                FlagsRegister f = this.Flags;

                if (extended)
                {
                    f.Sign = value > 0x7F;
                    f.Zero = value == 0;
                    f.ParityOverflow = EvenParity(value);
                }

                f.Carry = carry > 0;
                f.AddSub = false;
                f.HalfCarry = false;
                f.SetUndocumentedFlags(value);

                reg.Write(m, value);
                aux?.Write(m, value);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder RotateRightCarry(IReference<byte> reg, IReference<byte> aux = null, bool extended = true)
        {
            this.handler = m =>
            {
                // 4 t-states
                byte value = reg.Read(m);
                byte carry = (byte)(value & 0x01);
                carry <<= 7;
                value >>= 1;
                value |= carry;
                FlagsRegister f = this.Flags;

                if (extended)
                {
                    f.Sign = value > 0x7F;
                    f.Zero = value == 0;
                    f.ParityOverflow = EvenParity(value);
                }

                f.Carry = carry != 0;
                f.AddSub = false;
                f.HalfCarry = false;
                f.SetUndocumentedFlags(value);

                reg.Write(m, value);
                aux?.Write(m, value);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder ShiftLeft(byte lowestBit, IReference<byte> reg, IReference<byte> aux = null)
        {
            this.handler = m =>
            {
                byte value = reg.Read(m);

                bool carry = (value & 0x80) != 0;
                value <<= 1;
                value |= lowestBit;
                FlagsRegister f = this.Flags;

                f.Sign = value > 0x7F;
                f.Zero = value == 0;
                f.ParityOverflow = EvenParity(value);
                f.Carry = carry;
                f.AddSub = false;
                f.HalfCarry = false;
                f.SetUndocumentedFlags(value);

                reg.Write(m, value);
                aux?.Write(m, value);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder ShiftRight(byte keepHighBit, IReference<byte> reg, IReference<byte> aux = null)
        {
            this.handler = m =>
            {
                byte value = reg.Read(m);

                bool carry = (value & 1) != 0;
                byte high = keepHighBit == 1 ? (byte)(value & 0x80) : (byte)0;
                value >>= 1;
                value |= high;
                FlagsRegister f = this.Flags;

                f.Sign = value > 0x7F;
                f.Zero = value == 0;
                f.ParityOverflow = EvenParity(value);
                f.Carry = carry;
                f.AddSub = false;
                f.HalfCarry = false;
                f.SetUndocumentedFlags(value);

                reg.Write(m, value);
                aux?.Write(m, value);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder TestBit(int bit, IReference<byte> reg)
        {
            this.handler = m =>
            {
                var v = reg.Read(m);
                var res = (byte)(v & (1 << bit));

                var f = Flags;
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = true;
                f.ParityOverflow = res == 0;
                f.AddSub = false;
                f.SetUndocumentedFlags(res); // todo: emulate IX+d and [HL] undocumented flags

                return State.Next;
            };
            return this;
        }

        public InstructionBuilder ResetBit(int bit, IReference<byte> reg, IReference<byte> aux = null)
        {
            this.handler = m =>
            {
                var v = reg.Read(m);

                var res = v & ~(1 << bit);
                reg.Write(m, (byte)res);
                aux?.Write(m, (byte)res);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder SetBit(int bit, IReference<byte> reg, IReference<byte> aux = null)
        {
            this.handler = m =>
            {
                var v = reg.Read(m);
                var res = v | (1 << bit);
                reg.Write(m, (byte)res);
                aux?.Write(m, (byte)res);
                return State.Next;
            };
            return this;
        }

        public InstructionBuilder In(Port port, ByteRegister dst, IReference<byte> portRef, ByteRegister high, bool extended = true)
        {
            this.handler = m =>
            {
                var portNumber = portRef.Read(m);
                var device = port.Get(portNumber);
                var highPart = high.Value;
                var value = device.Read(highPart);
                if (extended)
                {
                    // affect flags
                    var f = Flags;
                    f.Sign = value > 0x7F;
                    f.Zero = value == 0;
                    f.HalfCarry = false;
                    f.ParityOverflow = EvenParity(value);
                    f.AddSub = false;
                    f.SetUndocumentedFlags(value);
                }

                if(dst != null)
                {
                    dst.Value = value;
                }
                
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Out(Port port, ByteRegister src, IReference<byte> portRef, ByteRegister high)
        {
            this.handler = m =>
            {
                var portNumber = portRef.Read(m);
                var device = port.Get(portNumber);
                device.Write(high.Value, src?.Value ?? 0);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder SetInterruptMode(CPU cpu, int mode)
        {
            this.handler = m =>
            {
                cpu.InterruptMode = mode;
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder EnableInterrupts(CPU cpu)
        {
            this.handler = m =>
            {
                cpu.IFF1 = cpu.IFF2 = true;
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder DisableInterrupts(CPU cpu)
        {
            this.handler = m =>
            {
                cpu.IFF1 = cpu.IFF2 = false;
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Exx(GeneralRegisters r, GeneralRegisters rx)
        {
            this.handler = m =>
            {
                var bc = r.BC.Value;
                var de = r.DE.Value;
                var hl = r.HL.Value;

                r.BC.Value = rx.BC.Value;
                r.DE.Value = rx.DE.Value;
                r.HL.Value = rx.HL.Value;

                rx.BC.Value = bc;
                rx.DE.Value = de;
                rx.HL.Value = hl;

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Exchange(IReference<word> reg1, IReference<word> reg2)
        {
            // flags not affected
            this.handler = m =>
            {
                // 4 t-states
                var t = reg1.Read(m);
                reg1.Write(m, reg2.Read(m));
                reg2.Write(m, t);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Increment(WordRegister reg)
        {
            this.handler = m => 
            {
                reg.Increment(); // flags not affected
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Decrement(WordRegister reg)
        {
            this.handler = m => 
            {
                reg.Decrement(); // flags not affected
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Increment(IReference<byte> byteref)
        {
            this.handler = m => 
            {
                // 4 t-states
                byte prev = byteref.Read(m);
                byte next = prev;
                byteref.Write(m, ++next);
                FlagsRegister f = this.Flags;
                f.Sign = (next & 0x80) != 0;
                f.Zero = next == 0;
                f.HalfCarry = (prev & 0x0F) == 0x0F;
                f.ParityOverflow = (prev == 0x7F);
                f.AddSub = false;
                f.SetUndocumentedFlags(next);

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Decrement(IReference<byte> byteref)
        {
            this.handler = m => 
            {
                // 4 t-states
                byte prev = byteref.Read(m);
                byte next = prev;
                byteref.Write(m, --next);
                FlagsRegister f = this.Flags;
                f.Sign = (next & 0x80) != 0;
                f.Zero = next == 0;
                f.HalfCarry = (next & 0x0F) == 0x0F;
                f.ParityOverflow = (prev == 0x80);
                f.AddSub = true;
                f.SetUndocumentedFlags(next);

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder DecrementJumpIfZero(ByteRegister reg, IReference<byte> distance)
        {
            this.handler = m =>
            {
                reg.Decrement();
                if (reg.Value != 0)
                {
                    this.PC.Value += this.JumpByte(distance.Read(m));
                    return State.NextSlow;
                }
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder JumpRelative(IReference<byte> distance, Func<bool> p)
        {
            this.handler = m =>
            {
                if (p())
                {
                    this.PC.Value += this.JumpByte(distance.Read(m));
                    return State.NextSlow;
                }
                return State.Next;
            }; 
            
            return this;
        }

        public InstructionBuilder JumpRelative(IReference<byte> distance)
        {
            this.handler = m =>
            {
                this.PC.Value += this.JumpByte(distance.Read(m));
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Jump(IReference<word> newpc, Func<bool> cond = null)
        {
            this.handler = m =>
            {
                if (cond == null || cond())
                {
                    this.PC.Value = newpc.Read(m);
                    return State.RepeatSlow; // do not touch modified PC
                }
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Push(WordRegister sp, WordRegister reg)
        {
            this.handler = m =>
            {
                sp.Value -= 2;
                m.WriteWord(sp.Value, reg.Value);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Pop(WordRegister sp, WordRegister reg)
        {
            this.handler = m =>
            {
                reg.Value = m.ReadWord(sp.Value);
                sp.Value += 2;
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Reset(WordRegister sp, word offset)
        {
            this.handler = m =>
            {
                sp.Value -= 2;
                m.WriteWord(sp.Value, (word)(this.PC.Value + 1));
                this.PC.Value = offset;
                return State.RepeatSlow;
            }; 
            return this;
        }

        public InstructionBuilder Call(WordRegister sp, Func<bool> cond = null)
        {
            this.handler = m =>
            {
                if (cond == null || cond())
                {
                    sp.Value -= 2;
                    m.WriteWord(sp.Value, (word)(this.PC.Value + 3));
                    this.PC.Value = m.ReadWord((word)(this.PC.Value + 1));
                    return State.RepeatSlow;
                }

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Ret(WordRegister sp, Func<bool> cond = null)
        {
            this.handler = m =>
            {
                if(cond == null || cond())
                {
                    this.PC.Value = m.ReadWord(sp.Value);
                    sp.Value += 2;
                    return State.RepeatSlow;
                }

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Retn(WordRegister sp, CPU cpu)
        {
            this.handler = m =>
            {
                this.PC.Value = m.ReadWord(sp.Value);
                sp.Value += 2;
                cpu.IFF1 = cpu.IFF2;
                return State.RepeatSlow;
            }; 
            return this;
        }

        public InstructionBuilder Cp(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte res = (byte)(v1 - v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfBorrow(v1, v2);
                f.ParityOverflow = IsUnderflow(v1, v2, res);
                f.AddSub = true;
                f.Carry = v1 < v2;
                f.SetUndocumentedFlags(v2); // from operand

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Or(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte res = (byte)(v1 | v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = false;
                f.ParityOverflow = EvenParity(res);
                f.AddSub = false;
                f.Carry = false;
                f.SetUndocumentedFlags(res);

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Xor(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte res = (byte)(v1 ^ v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = false;
                f.ParityOverflow = EvenParity(res);
                f.AddSub = false;
                f.Carry = false;
                f.SetUndocumentedFlags(res);

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder And(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte res = (byte)(v1 & v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = true;
                f.ParityOverflow = EvenParity(res);
                f.AddSub = false;
                f.Carry = false;
                f.SetUndocumentedFlags(res);

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Sbc(IReference<word> dst, IReference<word> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                var v1 = dst.Read(m);
                var v2 = src.Read(m);
                byte v3 = f.Carry ? (byte)1 : (byte)0;
                byte h1 = (byte)(v1 >> 8);
                byte h2 = (byte)(v2 >> 8);
                word res = (word)(v1 - v2 - v3);

                f.Sign = res > 0x7FFF;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfBorrow(h1, h2, v3);
                f.ParityOverflow = IsUnderflow(v1, v2, res);
                f.AddSub = true;
                f.Carry = v1 < v2 + v3;
                f.SetUndocumentedFlags((byte)(h1 - h2)); // from high bytes sub?

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Sbc(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte v3 = f.Carry ? (byte)1 : (byte)0;
                byte res = (byte)(v1 - v2 - v3);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfBorrow(v1, v2, v3);
                f.ParityOverflow = IsUnderflow(v1, v2, res);
                f.AddSub = true;
                f.Carry = v1 < v2 + v3;
                f.SetUndocumentedFlags(res);

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Sub(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte res = (byte)(v1 - v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfBorrow(v1, v2);
                f.ParityOverflow = IsUnderflow(v1, v2, res);
                f.AddSub = true;
                f.Carry = v1 < v2;
                f.SetUndocumentedFlags(res);

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }
        
        public InstructionBuilder Neg(ByteRegister a)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                byte v1 = 0;
                byte v2 = a.Value;
                var res = a.Value = (byte)(v1 - v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfBorrow(v1, v2);
                f.ParityOverflow = IsUnderflow(v1, v2, res);
                f.AddSub = true;
                f.Carry = v1 < v2;
                f.SetUndocumentedFlags(res);
                
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Adc(IReference<word> dst, IReference<word> src)
        {
            this.handler = m =>
            {
                // 4 t-states
                var f = this.Flags;
                word v1 = dst.Read(m);
                word v2 = src.Read(m);
                byte v3 = f.Carry ? (byte)1 : (byte)0;
                word res = (word)(v1 + v2 + v3);

                f.Sign = res > 0x7FFF;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfCarry((byte)(v1 >> 8), (byte)(v2 >> 8), v3);
                f.ParityOverflow = IsOverflow(v1, v2, res);
                f.AddSub = false;
                f.Carry = (v1 + v2 + v3) > 0xFFFF;
                f.SetUndocumentedFlags((byte)(res >> 8));

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Adc(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                // 4 t-states
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte v3 = f.Carry ? (byte)1 : (byte)0;
                byte res = (byte)(v1 + v2 + v3);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfCarry(v1, v2, v3);
                f.ParityOverflow = IsOverflow(v1, v2, res);
                f.AddSub = false;
                f.Carry = (v1 + v2 + v3) > 0xFF;
                f.SetUndocumentedFlags(res);

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Add(IReference<byte> dst, IReference<byte> src)
        {
            this.handler = m =>
            {
                // 4 t-states
                var f = this.Flags;
                byte v1 = dst.Read(m);
                byte v2 = src.Read(m);
                byte res = (byte)(v1 + v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfCarry(v1, v2);
                f.ParityOverflow = IsOverflow(v1, v2, res);
                f.AddSub = false;
                f.Carry = (v1 + v2) > 0xFF;
                f.SetUndocumentedFlags(res);
                
                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Add(WordRegister dst, WordRegister src) // add src to dst
        {
            this.handler = m =>
            {
                // 11 t-states
                ushort x = dst.Value;
                ushort y = src.Value;
                int z = x + y;

                FlagsRegister f = this.Flags;
                f.HalfCarry = ((x ^ y ^ z) >> 8 & 0x10) != 0;
                f.AddSub = false;
                f.Carry = z > 0xFFFF;
                f.SetUndocumentedFlags((byte)(z >> 8 & 0xFF));

                dst.Value = (ushort)z;
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder BinaryCodedDecimalCorrection(ByteRegister reg)
        {
            this.handler = m =>
            {
                // 4 t-states
                var f = Flags;
                byte a = reg.Value;
                byte hi = (byte)(a >> 4);
                byte lo = (byte)(a & 0x0F);

                byte diff = DAA_Diff(hi, lo, f.Carry, f.HalfCarry);

                if (f.AddSub) a -= diff;
                else a += diff;

                reg.Value = a;

                //f.AddSub not affected
                f.Zero = (a == 0);
                f.ParityOverflow = EvenParity(a);
                f.Carry = f.Carry ? true : (hi >= 9 && lo > 9) || (hi > 9 && lo <= 9);
                f.Sign = (a & 0x80) != 0;
                f.HalfCarry = (!f.AddSub && lo > 9) || (f.AddSub && f.HalfCarry && lo <= 5);
                f.SetUndocumentedFlags(a);

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder InvertCarryFlag(ByteRegister a)
        {
            this.handler = m =>
            {
                Flags.HalfCarry = Flags.Carry;
                Flags.AddSub = false;
                Flags.Carry = !Flags.Carry;
                Flags.SetUndocumentedFlags(a.Value);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder SetCarryFlag(ByteRegister a)
        {
            this.handler = m =>
            {
                // 4 t-states
                Flags.HalfCarry = false;
                Flags.AddSub = false;
                Flags.Carry = true;
                Flags.SetUndocumentedFlags(a.Value);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Invert(ByteRegister a)
        {
            this.handler = m =>
            {
                // 4 t-states
                a.Value ^= 0xFF;
                var f = Flags;
                f.AddSub = true;
                f.HalfCarry = true;
                f.SetUndocumentedFlags(a.Value);

                return State.Next;
            }; 
            return this;
        }

        private bool IsUnderflow(word v1, word v2, word res)
        {
            return !SameSign(v1,v2) && SameSign(v2,res); 
        }

        private bool IsUnderflow(byte v1, byte v2, byte res)
        {
            return !SameSign(v1,v2) && SameSign(v2,res); 
        }

        private bool IsOverflow(word v1, word v2, word res)
        {
            return SameSign(v1,v2) && !SameSign(v2,res);
        }

        private bool IsOverflow(byte v1, byte v2, byte res)
        {
            return SameSign(v1,v2) && !SameSign(v2,res);
        }

        private bool SameSign(word a, word b)
        {
            return ((a ^ b) & 0x8000) == 0;
        }

        private bool SameSign(byte a, byte b)
        {
            return ((a ^ b) & 0x80) == 0;
        }

        private bool EvenParity(byte a)
        {
            ulong x1 = 0x0101010101010101;
            ulong x2 = 0x8040201008040201;
            return ((((a * x1) & x2) % (ulong)0x1FF) & 1) == 0;
        }

        private byte DAA_Diff(byte hi, byte lo, bool carry, bool halfcarry)
        {
            if (!carry && hi <= 9 && !halfcarry && lo <= 9) return 0x00;
            if (!carry && hi <= 9 && halfcarry && lo <= 9) return 0x06;
            if (!carry && hi <= 8 && lo > 9) return 0x06;
            if (!carry && hi > 9 && !halfcarry && lo <= 9) return 0x60;
            if (carry && !halfcarry && lo <= 9) return 0x60;
            if (carry && halfcarry && lo <= 9) return 0x66;
            if (carry && lo > 9) return 0x66;
            if (!carry && hi >= 9 && lo > 9) return 0x66;
            if (!carry && hi > 9 && halfcarry && lo <= 9) return 0x66;
            throw new Exception($"{hi} {lo} {carry} {halfcarry}");
        }

        // all relative jumps measured from next op after jmp
        private ushort JumpByte(byte offset)
        {
            if (offset < 0x80) // positive offset
                return (ushort)offset;
            else
                return (ushort)(-(0xFF - offset + 1));
        }

        private bool IsHalfBorrow(byte target, byte value, byte carry = 0)
        {
            return (target & 0xF) - (value & 0xF) - (carry) < 0;
        }

        private bool IsHalfCarry(byte target, byte value, byte carry = 0)
        {
            return (((target & 0xF) + (value & 0xF) + (carry & 0x0F)) & 0x10) == 0x10;
        }
    }
}
