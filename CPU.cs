using System;

namespace z80emu
{
    delegate void Handler(Memory memory);

    class CPU
    {
        public WordRegister regPC = new WordRegister();
        public WordRegister regSP = new WordRegister();
        public WordRegister regIX = new WordRegister();
        public WordRegister regIY = new WordRegister();

        public AFRegister regAF = new AFRegister();
        public AFRegister regAFx = new AFRegister();

        public GeneralRegisters Registers = new GeneralRegisters();
        public GeneralRegisters RegistersCopy = new GeneralRegisters();

        public byte regI;
        public byte regR;

        public Handler[] table = new Handler[0x100];

        public CPU()
        {
            table[0x00] = Nop();
            table[0x01] = LoadImm(Registers.BC);                             // LD BC,**
            table[0x02] = Load(Registers.BC.MemRef(), regAF.A.ValueRef());   // LD [BC],A
            table[0x03] = Increment(Registers.BC);                           // INC BC 
            table[0x04] = Increment(Registers.BC.High);                      // INC B
            table[0x05] = Decrement(Registers.BC.High);                      // DEC B
            table[0x06] = LoadImm(Registers.BC.High);                        // LD B,*
            table[0x07] = RotateLeftCarry(regAF.High);                       // RLCA
            table[0x08] = Exchange(regAF, regAFx);                           // EX AF,AF'
            table[0x09] = Add(Registers.HL, Registers.BC);                   // ADD HL,BC
            table[0x0A] = Load(regAF.A, Registers.BC.MemRef());              // LD A,[BC]
            table[0x0B] = Decrement(Registers.BC);                           // DEC BC
            table[0x0C] = Increment(Registers.BC.High);                      // INC B
            table[0x0D] = Decrement(Registers.BC.Low);                       // DEC C
            table[0x0E] = LoadImm(Registers.BC.Low);                         // LD C,*
            table[0x0F] = RotateRightCarry(regAF.High);                      // RRCA
            table[0x10] = DecrementJumpIfZeroImm();                          // DJNZ *
            table[0x11] = LoadImm(Registers.DE);                             // LD DE,**
            table[0x12] = Load(Registers.DE.MemRef(), regAF.A.ValueRef());   // LD [DE],A
            table[0x13] = Increment(Registers.DE);                           // INC DE
            table[0x14] = Increment(Registers.DE.High);                      // INC D
            table[0x15] = Decrement(Registers.DE.High);                      // DEC D
            table[0x16] = LoadImm(Registers.DE.High);                        // LD D,*
            table[0x17] = RotateLeft(regAF.High);                            // RLA
            table[0x18] = JumpImm();                                         // JR *
            table[0x19] = Add(Registers.HL, Registers.DE);                   // ADD HL,DE
        }

        public FlagsRegister Flags => this.regAF.F;

        public void Run(Memory memory)
        {
            var pc = regPC.MemRef();
            while (true)
            {
                Dump();
                var instruction = memory.ReadByte(pc);
                if (instruction == 0x76) 
                {
                    return; // temp: halt breaks execution
                }

                table[instruction](memory);
            }
        }

        public void Dump()
        {
            Registers.Dump("");
            regAF.Dump("AF");

            RegistersCopy.Dump("'");
            regAFx.Dump("AF'");

            regPC.Dump("PC");
            regSP.Dump("SP");
            regIX.Dump("IX");
            regIY.Dump("IY");
            Console.Write($"I={regI} ");
            Console.Write($"R={regR} ");
            Console.WriteLine();
        }

        public Handler Nop()
        {
            return m => regPC.Increment();
        }

        public Handler LoadImm(WordRegister dst)
        {
            return m =>
            {
                // 10 t-states
                dst.Value = m.ReadWord(regPC.MemRef(1)); // flags not affected
                regPC.Increment();
                regPC.Increment();
                regPC.Increment();
            };
        }

        public Handler LoadImm(ByteRegister dst)
        {
            return m =>
            {
                dst.Value = m.ReadByte(regPC.MemRef(1)); // flags not affected
                regPC.Increment();
                regPC.Increment();
            };
        }

        public Handler Load(ByteRegister dst, MemoryRef memorySrc)
        {
            return (Memory m) =>
            {
                // 7 t-states
                dst.Value = m.ReadByte(memorySrc); // flags not affected
                regPC.Increment();
            };
        }

        public Handler Load(MemoryRef memoryDst, ByteValueRef vr)
        {
            return (Memory m) =>
            {
                m.WriteByte(memoryDst, vr.ByteValue); // flags not affected
                regPC.Increment();
            };
        }

        public Handler RotateLeft(ByteRegister reg)
        {
            return (Memory m) =>
            {
                // 4 t-states
                byte value = reg.Value;
                FlagsRegister f = this.Flags;

                byte oldCarry = f.Carry ? (byte)1 : (byte)0;

                //f.Sign not affected
                //f.Zero not affected
                //f.ParityOverflow not affected
                f.Carry = (value & 0x80) != 0;
                f.AddSub = false;
                f.HalfCarry = false;
                value <<= 1;
                value |= oldCarry;
                reg.Value = value;
                regPC.Increment();
            };
        }

        public Handler RotateLeftCarry(ByteRegister reg)
        {
            return (Memory m) =>
            {
                // 4 t-states
                byte value = reg.Value;
                byte carry = (byte)(value >> 7);
                FlagsRegister f = this.Flags;

                //f.Sign not affected
                //f.Zero not affected
                //f.ParityOverflow not affected
                f.Carry = carry > 0;
                f.AddSub = false;
                f.HalfCarry = false;
                value <<= 1;
                value |= carry;
                reg.Value = value;
                regPC.Increment();
            };
        }

        public Handler RotateRightCarry(ByteRegister reg)
        {
            return (Memory m) =>
            {
                // 4 t-states
                byte value = reg.Value;
                byte carry = (byte)(value & 0x01);
                carry <<= 7;
                FlagsRegister f = this.Flags;

                //f.Sign not affected
                //f.Zero not affected
                //f.ParityOverflow not affected
                f.Carry = carry != 0;
                f.AddSub = false;
                f.HalfCarry = false;
                value >>= 1;
                value |= carry;
                reg.Value = value;
                regPC.Increment();
            };
        }

        public Handler Exchange(WordRegister reg1, WordRegister reg2)
        {
            // flags not affected
            return m =>
            {
                // 4 t-states
                var t = reg1.Value;
                reg1.Value = reg2.Value;
                reg2.Value = t;
                regPC.Increment();
            };
        }

        public Handler Increment(WordRegister reg)
        {
            return m => 
            {
                reg.Increment(); // flags not affected
                regPC.Increment();
            };
        }

        public Handler Decrement(WordRegister reg)
        {
            return m => 
            {
                reg.Decrement(); // flags not affected
                regPC.Increment();
            };
        }

        public Handler Increment(ByteRegister reg)
        {
            return m => 
            {
                // 4 t-states
                byte prev = reg.Increment();
                byte next = reg.Value;
                FlagsRegister f = this.Flags;
                f.Sign = (next & 0x80) != 0;
                f.Zero = next == 0;
                f.HalfCarry = (prev & 0x0F) == 0x0F;
                f.ParityOverflow = (prev == 0x7F);
                f.AddSub = false;
                // f.Carry preserved
                regPC.Increment();
            };
        }

        public Handler Decrement(ByteRegister reg)
        {
            return m => 
            {
                // 4 t-states
                byte prev = reg.Decrement();
                byte next = reg.Value;
                FlagsRegister f = this.Flags;
                f.Sign = (next & 0x80) != 0;
                f.Zero = next == 0;
                f.HalfCarry = (next & 0x0F) == 0x0F;
                f.ParityOverflow = (prev == 0x80);
                f.AddSub = true;
                // f.Carry preserved
                regPC.Increment();
            };
        }

        public Handler DecrementJumpIfZeroImm()
        {
            return m =>
            {
                var reg = this.Registers.BC.High;
                byte offset = m.ReadByte(regPC.MemRef(1));
                reg.Decrement();
                if (reg.Value != 0)
                {
                    // 13 t-states
                    this.JumpByte(offset);
                }
                else
                {
                    // 4 t-states
                    regPC.Increment();
                    regPC.Increment();
                }
            };
        }

        public Handler JumpImm()
        {
            return m =>
            {
                // 12 t-states
                byte offset = m.ReadByte(regPC.MemRef(1));
                this.JumpByte(offset);
            };
        }

        public Handler Add(WordRegister dst, WordRegister src) // add src to dst
        {
            return m =>
            {
                // 11 t-states
                ushort prev = dst.Value;
                ushort next = (ushort)(prev + src.Value);
                dst.Value = next;
                FlagsRegister f = this.Flags;
                f.HalfCarry = IsHalfCarry(dst.High.Value, src.High.Value);
                f.AddSub = false;
                f.Carry = prev.And(0x8000) == 0x8000 && next.And(0x8000) == 0;
                regPC.Increment();
            };
        }

        private void JumpByte(byte offset)
        {
            if (offset < 0x80) // positive offset
                this.regPC.Value += (ushort)offset;
            else
                this.regPC.Value -= (ushort)(0xFF - offset + 1);
        }

        private bool IsHalfCarry(byte target, byte value)
        {
            return (((target & 0xF) + (value & 0xF)) & 0x10) == 0x10;
        }
    }
}
