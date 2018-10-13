using System;
using word = System.UInt16;

namespace z80emu
{
    delegate word Handler(Memory memory);

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
            table[0x01] = Load(Registers.BC, regPC.WordRef(1), 3);           // LD BC,**
            table[0x02] = Load(Registers.BC.ByteRef(), regAF.A, 1);          // LD [BC],A
            table[0x03] = Increment(Registers.BC);                           // INC BC 
            table[0x04] = Increment(Registers.BC.High);                      // INC B
            table[0x05] = Decrement(Registers.BC.High);                      // DEC B
            table[0x06] = Load(Registers.BC.High, regPC.ByteRef(1), 2);      // LD B,*
            table[0x07] = RotateLeftCarry(regAF.A);                          // RLCA
            table[0x08] = Exchange(regAF, regAFx);                           // EX AF,AF'
            table[0x09] = Add(Registers.HL, Registers.BC);                   // ADD HL,BC
            table[0x0A] = Load(regAF.A, Registers.BC.ByteRef(), 1);          // LD A,[BC]
            table[0x0B] = Decrement(Registers.BC);                           // DEC BC
            table[0x0C] = Increment(Registers.BC.Low);                       // INC C
            table[0x0D] = Decrement(Registers.BC.Low);                       // DEC C
            table[0x0E] = Load(Registers.BC.Low, regPC.ByteRef(1), 2);       // LD C,*
            table[0x0F] = RotateRightCarry(regAF.A);                         // RRCA

            table[0x10] = DecrementJumpIfZero(Registers.BC.High, regPC.ByteRef(1));  // DJNZ *
            table[0x11] = Load(Registers.DE, regPC.WordRef(1), 3);           // LD DE,**
            table[0x12] = Load(Registers.DE.ByteRef(), regAF.A, 1);          // LD [DE],A
            table[0x13] = Increment(Registers.DE);                           // INC DE
            table[0x14] = Increment(Registers.DE.High);                      // INC D
            table[0x15] = Decrement(Registers.DE.High);                      // DEC D
            table[0x16] = Load(Registers.DE.High, regPC.ByteRef(1), 2);      // LD D,*
            table[0x17] = RotateLeft(regAF.A);                               // RLA
            table[0x18] = JumpRelative(regPC.ByteRef(1));                    // JR *
            table[0x19] = Add(Registers.HL, Registers.DE);                   // ADD HL,DE
            table[0x1A] = Load(regAF.A, Registers.DE.ByteRef(), 1);          // LD A,[DE]
            table[0x1B] = Decrement(Registers.DE);                           // DEC DE
            table[0x1C] = Increment(Registers.DE.Low);                       // INC E
            table[0x1D] = Decrement(Registers.DE.Low);                       // DEC E
            table[0x1E] = Load(Registers.DE.Low, regPC.ByteRef(1), 2);       // LD E,*
            table[0x1F] = RotateRight(regAF.A);                              // RRA

            table[0x20] = JumpRelative(regPC.ByteRef(1), ()=>!Flags.Zero);   // JR NZ,*
            table[0x21] = Load(Registers.HL, regPC.WordRef(1), 3);           // LD HL,**
            table[0x22] = Load(regPC.AsWordPtr(1), Registers.HL, 3);         // LD [**],HL
            table[0x23] = Increment(Registers.HL);                           // INC HL
            table[0x24] = Increment(Registers.HL.High);                      // INC H
            table[0x25] = Decrement(Registers.HL.High);                      // DEC H
            table[0x26] = Load(Registers.HL.High, regPC.ByteRef(1), 2);      // LD H,*
            table[0x27] = BinaryCodedDecimalCorrection(regAF.A);             // DAA
            table[0x28] = JumpRelative(regPC.ByteRef(1), ()=>Flags.Zero);    // JR Z,*
            table[0x29] = Add(Registers.HL, Registers.HL);                   // ADD HL,HL
            table[0x2A] = Load(Registers.HL, regPC.AsWordPtr(1), 3);         // LD HL,[**]
            table[0x2B] = Decrement(Registers.HL);                           // DEC HL
            table[0x2C] = Increment(Registers.HL.Low);                       // INC L
            table[0x2D] = Decrement(Registers.HL.Low);                       // DEC L
            table[0x2E] = Load(Registers.HL.Low, regPC.ByteRef(1), 2);       // LD L,*
            table[0x2F] = Invert(regAF.A);                                   // CPL
            
            table[0x30] = JumpRelative(regPC.ByteRef(1), ()=>!Flags.Carry);  // JR NC,*
            table[0x31] = Load(regSP, regPC.WordRef(1), 3);                  // LD SP,**
            table[0x32] = Load(regPC.AsBytePtr(1), regAF.A, 3);              // LD [**],A
            table[0x33] = Increment(regSP);                                  // INC SP
            table[0x34] = Increment(Registers.HL.ByteRef());                 // INC [HL]
            table[0x35] = Decrement(Registers.HL.ByteRef());                 // DEC [HL]
            table[0x36] = Load(Registers.HL.ByteRef(), regPC.ByteRef(1), 2); // LD [HL],*
            table[0x37] = SetCarryFlag();                                    // SCF
            table[0x38] = JumpRelative(regPC.ByteRef(1), ()=>Flags.Carry);   // JR C,*
            table[0x39] = Add(Registers.HL, regSP);                          // ADD HL,SP
            table[0x3A] = Load(regAF.A, regPC.AsBytePtr(1), 3);              // LD A,[**]
            table[0x3B] = Decrement(regSP);                                  // DEC SP
            table[0x3C] = Increment(regAF.A);                                // INC A
            table[0x3D] = Decrement(regAF.A);                                // DEC A
            table[0x3E] = Load(regAF.A, regPC.ByteRef(1), 2);                // LD A,*
            table[0x3F] = InvertCarryFlag();                                 // CCF

            table[0x40] = Load(Registers.BC.High, Registers.BC.High, 1);     // LD B,B
            table[0x41] = Load(Registers.BC.High, Registers.BC.Low, 1);      // LD B,C
            table[0x42] = Load(Registers.BC.High, Registers.DE.High, 1);     // LD B,D
            table[0x43] = Load(Registers.BC.High, Registers.DE.Low, 1);      // LD B,E
            table[0x44] = Load(Registers.BC.High, Registers.HL.High, 1);     // LD B,H
            table[0x45] = Load(Registers.BC.High, Registers.HL.Low, 1);      // LD B,L
            table[0x46] = Load(Registers.BC.High, Registers.HL.ByteRef(), 1);// LD B,[HL]
            table[0x47] = Load(Registers.BC.High, regAF.A, 1);               // LD B,A
            table[0x48] = Load(Registers.BC.Low, Registers.BC.High, 1);      // LD C,B
            table[0x49] = Load(Registers.BC.Low, Registers.BC.Low, 1);       // LD C,C
            table[0x4A] = Load(Registers.BC.Low, Registers.DE.High, 1);      // LD C,D
            table[0x4B] = Load(Registers.BC.Low, Registers.DE.Low, 1);       // LD C,E
            table[0x4C] = Load(Registers.BC.Low, Registers.HL.High, 1);      // LD C,H
            table[0x4D] = Load(Registers.BC.Low, Registers.HL.Low, 1);       // LD C,L
            table[0x4E] = Load(Registers.BC.Low, Registers.HL.ByteRef(), 1); // LD C,[HL]
            table[0x4F] = Load(Registers.BC.Low, regAF.A, 1);                // LD C,A

            table[0x50] = Load(Registers.DE.High, Registers.BC.High, 1);     // LD D,B
            table[0x51] = Load(Registers.DE.High, Registers.BC.Low, 1);      // LD D,C
            table[0x52] = Load(Registers.DE.High, Registers.DE.High, 1);     // LD D,D
            table[0x53] = Load(Registers.DE.High, Registers.DE.Low, 1);      // LD D,E
            table[0x54] = Load(Registers.DE.High, Registers.HL.High, 1);     // LD D,H
            table[0x55] = Load(Registers.DE.High, Registers.HL.Low, 1);      // LD D,L
            table[0x56] = Load(Registers.DE.High, Registers.HL.ByteRef(), 1);// LD D,[HL]
            table[0x57] = Load(Registers.DE.High, regAF.A, 1);               // LD D,A
            table[0x58] = Load(Registers.DE.Low, Registers.BC.High, 1);      // LD E,B
            table[0x59] = Load(Registers.DE.Low, Registers.BC.Low, 1);       // LD E,C
            table[0x5A] = Load(Registers.DE.Low, Registers.DE.High, 1);      // LD E,D
            table[0x5B] = Load(Registers.DE.Low, Registers.DE.Low, 1);       // LD E,E
            table[0x5C] = Load(Registers.DE.Low, Registers.HL.High, 1);      // LD E,H
            table[0x5D] = Load(Registers.DE.Low, Registers.HL.Low, 1);       // LD E,L
            table[0x5E] = Load(Registers.DE.Low, Registers.HL.ByteRef(), 1); // LD E,[HL]
            table[0x5F] = Load(Registers.DE.Low, regAF.A, 1);                // LD E,A

            table[0x60] = Load(Registers.HL.High, Registers.BC.High, 1);     // LD H,B
            table[0x61] = Load(Registers.HL.High, Registers.BC.Low, 1);      // LD H,C
            table[0x62] = Load(Registers.HL.High, Registers.DE.High, 1);     // LD H,D
            table[0x63] = Load(Registers.HL.High, Registers.DE.Low, 1);      // LD H,E
            table[0x64] = Load(Registers.HL.High, Registers.HL.High, 1);     // LD H,H
            table[0x65] = Load(Registers.HL.High, Registers.HL.Low, 1);      // LD H,L
            table[0x66] = Load(Registers.HL.High, Registers.HL.ByteRef(), 1);// LD H,[HL]
            table[0x67] = Load(Registers.HL.High, regAF.A, 1);               // LD H,A
            table[0x68] = Load(Registers.HL.Low, Registers.BC.High, 1);      // LD L,B
            table[0x69] = Load(Registers.HL.Low, Registers.BC.Low, 1);       // LD L,C
            table[0x6A] = Load(Registers.HL.Low, Registers.DE.High, 1);      // LD L,D
            table[0x6B] = Load(Registers.HL.Low, Registers.DE.Low, 1);       // LD L,E
            table[0x6C] = Load(Registers.HL.Low, Registers.HL.High, 1);      // LD L,H
            table[0x6D] = Load(Registers.HL.Low, Registers.HL.Low, 1);       // LD L,L
            table[0x6E] = Load(Registers.HL.Low, Registers.HL.ByteRef(), 1); // LD L,[HL]
            table[0x6F] = Load(Registers.HL.Low, regAF.A, 1);                // LD L,A

            table[0x70] = Load(Registers.HL.ByteRef(), Registers.BC.High, 1);// LD [HL],B
            table[0x71] = Load(Registers.HL.ByteRef(), Registers.BC.Low, 1); // LD [HL],C
            table[0x72] = Load(Registers.HL.ByteRef(), Registers.DE.High, 1);// LD [HL],D
            table[0x73] = Load(Registers.HL.ByteRef(), Registers.DE.Low, 1); // LD [HL],E
            table[0x74] = Load(Registers.HL.ByteRef(), Registers.HL.High, 1);// LD [HL],H
            table[0x75] = Load(Registers.HL.ByteRef(), Registers.HL.Low, 1); // LD [HL],L
            table[0x76] = null;                                              // HALT
            table[0x77] = Load(Registers.HL.ByteRef(), regAF.A, 1);          // LD [HL],A
            table[0x78] = Load(regAF.A, Registers.BC.High, 1);               // LD A,B
            table[0x79] = Load(regAF.A, Registers.BC.Low, 1);                // LD A,C
            table[0x7A] = Load(regAF.A, Registers.DE.High, 1);               // LD A,D
            table[0x7B] = Load(regAF.A, Registers.DE.Low, 1);                // LD A,E
            table[0x7C] = Load(regAF.A, Registers.HL.High, 1);               // LD A,H
            table[0x7D] = Load(regAF.A, Registers.HL.Low, 1);                // LD A,L
            table[0x7E] = Load(regAF.A, Registers.HL.ByteRef(), 1);          // LD A,[HL]
            table[0x7F] = Load(regAF.A, regAF.A, 1);                         // LD A,A

            table[0x80] = Add(regAF.A, Registers.BC.High);                   // ADD A,B
            table[0x81] = Add(regAF.A, Registers.BC.Low);                    // ADD A,C
            table[0x82] = Add(regAF.A, Registers.DE.High);                   // ADD A,D
            table[0x83] = Add(regAF.A, Registers.DE.Low);                    // ADD A,E
            table[0x84] = Add(regAF.A, Registers.HL.High);                   // ADD A,H
            table[0x85] = Add(regAF.A, Registers.HL.Low);                    // ADD A,L
            table[0x86] = Add(regAF.A, Registers.HL.ByteRef());              // ADD A,[HL]
            table[0x87] = Add(regAF.A, regAF.A);                             // ADD A,A
            table[0x88] = Adc(regAF.A, Registers.BC.High);                   // ADC A,B
            table[0x89] = Adc(regAF.A, Registers.BC.Low);                    // ADC A,C
            table[0x8A] = Adc(regAF.A, Registers.DE.High);                   // ADC A,D
            table[0x8B] = Adc(regAF.A, Registers.DE.Low);                    // ADC A,E
            table[0x8C] = Adc(regAF.A, Registers.HL.High);                   // ADC A,H
            table[0x8D] = Adc(regAF.A, Registers.HL.Low);                    // ADC A,L
            table[0x8E] = Adc(regAF.A, Registers.HL.ByteRef());              // ADC A,[HL]
            table[0x8F] = Adc(regAF.A, regAF.A);                             // ADC A,A

            table[0x90] = Sub(regAF.A, Registers.BC.High);                   // SUB B
            table[0x91] = Sub(regAF.A, Registers.BC.Low);                    // SUB C
            table[0x92] = Sub(regAF.A, Registers.DE.High);                   // SUB D
            table[0x93] = Sub(regAF.A, Registers.DE.Low);                    // SUB E
            table[0x94] = Sub(regAF.A, Registers.HL.High);                   // SUB H
            table[0x95] = Sub(regAF.A, Registers.HL.Low);                    // SUB L
            table[0x96] = Sub(regAF.A, Registers.HL.ByteRef());              // SUB [HL]
            table[0x97] = Sub(regAF.A, regAF.A);                             // SUB A
            table[0x98] = Sbc(regAF.A, Registers.BC.High);                   // SBC A,B
            table[0x99] = Sbc(regAF.A, Registers.BC.Low);                    // SBC A,C
            table[0x9A] = Sbc(regAF.A, Registers.DE.High);                   // SBC A,D
            table[0x9B] = Sbc(regAF.A, Registers.DE.Low);                    // SBC A,E
            table[0x9C] = Sbc(regAF.A, Registers.HL.High);                   // SBC A,H
            table[0x9D] = Sbc(regAF.A, Registers.HL.Low);                    // SBC A,L
            table[0x9E] = Sbc(regAF.A, Registers.HL.ByteRef());              // SBC A,[HL]
            table[0x9F] = Sbc(regAF.A, regAF.A);                             // SBC A,A

            table[0xA0] = And(regAF.A, Registers.BC.High);                   // AND B
            table[0xA1] = And(regAF.A, Registers.BC.Low);                    // AND C
            table[0xA2] = And(regAF.A, Registers.DE.High);                   // AND D
            table[0xA3] = And(regAF.A, Registers.DE.Low);                    // AND E
            table[0xA4] = And(regAF.A, Registers.HL.High);                   // AND H
            table[0xA5] = And(regAF.A, Registers.HL.Low);                    // AND L
            table[0xA6] = And(regAF.A, Registers.HL.ByteRef());              // AND [HL]
            table[0xA7] = And(regAF.A, regAF.A);                             // AND A
            table[0xA8] = Xor(regAF.A, Registers.BC.High);                   // XOR B
            table[0xA9] = Xor(regAF.A, Registers.BC.Low);                    // XOR C
            table[0xAA] = Xor(regAF.A, Registers.DE.High);                   // XOR D
            table[0xAB] = Xor(regAF.A, Registers.DE.Low);                    // XOR E
            table[0xAC] = Xor(regAF.A, Registers.HL.High);                   // XOR H
            table[0xAD] = Xor(regAF.A, Registers.HL.Low);                    // XOR L
            table[0xAE] = Xor(regAF.A, Registers.HL.ByteRef());              // XOR [HL]
            table[0xAF] = Xor(regAF.A, regAF.A);                             // XOR A

            table[0xB0] = Or(regAF.A, Registers.BC.High);                    // OR B
            table[0xB1] = Or(regAF.A, Registers.BC.Low);                     // OR C
            table[0xB2] = Or(regAF.A, Registers.DE.High);                    // OR D
            table[0xB3] = Or(regAF.A, Registers.DE.Low);                     // OR E
            table[0xB4] = Or(regAF.A, Registers.HL.High);                    // OR H
            table[0xB5] = Or(regAF.A, Registers.HL.Low);                     // OR L
            table[0xB6] = Or(regAF.A, Registers.HL.ByteRef());               // OR [HL]
            table[0xB7] = Or(regAF.A, regAF.A);                              // OR A
            table[0xB8] = Cp(regAF.A, Registers.BC.High);                    // CP B
            table[0xB9] = Cp(regAF.A, Registers.BC.Low);                     // CP C
            table[0xBA] = Cp(regAF.A, Registers.DE.High);                    // CP D
            table[0xBB] = Cp(regAF.A, Registers.DE.Low);                     // CP E
            table[0xBC] = Cp(regAF.A, Registers.HL.High);                    // CP H
            table[0xBD] = Cp(regAF.A, Registers.HL.Low);                     // CP L
            table[0xBE] = Cp(regAF.A, Registers.HL.ByteRef());               // CP [HL]
            table[0xBF] = Cp(regAF.A, regAF.A);                              // CP A

            table[0xC0] = Ret(regSP, regPC, () => !Flags.Zero);              // RET NZ
            table[0xC1] = Pop(regSP, Registers.BC);                          // POP BC
            table[0xC2] = JumpAbsolute(regPC, () => !Flags.Zero);            // JP NZ,**
            table[0xC3] = JumpAbsolute(regPC, () => true);                   // JP **
            table[0xC4] = Call(regSP, regPC, () => !Flags.Zero);             // CALL NZ,**
            table[0xC5] = Push(regSP, Registers.BC);                         // PUSH BC
            table[0xC6] = Add(regAF.A, regPC.ByteRef(1), 2);                 // ADD A,*
            table[0xC7] = Reset(regSP, regPC, 0);                            // RST 0x00
            table[0xC8] = Ret(regSP, regPC, () => Flags.Zero);               // RET Z
            table[0xC9] = Ret(regSP, regPC, () => true);                     // RET
            table[0xCA] = JumpAbsolute(regPC, () => Flags.Zero);             // JP Z,**
            table[0xCB] = null; //  BITS
            table[0xCC] = Call(regSP, regPC, () => Flags.Zero);              // CALL Z,**
            table[0xCD] = Call(regSP, regPC, () => true);                    // CALL **
            table[0xCE] = Adc(regAF.A, regPC.ByteRef(1), 2);                 // ADC A,*
            table[0xCF] = Reset(regSP, regPC, 0x08);                         // RST 0x08
        }

        public FlagsRegister Flags => this.regAF.F;

        public void Run(Memory memory)
        {
            while (true)
            {
                Dump();
                var instruction = memory.ReadByte(regPC.Value);
                if (instruction == 0x76) 
                {
                    return; // temp: halt breaks execution
                }

                var offset = table[instruction](memory);
                regPC.Value += offset;
            }
        }

        public void Dump()
        {
            regAF.Dump("AF");
            Registers.Dump("");

            regAFx.Dump("AF'");
            RegistersCopy.Dump("'");

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
            return m => 
            {
                return 1;
            };
        }

        public Handler Load<T>(IPointerReference<T> dst, IReference<T> src, word size)
        {
            return m =>
            {
                dst.Get(m).Write(m, src.Read(m));
                return size;
            };
        }

        public Handler Load<T>(IReference<T> dst, IPointerReference<T> src, word size)
        {
            return m =>
            {
                // 10 t-states
                dst.Write(m, src.Get(m).Read(m));// flags not affected
                return size;
            };
        }

        public Handler Load<T>(IReference<T> dst, IReference<T> src, word size)
        {
            return (Memory m) =>
            {
                // 7 t-states
                dst.Write(m, src.Read(m));
                return size;
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
                return 1;
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
                return 1;
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
                return 1;
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
                return 1;
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
                return 1;
            };
        }

        public Handler Increment(WordRegister reg)
        {
            return m => 
            {
                reg.Increment(); // flags not affected
                return 1;
            };
        }

        public Handler Decrement(WordRegister reg)
        {
            return m => 
            {
                reg.Decrement(); // flags not affected
                return 1;
            };
        }

        public Handler Increment(IReference<byte> byteref)
        {
            return m => 
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
                // f.Carry preserved
                return 1;
            };
        }

        public Handler Decrement(IReference<byte> byteref)
        {
            return m => 
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
                // f.Carry preserved
                return 1;
            };
        }

        public Handler DecrementJumpIfZero(ByteRegister reg, IReference<byte> distance)
        {
            return m =>
            {
                byte offset = distance.Read(m);
                
                // 4 t-states
                reg.Decrement();
                word ret = 2;
                if (reg.Value != 0)
                {
                    // +9 t-states
                    ret += this.JumpByte(offset);
                }
                return ret;
            };
        }

        public Handler JumpRelative(IReference<byte> distance, Func<bool> p)
        {
            return m =>
            {
                byte offset = distance.Read(m);
                // 7 t-states
                word ret = 2;
                if (p())
                {
                    ret += this.JumpByte(offset); // +5 t-states
                }
                return ret;
            };
        }

        public Handler JumpRelative(IReference<byte> distance)
        {
            return m =>
            {
                // 12 t-states
                byte offset = distance.Read(m);
                word ret = 2;
                ret += this.JumpByte(offset);
                return ret;
            };
        }

        public Handler JumpAbsolute(WordRegister regPC, Func<bool> p)
        {
            return m =>
            {
                if (p())
                {
                    regPC.Value = m.ReadWord((word)(regPC.Value + 1));
                    return 0;
                }
                return 3;
            };
        }

        public Handler Push(WordRegister sp, WordRegister reg)
        {
            return m =>
            {
                sp.Value -= 2;
                m.WriteWord(sp.Value, reg.Value);
                return 1;
            };
        }

        public Handler Pop(WordRegister sp, WordRegister reg)
        {
            return m =>
            {
                reg.Value = m.ReadWord(sp.Value);
                sp.Value += 2;
                return 1;
            };
        }

        public Handler Reset(WordRegister sp, WordRegister pc, byte offset)
        {
            return m =>
            {
                sp.Value -= 2;
                m.WriteWord(sp.Value, pc.Value);
                pc.Value = offset;
                return 0;
            };
        }

        public Handler Call(WordRegister sp, WordRegister pc, Func<bool> p)
        {
            return m =>
            {
                if (p())
                {
                    sp.Value -= 2;
                    m.WriteWord(sp.Value, (word)(pc.Value + 3));
                    pc.Value = m.ReadWord((word)(pc.Value + 1));
                    return 0;
                }

                return 3;
            };
        }

        public Handler Ret(WordRegister sp, WordRegister pc, Func<bool> p)
        {
            return m =>
            {
                if(p())
                {
                    regPC.Value = m.ReadWord(sp.Value);
                    sp.Value += 2;
                    return 0;
                }

                return 1;
            };
        }

        public Handler Cp(IReference<byte> dst, IReference<byte> src)
        {
            return m =>
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
                return 1;
            };
        }

        public Handler Or(IReference<byte> dst, IReference<byte> src)
        {
            return m =>
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
                dst.Write(m, res);
                return 1;
            };
        }

        public Handler Xor(IReference<byte> dst, IReference<byte> src)
        {
            return m =>
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
                dst.Write(m, res);
                return 1;
            };
        }

        public Handler And(IReference<byte> dst, IReference<byte> src)
        {
            return m =>
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
                dst.Write(m, res);
                return 1;
            };
        }

        public Handler Sbc(IReference<byte> dst, IReference<byte> src)
        {
            return m =>
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
                dst.Write(m, res);
                return 1;
            };
        }

        public Handler Sub(IReference<byte> dst, IReference<byte> src)
        {
            return m =>
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
                dst.Write(m, res);
                return 1;
            };
        }

        public Handler Adc(IReference<byte> dst, IReference<byte> src, byte sz = 1)
        {
            return m =>
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
                dst.Write(m, res);
                return sz;
            };
        }

        public Handler Add(IReference<byte> dst, IReference<byte> src, byte sz = 1)
        {
            return m =>
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
                dst.Write(m, res);
                return sz;
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
                return 1;
            };
        }

        public Handler BinaryCodedDecimalCorrection(ByteRegister reg)
        {
            return m =>
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

                return 1;
            };
        }

        public Handler InvertCarryFlag()
        {
            return m =>
            {
                Flags.HalfCarry = Flags.Carry;
                Flags.AddSub = false;
                Flags.Carry = !Flags.Carry;
                return 1;
            };
        }

        public Handler SetCarryFlag()
        {
            return m =>
            {
                // 4 t-states
                Flags.HalfCarry = false;
                Flags.AddSub = false;
                Flags.Carry = true;
                return 1;
            };
        }

        public Handler Invert(ByteRegister reg)
        {
            return m =>
            {
                // 4 t-states
                reg.Value ^= 0xFF;
                var f = Flags;
                f.AddSub = true;
                f.HalfCarry = true;

                return 1;
            };
        }

        private bool IsUnderflow(byte v1, byte v2, byte res)
        {
            return !SameSign(v1,v2) && SameSign(v2,res); 
        }

        private bool IsOverflow(byte v1, byte v2, byte res)
        {
            return SameSign(v1,v2) && !SameSign(v2,res);
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
