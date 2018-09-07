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
            table[0x07] = RotateLeftCarry(regAF.A);                          // RLCA
            table[0x08] = Exchange(regAF, regAFx);                           // EX AF,AF'
            table[0x09] = Add(Registers.HL, Registers.BC);                   // ADD HL,BC
            table[0x0A] = Load(regAF.A, Registers.BC.MemRef());              // LD A,[BC]
            table[0x0B] = Decrement(Registers.BC);                           // DEC BC
            table[0x0C] = Increment(Registers.BC.Low);                       // INC C
            table[0x0D] = Decrement(Registers.BC.Low);                       // DEC C
            table[0x0E] = LoadImm(Registers.BC.Low);                         // LD C,*
            table[0x0F] = RotateRightCarry(regAF.A);                         // RRCA

            table[0x10] = DecrementJumpIfZeroImm();                          // DJNZ *
            table[0x11] = LoadImm(Registers.DE);                             // LD DE,**
            table[0x12] = Load(Registers.DE.MemRef(), regAF.A.ValueRef());   // LD [DE],A
            table[0x13] = Increment(Registers.DE);                           // INC DE
            table[0x14] = Increment(Registers.DE.High);                      // INC D
            table[0x15] = Decrement(Registers.DE.High);                      // DEC D
            table[0x16] = LoadImm(Registers.DE.High);                        // LD D,*
            table[0x17] = RotateLeft(regAF.A);                               // RLA
            table[0x18] = JumpRelativeImm();                                 // JR *
            table[0x19] = Add(Registers.HL, Registers.DE);                   // ADD HL,DE
            table[0x1A] = Load(regAF.A, Registers.DE.MemRef());              // LD A,[DE]
            table[0x1B] = Decrement(Registers.DE);                           // DEC DE
            table[0x1C] = Increment(Registers.DE.Low);                       // INC E
            table[0x1D] = Decrement(Registers.DE.Low);                       // DEC E
            table[0x1E] = LoadImm(Registers.DE.Low);                         // LD E,*
            table[0x1F] = RotateRight(regAF.A);                              // RRA

            table[0x20] = JumpRelativeZeroImm(false);                        // JR NZ,*
            table[0x21] = LoadImm(Registers.HL);                             // LD HL,**
            table[0x22] = Load(Registers.HL.MemRef(), regAF.A.ValueRef());   // LD [HL],A
            table[0x23] = Increment(Registers.HL);                           // INC HL
            table[0x24] = Increment(Registers.HL.High);                      // INC H
            table[0x25] = Decrement(Registers.HL.High);                      // DEC H
            table[0x26] = LoadImm(Registers.HL.High);                        // LD H,*

            table[0x27] = BinaryCodedDecimalCorrection();                    // DAA
            table[0x28] = JumpRelativeZeroImm(true);                         // JR Z,*
            table[0x29] = Add(Registers.HL, Registers.HL);                   // ADD HL,HL
            table[0x2A] = LoadImm(Registers.HL);                             // LD HL,**
            table[0x2B] = Decrement(Registers.HL);                           // DEC HL
            table[0x2C] = Increment(Registers.HL.Low);                       // INC L
            table[0x2D] = Decrement(Registers.HL.Low);                       // DEC L
            table[0x2E] = LoadImm(Registers.HL.Low);                         // LD L,*
            table[0x2F] = InvertA();                                         // CPL
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

        public Handler RotateRight(ByteRegister reg)
        {
            return (Memory m) =>
            {
                // 4 t-states
                byte value = reg.Value;
                FlagsRegister f = this.Flags;

                byte oldCarry = f.Carry ? (byte)0x80 : (byte)0;

                //f.Sign not affected
                //f.Zero not affected
                //f.ParityOverflow not affected
                f.Carry = (value & 1) != 0;
                f.AddSub = false;
                f.HalfCarry = false;
                value >>= 1;
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
                
                // 4 t-states
                regPC.Increment();
                regPC.Increment();
                reg.Decrement();
                if (reg.Value != 0)
                {
                    // +9 t-states
                    this.JumpByte(offset);
                }
            };
        }

        public Handler JumpRelativeZeroImm(bool zero)
        {
            return m =>
            {
                byte offset = m.ReadByte(regPC.MemRef(1));
                // 7 t-states
                regPC.Increment();
                regPC.Increment();

                if (this.Flags.Zero == zero)
                {
                    // +5 t-states
                    JumpByte(offset);
                    return;
                }

            };
        }

        public Handler JumpRelativeImm()
        {
            return m =>
            {
                // 12 t-states
                byte offset = m.ReadByte(regPC.MemRef(1));
                regPC.Increment();
                regPC.Increment();
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
                byte oldSrcHigh = src.High.Value; // make copy in case src and dst is same register
                dst.Value = next;
                FlagsRegister f = this.Flags;
                f.HalfCarry = IsHalfCarry(dst.High.Value, oldSrcHigh);
                f.AddSub = false;
                f.Carry = prev.And(0x8000) == 0x8000 && next.And(0x8000) == 0;
                regPC.Increment();
            };
        }

        public Handler BinaryCodedDecimalCorrection()
        {
            return m =>
            {
                // 4 t-states
                var f = Flags;
                byte a = regAF.A.Value;
                byte hi = (byte)(a >> 4);
                byte lo = (byte)(a & 0x0F);

                byte diff = DAA_Diff(hi, lo, f.Carry, f.HalfCarry);

                if (f.AddSub) a -= diff;
                else a += diff;

                regAF.A.Value = a;

                //f.AddSub not affected
                f.Zero = (a == 0);
                f.ParityOverflow = EvenParity(a);
                f.Carry = f.Carry ? true : (hi >= 9 && lo > 9) || (hi > 9 && lo <= 9);
                f.Sign = (a & 0x80) != 0;
                f.HalfCarry = (!f.AddSub && lo > 9) || (f.AddSub && f.HalfCarry && lo <= 5);

                regPC.Increment();
            };
        }

        public Handler InvertA()
        {
            return m =>
            {
                // 4 t-states
                regAF.Value ^= 0xFF00;
                var f = Flags;
                f.AddSub = true;
                f.HalfCarry = true;

                regPC.Increment();
            };
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
