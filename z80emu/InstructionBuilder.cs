using System;
using System.Runtime.CompilerServices;
using Word16 = System.UInt16;

namespace z80emu
{
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
            var instruction = m.ReadByte((Word16)(pc.Value + this.offset));
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
        private readonly CPU cpu;
        private readonly Clock clock;

        public InstructionBuilder(CPU cpu, WordRegister pc, FlagsRegister flags, ByteRegister r, Clock clock)
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
                f.SetFlags(
                    sign: v > 0x7F,
                    zero: v == 0,
                    halfCarry: false,
                    overflow: cpu.IFF2,
                    addsub: false,
                    v);

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

                var c = counter.Value;
                var n = (byte)(a.Value + data);

                int f = this.Flags.Value;
                f &= 0b11000001; // reset bits addsub(1), overflow(2), flag3(3), halfcarry(4), flag5(5)
                f |= (c != 0 ? 4 : 0); // set overflow
                f |= (n & 0b1000); // bit 3
                f |= (n & 0b0010) << 4; // bit 1 shifted to position 5
                Flags.Value = (byte)f;

                if (mode.HasFlag(BlockMode.Once))
                    return State.Next;
                if (c == 0) 
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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: h,
                    overflow: counter.Value != 0,
                    addsub: true,
                    (byte)((n & 0b001000) | ((n << 4) & 0b100000))); // bit 3 and bit 1 shifted to position 5

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

                // undocumented
                var k = hl.Low.Value + v;
                var b = B.Value;

                f.SetFlags(
                    sign:(b & 0x80) != 0,
                    zero:b == 0,
                    halfCarry:k > 255,
                    overflow:EvenParity((byte)((k & 7) ^ b)),
                    addsub:(v & 0x80) != 0,
                    carry:k > 255,
                    b);

                if (mode.HasFlag(BlockMode.Once))
                    return State.Next;
                if (b == 0) 
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

                // undocumented
                var b = B.Value;
                var k = (increment ? C.Value + 1 : C.Value - 1) + v;

                f.SetFlags(
                    sign:(b & 0x80) != 0,
                    zero: b == 0,
                    halfCarry:k > 255,
                    overflow:EvenParity((byte)((k & 7) ^ b)),
                    addsub: true,
                    carry:k > 255,
                    b); 

                if (mode.HasFlag(BlockMode.Once))
                    return State.Next;
                if (b == 0) 
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
                f.SetFlags(
                    sign:a > 0x7F,
                    zero: a == 0,
                    halfCarry: false,
                    overflow: EvenParity(a),
                    addsub: false,
                    a);

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
                f.SetFlags(
                    sign:a > 0x7F,
                    zero: a == 0,
                    halfCarry: false,
                    overflow: EvenParity(a),
                    addsub: false,
                    a);

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
                    f.SetFlags(
                        sign: value > 0x7F,
                        zero: value == 0,
                        halfCarry: false,
                        overflow: EvenParity(value),
                        addsub: false,
                        carry: highestBit,
                        value);
                }
                else
                {
                    f.SetFlags(
                        halfCarry: false,
                        addsub: false,
                        carry: highestBit,
                        value);
                }

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
                    f.SetFlags(
                        sign: value > 0x7F,
                        zero: value == 0,
                        halfCarry: false,
                        overflow: EvenParity(value),
                        addsub: false,
                        carry: lowestBit,
                        value);
                }
                else
                {
                    f.SetFlags(
                        halfCarry: false,
                        addsub: false,
                        carry: lowestBit,
                        value);
                }

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
                    f.SetFlags(
                        sign: value > 0x7F,
                        zero: value == 0,
                        halfCarry: false,
                        overflow: EvenParity(value),
                        addsub: false,
                        carry: carry > 0,
                        value);
                }
                else
                {
                    f.SetFlags(
                        halfCarry: false,
                        addsub: false,
                        carry: carry > 0,
                        value);
                }

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
                    f.SetFlags(
                        sign: value > 0x7F,
                        zero: value == 0,
                        halfCarry: false,
                        overflow: EvenParity(value),
                        addsub: false,
                        carry: carry != 0,
                        value);
                }
                else
                {
                    f.SetFlags(
                        halfCarry: false,
                        addsub: false,
                        carry: carry != 0,
                        value);
                }

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

                f.SetFlags(
                    sign: value > 0x7F,
                    zero: value == 0,
                    halfCarry: false,
                    overflow: EvenParity(value),
                    addsub: false,
                    carry: carry,
                    value);

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

                f.SetFlags(
                    sign: value > 0x7F,
                    zero: value == 0,
                    halfCarry: false,
                    overflow: EvenParity(value),
                    addsub: false,
                    carry: carry,
                    value);

                reg.Write(m, value);
                aux?.Write(m, value);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder TestBit(int bit, ByteRegister reg)
        {
            this.handler = m =>
            {
                var v = reg.Read(m);
                var res = (byte)(v & (1 << bit));
                FlagsRegister f = this.Flags;

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: true,
                    overflow: res == 0,
                    addsub: false,
                    v); // todo: emulate IX+d and [HL] undocumented flags

                return State.Next;
            };
            return this;
        }

        public InstructionBuilder TestBitRef(int bit, IReference<byte> r)
        {
            this.handler = m =>
            {
                var memref = r as MemoryReference;

                var v = r.Read(m);
                var res = (byte)(v & (1 << bit));

                var f = Flags;
                f.SetFlags(
                    sign:res > 0x7F,
                    zero: res == 0,
                    halfCarry: true,
                    overflow: res == 0,
                    addsub: false,
                    (byte)(memref.GetOffset(m) & 0xFF)); // todo: for [HL] test, get state from internal undocumented register

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
                    this.Flags.SetFlags(
                        sign: value > 0x7F,
                        zero: value == 0,
                        halfCarry: false,
                        overflow: EvenParity(value),
                        addsub: false,
                        value);
                }

                if (dst != null)
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

        public InstructionBuilder Exchange(IReference<Word16> reg1, IReference<Word16> reg2)
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

                f.SetFlags(
                    sign: (next & 0x80) != 0,
                    zero: next == 0,
                    halfCarry: (prev & 0x0F) == 0x0F,
                    overflow: (prev == 0x7F),
                    addsub: false,
                    next);

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

                f.SetFlags(
                    sign: (next & 0x80) != 0,
                    zero: next == 0,
                    halfCarry: (next & 0x0F) == 0x0F,
                    overflow: (prev == 0x80),
                    addsub: true,
                    next);

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

        public InstructionBuilder Jump(IReference<Word16> newpc, Func<bool> cond = null)
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

        public InstructionBuilder Reset(WordRegister sp, Word16 offset)
        {
            this.handler = m =>
            {
                sp.Value -= 2;
                m.WriteWord(sp.Value, (Word16)(this.PC.Value + 1));
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
                    m.WriteWord(sp.Value, (Word16)(this.PC.Value + 3));
                    this.PC.Value = m.ReadWord((Word16)(this.PC.Value + 1));
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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: IsHalfBorrow(v1, v2),
                    overflow: IsUnderflow(v1, v2, res),
                    addsub: true,
                    carry: v1 < v2,
                    v2); // undocumented flags from operand

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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: false,
                    overflow: EvenParity(res),
                    addsub: false,
                    carry: false,
                    res);

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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: false,
                    overflow: EvenParity(res),
                    addsub: false,
                    carry: false,
                    res);

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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: true,
                    overflow: EvenParity(res),
                    addsub: false,
                    carry: false,
                    res);

                dst.Write(m, res);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Sbc(IReference<Word16> dst, IReference<Word16> src)
        {
            this.handler = m =>
            {
                var f = this.Flags;
                var v1 = dst.Read(m);
                var v2 = src.Read(m);
                byte v3 = f.Carry ? (byte)1 : (byte)0;
                byte h1 = (byte)(v1 >> 8);
                byte h2 = (byte)(v2 >> 8);
                int res = v1 - v2 - v3;
                int c = v1 ^ v2 ^ res;

                f.SetFlags(
                    sign: ((Word16)res) > 0x7FFF,
                    zero: (res & 0xFFFF) == 0,
                    halfCarry: (c >> 8 & 0x10) != 0,
                    overflow: IsUnderflow(v1, v2, (Word16)res),
                    addsub: true,
                    carry: v1 < v2 + v3,
                    (byte)(res >> 8));

                dst.Write(m, (Word16)res);
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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: IsHalfBorrow(v1, v2, v3),
                    overflow: IsUnderflow(v1, v2, res),
                    addsub: true,
                    carry: v1 < v2 + v3,
                    res);

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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: IsHalfBorrow(v1, v2),
                    overflow: IsUnderflow(v1, v2, res),
                    addsub: true,
                    carry: v1 < v2,
                    res);

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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: IsHalfBorrow(v1, v2),
                    overflow: IsUnderflow(v1, v2, res),
                    addsub: true,
                    carry: v1 < v2,
                    res);
                
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder Adc(IReference<Word16> dst, IReference<Word16> src)
        {
            this.handler = m =>
            {
                // 4 t-states
                var f = this.Flags;
                Word16 v1 = dst.Read(m);
                Word16 v2 = src.Read(m);
                byte v3 = f.Carry ? (byte)1 : (byte)0;
                int res = v1 + v2 + v3;
                int c = v1 ^ v2 ^ res;

                f.SetFlags(
                    sign: ((Word16)res) > 0x7FFF,
                    zero: (res & 0xFFFF) == 0,
                    halfCarry: (c >> 8 & 0x10) != 0,
                    overflow: IsOverflow(v1, v2, (Word16)res),
                    addsub: false,
                    carry: res > 0xFFFF,
                    (byte)(res >> 8 & 0xFF));

                dst.Write(m, (Word16)res);
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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: IsHalfCarry(v1, v2, v3),
                    overflow: IsOverflow(v1, v2, res),
                    addsub: false,
                    carry: (v1 + v2 + v3) > 0xFF,
                    res);

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

                f.SetFlags(
                    sign: res > 0x7F,
                    zero: res == 0,
                    halfCarry: IsHalfCarry(v1, v2),
                    overflow: IsOverflow(v1, v2, res),
                    addsub: false,
                    carry: (v1 + v2) > 0xFF,
                    res);

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

                Flags.SetFlags(
                    halfCarry: ((x ^ y ^ z) >> 8 & 0x10) != 0,
                    addsub: false,
                    carry: z > 0xFFFF,
                    ((byte)(z >> 8 & 0xFF)));

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
                bool addsub = f.AddSub;
                if (addsub)
                    a -= diff;
                else
                    a += diff;

                reg.Value = a;

                //f.AddSub not affected
                f.SetFlags(
                    sign:(a & 0x80) != 0,
                    zero:a == 0,
                    halfCarry:(!f.AddSub && lo > 9) || (f.AddSub && f.HalfCarry && lo <= 5),
                    overflow: EvenParity(a),
                    addsub: addsub,
                    carry: f.Carry ? true : (hi >= 9 && lo > 9) || (hi > 9 && lo <= 9),
                    a);

                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder InvertCarryFlag(ByteRegister a)
        {
            this.handler = m =>
            {
                bool carry = Flags.Carry;
                Flags.SetFlags(halfCarry: carry, addsub: false, carry: !carry, a.Value);
                return State.Next;
            }; 
            return this;
        }

        public InstructionBuilder SetCarryFlag(ByteRegister a)
        {
            this.handler = m =>
            {
                // 4 t-states
                Flags.SetFlags(halfCarry: false, addsub: false, carry: true, undocumented: a.Value);
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
                Flags.SetFlags(halfCarry: true, addsub: true, carry: Flags.Carry, a.Value);
                return State.Next;
            }; 
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsUnderflow(Word16 v1, Word16 v2, Word16 res)
        {
            return !SameSign(v1,v2) && SameSign(v2,res); 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsUnderflow(byte v1, byte v2, byte res)
        {
            return !SameSign(v1,v2) && SameSign(v2,res); 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOverflow(Word16 v1, Word16 v2, Word16 res)
        {
            return SameSign(v1,v2) && !SameSign(v2,res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsOverflow(byte v1, byte v2, byte res)
        {
            return SameSign(v1,v2) && !SameSign(v2,res);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SameSign(Word16 a, Word16 b)
        {
            return ((a ^ b) & 0x8000) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SameSign(byte a, byte b)
        {
            return ((a ^ b) & 0x80) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ushort JumpByte(byte offset)
        {
            if (offset < 0x80) // positive offset
                return (ushort)offset;
            else
                return (ushort)(-(0xFF - offset + 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsHalfBorrow(byte target, byte value, byte carry = 0)
        {
            return (target & 0xF) - (value & 0xF) - (carry) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsHalfCarry(byte target, byte value, byte carry = 0)
        {
            return (((target & 0xF) + (value & 0xF) + (carry & 0x0F)) & 0x10) == 0x10;
        }
    }
}
