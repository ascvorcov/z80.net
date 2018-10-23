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

        public Port Port = Port.Create();
        
        public WordRegister regIR = new WordRegister();
        public ByteRegister regI => regIR.High;
        public ByteRegister regR => regIR.Low;
        public bool IFF1;
        public bool IFF2;
        public int InterruptMode = 0;

        public Handler[] table = new Handler[0x100];

        private DateTime previousInterrupt = DateTime.Now;

        public CPU()
        {
            table[0x00] = Nop();
            table[0x01] = Load(Registers.BC, regPC.WordRef(1), 3);           // LD BC,**
            table[0x02] = Load(Registers.BC.ByteRef(), regAF.A, 1);          // LD [BC],A
            table[0x03] = Increment(Registers.BC);                           // INC BC 
            table[0x04] = Increment(Registers.BC.High);                      // INC B
            table[0x05] = Decrement(Registers.BC.High);                      // DEC B
            table[0x06] = Load(Registers.BC.High, regPC.ByteRef(1), 2);      // LD B,*
            table[0x07] = RotateLeftCarry(regAF.A, false);                   // RLCA
            table[0x08] = Exchange(regAF, regAFx);                           // EX AF,AF'
            table[0x09] = Add(Registers.HL, Registers.BC);                   // ADD HL,BC
            table[0x0A] = Load(regAF.A, Registers.BC.ByteRef(), 1);          // LD A,[BC]
            table[0x0B] = Decrement(Registers.BC);                           // DEC BC
            table[0x0C] = Increment(Registers.BC.Low);                       // INC C
            table[0x0D] = Decrement(Registers.BC.Low);                       // DEC C
            table[0x0E] = Load(Registers.BC.Low, regPC.ByteRef(1), 2);       // LD C,*
            table[0x0F] = RotateRightCarry(regAF.A, false);                  // RRCA

            table[0x10] = DecrementJumpIfZero(Registers.BC.High, regPC.ByteRef(1));  // DJNZ *
            table[0x11] = Load(Registers.DE, regPC.WordRef(1), 3);           // LD DE,**
            table[0x12] = Load(Registers.DE.ByteRef(), regAF.A, 1);          // LD [DE],A
            table[0x13] = Increment(Registers.DE);                           // INC DE
            table[0x14] = Increment(Registers.DE.High);                      // INC D
            table[0x15] = Decrement(Registers.DE.High);                      // DEC D
            table[0x16] = Load(Registers.DE.High, regPC.ByteRef(1), 2);      // LD D,*
            table[0x17] = RotateLeft(regAF.A, false);                        // RLA
            table[0x18] = JumpRelative(regPC.ByteRef(1));                    // JR *
            table[0x19] = Add(Registers.HL, Registers.DE);                   // ADD HL,DE
            table[0x1A] = Load(regAF.A, Registers.DE.ByteRef(), 1);          // LD A,[DE]
            table[0x1B] = Decrement(Registers.DE);                           // DEC DE
            table[0x1C] = Increment(Registers.DE.Low);                       // INC E
            table[0x1D] = Decrement(Registers.DE.Low);                       // DEC E
            table[0x1E] = Load(Registers.DE.Low, regPC.ByteRef(1), 2);       // LD E,*
            table[0x1F] = RotateRight(regAF.A, false);                       // RRA

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
            table[0x76] = Halt();                                            // HALT
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
            table[0xC2] = Jump(regPC, regPC.WordRef(1),()=>!Flags.Zero);     // JP NZ,**
            table[0xC3] = Jump(regPC, regPC.WordRef(1),()=>true);            // JP **
            table[0xC4] = Call(regSP, regPC, () => !Flags.Zero);             // CALL NZ,**
            table[0xC5] = Push(regSP, Registers.BC);                         // PUSH BC
            table[0xC6] = Add(regAF.A, regPC.ByteRef(1), 2);                 // ADD A,*
            table[0xC7] = Reset(regSP, regPC, 0);                            // RST 0x00
            table[0xC8] = Ret(regSP, regPC, () => Flags.Zero);               // RET Z
            table[0xC9] = Ret(regSP, regPC, () => true);                     // RET
            table[0xCA] = Jump(regPC, regPC.WordRef(1),()=>Flags.Zero);      // JP Z,**
            table[0xCB] = Bits(regPC); //  BITS
            table[0xCC] = Call(regSP, regPC, () => Flags.Zero);              // CALL Z,**
            table[0xCD] = Call(regSP, regPC, () => true);                    // CALL **
            table[0xCE] = Adc(regAF.A, regPC.ByteRef(1), 2);                 // ADC A,*
            table[0xCF] = Reset(regSP, regPC, 0x08);                         // RST 0x08

            table[0xD0] = Ret(regSP, regPC, () => !Flags.Carry);             // RET NC
            table[0xD1] = Pop(regSP, Registers.DE);                          // POP DE
            table[0xD2] = Jump(regPC, regPC.WordRef(1), ()=>!Flags.Carry);    // JP NC,**
            table[0xD3] = Out(regAF.A, regPC.ByteRef(1));                    // OUT [*],A
            table[0xD4] = Call(regSP, regPC, () => !Flags.Carry);            // CALL NC,**
            table[0xD5] = Push(regSP, Registers.DE);                         // PUSH DE
            table[0xD6] = Sub(regAF.A, regPC.ByteRef(1), 2);                 // SUB *
            table[0xD7] = Reset(regSP, regPC, 0x10);                         // RST 0x10
            table[0xD8] = Ret(regSP, regPC, () => Flags.Carry);              // RET C
            table[0xD9] = Exx();                                             // EXX
            table[0xDA] = Jump(regPC, regPC.WordRef(1), ()=>Flags.Carry);    // JP C,**
            table[0xDB] = In(regAF.A, regPC.ByteRef(1), false);              // IN A,[*]
            table[0xDC] = Call(regSP, regPC, () => Flags.Carry);             // CALL C,**
            table[0xDD] = null;  // IX
            table[0xDE] = Sbc(regAF.A, regPC.ByteRef(1), 2);                 // SBC A,*
            table[0xDF] = Reset(regSP, regPC, 0x18);                         // RST 0x18

            table[0xE0] = Ret(regSP, regPC, () => !Flags.Parity);            // RET PO
            table[0xE1] = Pop(regSP, Registers.HL);                          // POP HL
            table[0xE2] = Jump(regPC, regPC.WordRef(1), ()=>!Flags.Parity);  // JP PO,**
            table[0xE3] = Exchange(regSP.WordRef(), Registers.HL);           // EX [SP],HL
            table[0xE4] = Call(regSP, regPC, () => !Flags.Parity);           // CALL PO,**
            table[0xE5] = Push(regSP, Registers.HL);                         // PUSH HL
            table[0xE6] = And(regAF.A, regPC.ByteRef(1), 2);                 // AND *
            table[0xE7] = Reset(regSP, regPC, 0x20);                         // RST 0x20
            table[0xE8] = Ret(regSP, regPC, () => Flags.Parity);             // RET PE
            table[0xE9] = Jump(regPC, Registers.HL.WordRef(1),()=>true);     // JP [HL]
            table[0xEA] = Jump(regPC, regPC.WordRef(1),() => Flags.Parity);  // JP PE,**
            table[0xEB] = Exchange(Registers.DE, Registers.HL);              // EX DE,HL
            table[0xEC] = Call(regSP, regPC, () => Flags.Parity);            // CALL PE,**
            table[0xED] = Extended(regPC);
            table[0xEE] = Xor(regAF.A, regPC.ByteRef(1), 2);                 // XOR *
            table[0xEF] = Reset(regSP, regPC, 0x28);                         // RST 0x28

            table[0xF0] = Ret(regSP, regPC, () => !Flags.Sign);              // RET P
            table[0xF1] = Pop(regSP, regAF);                                 // POP AF
            table[0xF2] = Jump(regPC, regPC.WordRef(1), ()=>!Flags.Sign);    // JP P,**
            table[0xF3] = DisableInterrupts();                               // DI
            table[0xF4] = Call(regSP, regPC, () => !Flags.Sign);             // CALL P,**
            table[0xF5] = Push(regSP, regAF);                                // PUSH AF
            table[0xF6] = Or(regAF.A, regPC.ByteRef(1), 2);                  // OR *
            table[0xF7] = Reset(regSP, regPC, 0x30);                         // RST 0x30
            table[0xF8] = Ret(regSP, regPC, () => Flags.Sign);               // RET M
            table[0xF9] = Load(regSP, Registers.HL, 1);                      // LD SP,HL
            table[0xFA] = Jump(regPC, regPC.WordRef(1),() => Flags.Sign);    // JP M,**
            table[0xFB] = EnableInterrupts();                                // EI
            table[0xFC] = Call(regSP, regPC, () => Flags.Sign);              // CALL M,**
            table[0xFD] = null;  // IY
            table[0xFE] = Cp(regAF.A, regPC.ByteRef(1), 2);                  // CP *
            table[0xFF] = Reset(regSP, regPC, 0x38);                         // RST 0x38
        }

        public FlagsRegister Flags => this.regAF.F;

        public void Run(Memory memory)
        {
            while (true)
            {
                Dump();
                var instruction = memory.ReadByte(regPC.Value);
                if (instruction == 0x76 && IFF1 == false) 
                {
                    return; // halt breaks execution if interrupts are disabled
                }

                var offset = table[instruction](memory);
                this.regPC.Value += offset;
                this.regR.Increment(); // hack to get some value
                this.CheckInterrupt(memory);
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
            Console.Write($"I={regI.Value} ");
            Console.Write($"R={regR.Value} ");
            Console.WriteLine();
        }

        private void CheckInterrupt(Memory m)
        {
            var now = DateTime.Now;
            var diff = (now - previousInterrupt);
            if (diff < TimeSpan.FromMilliseconds(50))
                return;

            previousInterrupt = now;
            if (!IFF1)
            {
                return; // interrupts disabled
            }

            IFF1 = IFF2 = false;
            switch(InterruptMode)
            {
                case 0:
                case 1:
                    Reset(regSP, regPC, 0x38)(m);
                    break;
                case 2:
                    var offset = m.ReadWord((word)(regI.Value << 8));
                    Reset(regSP, regPC, offset)(m);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private Handler Halt()
        {
            return m =>
            {
                return 0;
            };
        }

        private Handler Nop()
        {
            return m => 
            {
                return 1;
            };
        }

        private Handler Load<T>(IPointerReference<T> dst, IReference<T> src, word size)
        {
            return m =>
            {
                dst.Get(m).Write(m, src.Read(m));
                return size;
            };
        }

        private Handler Load<T>(IReference<T> dst, IPointerReference<T> src, word size)
        {
            return m =>
            {
                // 10 t-states
                dst.Write(m, src.Get(m).Read(m));// flags not affected
                return size;
            };
        }

        private Handler Load<T>(IReference<T> dst, IReference<T> src, word size)
        {
            return m =>
            {
                // 7 t-states
                dst.Write(m, src.Read(m));
                return size;
            };
        }

        private Handler LoadIR(IReference<byte> dst, IReference<byte> src)
        {
            // special flags-affecting extended version of load used by I and R registers;
            return m =>
            {
                var v = src.Read(m);
                var f = Flags;
                f.Sign = v > 0x7F;
                f.Zero = v == 0;
                f.HalfCarry = false;
                f.ParityOverflow = IFF2;
                f.AddSub = false;
                dst.Write(m, v);
                return 2;
            };

        }

        private Handler BlockLoad(WordRegister dst, WordRegister src, WordRegister counter, BlockMode mode)
        {
            return m =>
            {
                var data = m.ReadByte(src.Value);
                m.WriteByte(dst.Value, data);

                counter.Decrement();
                if (mode.HasFlag(BlockMode.Increment))
                {
                    src.Increment();
                    dst.Increment();
                }
                else
                {
                    src.Decrement();
                    dst.Decrement();
                }

                Flags.HalfCarry = false;
                Flags.ParityOverflow = counter.Value != 0;
                Flags.AddSub = false;
                if (!mode.HasFlag(BlockMode.Repeat) || Flags.ParityOverflow) 
                    return 2;
                return 0;
            };
        }

        private Handler BlockCompare(ByteRegister a, WordRegister hl, WordRegister counter, BlockMode mode)
        {
            return m =>
            {
                var v1 = m.ReadByte(hl.Value);
                counter.Decrement();
                if (mode.HasFlag(BlockMode.Increment))
                    hl.Increment();
                else
                    hl.Decrement();

                var v2 = a.Value;
                var f = Flags;
                byte res = (byte)(v1 - v2);
                f.Sign = res > 0x7F;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfBorrow(v1, v2);
                f.ParityOverflow = counter.Value != 0;
                f.AddSub = true;
                if (!mode.HasFlag(BlockMode.Repeat) || Flags.ParityOverflow) 
                    return 2;
                return 0;
            };
        }

        private Handler BlockOutput(WordRegister hl, WordRegister bc, BlockMode mode)
        {
            return m =>
            {
                var device = Port.Get(bc.Low.Value);
                var v = m.ReadByte(hl.Value);
                device.Write(v);
                if (mode.HasFlag(BlockMode.Increment))
                    hl.Increment();
                else
                    hl.Decrement();
                bc.High.Decrement();
                var f = Flags;
                f.Zero = bc.High.Value == 0;
                f.AddSub = true;
                if (!mode.HasFlag(BlockMode.Repeat) || Flags.Zero) 
                    return 2;
                return 0;
            };
        }

        private Handler BlockInput(WordRegister hl, WordRegister bc, BlockMode mode)
        {
            return m =>
            {
                var device = Port.Get(bc.Low.Value);
                m.WriteByte(hl.Value, device.Read());
                if (mode.HasFlag(BlockMode.Increment))
                    hl.Increment();
                else
                    hl.Decrement();
                bc.High.Decrement();
                var f = Flags;
                f.Zero = bc.High.Value == 0;
                f.AddSub = true;
                if (!mode.HasFlag(BlockMode.Repeat) || Flags.Zero) 
                    return 2;
                return 0;
            };
        }

        private Handler RotateDigitRight(ByteRegister regA, WordRegister hl)
        {
            return m =>
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

                regA.Value = a;
                m.WriteByte(hl.Value, h);
                return 2;
            };
        }

        private Handler RotateDigitLeft(ByteRegister regA, WordRegister hl)
        {
            return m =>
            {
                byte h = m.ReadByte(hl.Value);
                byte a = regA.Value;

                byte loa = (byte)(a & 0x0F);
                byte hia = (byte)(a & 0xF0);

                h = (byte)((h << 4) | loa);
                a = (byte)(hia | (h >> 4));

                var f = Flags;
                f.Sign = a > 0x7F;
                f.Zero = a == 0;
                f.HalfCarry = false;
                f.ParityOverflow = EvenParity(a);
                f.AddSub = false;

                regA.Value = a;
                m.WriteByte(hl.Value, h);
                return 2;
            };
        }

        private Handler RotateLeft(IReference<byte> reg, bool extended)
        {
            return (Memory m) =>
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
                reg.Write(m, value);
                return 1;
            };
        }

        private Handler RotateRight(IReference<byte> reg, bool extended)
        {
            return (Memory m) =>
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
                reg.Write(m, value);
                return 1;
            };
        }

        private Handler RotateLeftCarry(IReference<byte> reg, bool extended)
        {
            return (Memory m) =>
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
                reg.Write(m, value);
                return 1;
            };
        }

        private Handler RotateRightCarry(IReference<byte> reg, bool extended)
        {
            return (Memory m) =>
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
                reg.Write(m, value);
                return 1;
            };
        }

        private Handler ShiftLeft(IReference<byte> reg, byte lowestBit)
        {
            return m =>
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
                reg.Write(m, value);
                return 2;
            };
        }

        private Handler ShiftRight(IReference<byte> reg, bool keepHighBit)
        {
            return m =>
            {
                byte value = reg.Read(m);

                bool carry = (value & 1) != 0;
                byte high = keepHighBit ? (byte)(value & 0x80) : (byte)0;
                value >>= 1;
                value |= high;
                FlagsRegister f = this.Flags;

                f.Sign = value > 0x7F;
                f.Zero = value == 0;
                f.ParityOverflow = EvenParity(value);
                f.Carry = carry;
                f.AddSub = false;
                f.HalfCarry = false;
                reg.Write(m, value);
                return 2;
            };
        }
        private Handler In(ByteRegister dst, IReference<byte> portRef, bool extended = true)
        {
            return m =>
            {
                var portNumber = portRef.Read(m);
                var device = Port.Get(portNumber);
                var value = device.Read();
                if (extended)
                {
                    // affect flags
                    var f = Flags;
                    f.Sign = value > 0x7F;
                    f.Zero = value == 0;
                    f.HalfCarry = false;
                    f.ParityOverflow = EvenParity(value);
                    f.AddSub = false;
                }
                if(dst != null)
                    dst.Value = value;
                
                if(extended)
                    return 2;
                return 1;
            };
        }

        private Handler Out(ByteRegister src, IReference<byte> portRef, word size = 1)
        {
            return m =>
            {
                var portNumber = portRef.Read(m);
                var device = Port.Get(portNumber);
                device.Write(src?.Value ?? 0);
                return size;
            };
        }

        private Handler SetInterruptMode(int mode)
        {
            return m =>
            {
                this.InterruptMode = mode;
                return 2;
            };
        }

        private Handler EnableInterrupts()
        {
            return m =>
            {
                this.IFF1 = true;
                this.IFF2 = true;
                return 1;
            };
        }

        private Handler DisableInterrupts()
        {
            return m =>
            {
                this.IFF1 = false;
                this.IFF2 = false;
                return 1;
            };
        }

        private Handler Exx()
        {
            return m =>
            {
                var bc = Registers.BC.Value;
                var de = Registers.DE.Value;
                var hl = Registers.HL.Value;

                Registers.BC.Value = RegistersCopy.BC.Value;
                Registers.DE.Value = RegistersCopy.DE.Value;
                Registers.HL.Value = RegistersCopy.HL.Value;

                RegistersCopy.BC.Value = bc;
                RegistersCopy.DE.Value = de;
                RegistersCopy.HL.Value = hl;

                return 1;
            };
        }

        private Handler Exchange(IReference<word> reg1, IReference<word> reg2)
        {
            // flags not affected
            return m =>
            {
                // 4 t-states
                var t = reg1.Read(m);
                reg1.Write(m, reg2.Read(m));
                reg2.Write(m, t);
                return 1;
            };
        }

        private Handler Increment(WordRegister reg)
        {
            return m => 
            {
                reg.Increment(); // flags not affected
                return 1;
            };
        }

        private Handler Decrement(WordRegister reg)
        {
            return m => 
            {
                reg.Decrement(); // flags not affected
                return 1;
            };
        }

        private Handler Increment(IReference<byte> byteref)
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

        private Handler Decrement(IReference<byte> byteref)
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

        private Handler DecrementJumpIfZero(ByteRegister reg, IReference<byte> distance)
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

        private Handler JumpRelative(IReference<byte> distance, Func<bool> p)
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

        private Handler JumpRelative(IReference<byte> distance)
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

        private Handler Jump(WordRegister pc, IReference<word> newpc, Func<bool> p)
        {
            return m =>
            {
                if (p())
                {
                    pc.Value = newpc.Read(m);
                    return 0;
                }
                return 3;
            };
        }

        private Handler Push(WordRegister sp, WordRegister reg)
        {
            return m =>
            {
                sp.Value -= 2;
                m.WriteWord(sp.Value, reg.Value);
                return 1;
            };
        }

        private Handler Pop(WordRegister sp, WordRegister reg)
        {
            return m =>
            {
                reg.Value = m.ReadWord(sp.Value);
                sp.Value += 2;
                return 1;
            };
        }

        private Handler Reset(WordRegister sp, WordRegister pc, word offset)
        {
            return m =>
            {
                sp.Value -= 2;
                m.WriteWord(sp.Value, (word)(pc.Value + 1));
                pc.Value = offset;
                return 0;
            };
        }

        private Handler Call(WordRegister sp, WordRegister pc, Func<bool> p)
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

        private Handler Ret(WordRegister sp, WordRegister pc, Func<bool> p)
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

        private Handler Retn(WordRegister sp, WordRegister pc)
        {
            return m =>
            {
                regPC.Value = m.ReadWord(sp.Value);
                sp.Value += 2;
                IFF1 = IFF2;
                return 0;
            };
        }

        private Handler Cp(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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
                return sz;
            };
        }

        private Handler Or(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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
                return sz;
            };
        }

        private Handler Xor(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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
                return sz;
            };
        }

        private Handler And(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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
                return sz;
            };
        }

        private Handler Sbc(IReference<word> dst, IReference<word> src)
        {
            return m =>
            {
                var f = this.Flags;
                var v1 = dst.Read(m);
                var v2 = src.Read(m);
                byte v3 = f.Carry ? (byte)1 : (byte)0;
                word res = (word)(v1 - v2 - v3);
                f.Sign = res > 0x7FFF;
                f.Zero = res == 0;
                f.HalfCarry = IsHalfBorrow((byte)(v1 >> 8), (byte)(v2 >> 8), v3);
                f.ParityOverflow = IsUnderflow(v1, v2, res);
                f.AddSub = true;
                f.Carry = v1 < v2 + v3;
                dst.Write(m, res);
                return 2;
            };
        }

        private Handler Sbc(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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
                return sz;
            };
        }

        private Handler Sub(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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
                return sz;
            };
        }
        
        private Handler Neg(ByteRegister a)
        {
            return m =>
            {
                var f = this.Flags;
                byte res = (byte)(0 - a.Value);
                f.Sign = (res & 0x80) != 0;
                f.Zero = res == 0;
                f.HalfCarry = false;
                f.ParityOverflow = (a.Value == 0x80);
                f.AddSub = true;
                f.Carry = (a.Value != 0);
                a.Value = res;
                return 2;
            };
        }

        private Handler Adc(IReference<word> dst, IReference<word> src, byte sz = 1)
        {
            return m =>
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
                dst.Write(m, res);
                return sz;
            };
        }

        private Handler Adc(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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

        private Handler Add(IReference<byte> dst, IReference<byte> src, byte sz = 1)
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

        private Handler Add(WordRegister dst, WordRegister src) // add src to dst
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

        private Handler BinaryCodedDecimalCorrection(ByteRegister reg)
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

        private Handler InvertCarryFlag()
        {
            return m =>
            {
                Flags.HalfCarry = Flags.Carry;
                Flags.AddSub = false;
                Flags.Carry = !Flags.Carry;
                return 1;
            };
        }

        private Handler SetCarryFlag()
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

        private Handler Invert(ByteRegister reg)
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

        private Handler Bits(WordRegister pc)
        {
            var lookup = new IReference<byte>[]
            {
                Registers.BC.High,
                Registers.BC.Low,
                Registers.DE.High,
                Registers.DE.Low,
                Registers.HL.High,
                Registers.HL.Low,
                Registers.HL.ByteRef(),
                regAF.A
            };

            return m =>
            {
                var ext = m.ReadByte((word)(pc.Value + 1));

                if (ext >= 0xC0) // set
                {
                    var bitToTest = (ext >> 3) & 7;
                    var reg = lookup[ext & 7];
                    var v = reg.Read(m);

                    var res = v | (1 << bitToTest);
                    reg.Write(m, (byte)res);
                }
                else if (ext >= 0x80) // reset
                {
                    var bitToTest = (ext >> 3) & 7;
                    var reg = lookup[ext & 7];
                    var v = reg.Read(m);

                    var res = v & ~(1 << bitToTest);
                    reg.Write(m, (byte)res);
                }
                else if (ext >= 0x40) // test
                {
                    var bitToTest = (ext >> 3) & 7;
                    var reg = lookup[ext & 7];
                    var v = reg.Read(m);
                    var res = v & (1 << bitToTest);

                    var f = Flags;
                    f.Sign = res > 0x7F;
                    f.Zero = res == 0;
                    f.HalfCarry = true;
                    f.ParityOverflow = res == 0;
                    f.AddSub = false;
                }
                else
                {
                    var reg = lookup[ext & 7];

                    if(ext >= 0x38) //srl
                    {
                        ShiftRight(reg, false)(m);
                    }
                    else if(ext >= 0x30) //sll
                    {
                        ShiftLeft(reg, 1)(m);
                    }
                    else if(ext >= 0x28) //sra
                    {
                        ShiftRight(reg, true)(m);
                    }
                    else if(ext >= 0x20) //sla
                    {
                        ShiftLeft(reg, 0)(m);
                    }
                    else if(ext >= 0x18) //rr
                    {
                        RotateRight(reg, true)(m);
                    }
                    else if(ext >= 0x10) //rl
                    {
                        RotateLeft(reg, true)(m);
                    }
                    else if(ext >= 0x08) //rrc
                    {
                        RotateRightCarry(reg, true)(m);
                    }
                    else if(ext >= 0) //rlc
                    {
                        RotateLeftCarry(reg, true)(m);
                    }
                }

                return 2;
            };
        }

        private Handler Extended(WordRegister pc)
        {
            return m =>
            {
                var ext = m.ReadByte((word)(pc.Value + 1));
                switch (ext)
                {
                    case 0x40: return In(Registers.BC.High, Registers.BC.Low)(m); // IN B,(C)
                    case 0x50: return In(Registers.DE.High, Registers.BC.Low)(m); // IN D,(C)
                    case 0x60: return In(Registers.HL.High, Registers.BC.Low)(m); // IN H,(C)
                    case 0x70: return In(null, Registers.BC.Low)(m); // IN (C)

                    case 0x41: return Out(Registers.BC.High, Registers.BC.Low, 2)(m); // OUT (C),B
                    case 0x51: return Out(Registers.DE.High, Registers.BC.Low, 2)(m); // OUT (C),D
                    case 0x61: return Out(Registers.HL.High, Registers.BC.Low, 2)(m); // OUT (C),H
                    case 0x71: return Out(null, Registers.BC.Low, 2)(m); // OUT (C),0

                    case 0x42: return Sbc(Registers.HL, Registers.BC)(m); // SBC HL,BC
                    case 0x52: return Sbc(Registers.HL, Registers.DE)(m); // SBC HL,DE
                    case 0x62: return Sbc(Registers.HL, Registers.HL)(m); // SBC HL,HL
                    case 0x72: return Sbc(Registers.HL, regSP)(m); // SBC HL,SP

                    case 0x43: return Load(regPC.AsWordPtr(1), Registers.BC, 4)(m); // LD [**],BC
                    case 0x53: return Load(regPC.AsWordPtr(1), Registers.DE, 4)(m); // LD [**],DE
                    case 0x63: return Load(regPC.AsWordPtr(1), Registers.HL, 4)(m); // LD [**],HL
                    case 0x73: return Load(regPC.AsWordPtr(1), regSP, 4)(m); // LD [**],SP

                    case 0x44:
                    case 0x54:
                    case 0x64:
                    case 0x74:
                    case 0x4C:
                    case 0x5C:
                    case 0x6C:
                    case 0x7C:
                        return Neg(regAF.A)(m); // NEG

                    case 0x45:
                    case 0x55:
                    case 0x65:
                    case 0x75:
                    case 0x4D: // should be RETI, but external hw not emulated
                    case 0x5D:
                    case 0x6D:
                    case 0x7D:
                        return Retn(regSP, regPC)(m); // RETN

                    case 0x46:
                    case 0x4E: // undefined
                    case 0x66:
                    case 0x6E: // undefined
                        return SetInterruptMode(0)(m); // IM 0

                    case 0x56:
                    case 0x76:
                        return SetInterruptMode(1)(m); // IM 1
                    
                    case 0x5E:
                    case 0x7E:
                        return SetInterruptMode(2)(m); // IM 2

                    case 0x47:
                        return Load(regI, regAF.A, 2)(m); // LD I,A
                    case 0x57:
                        return LoadIR(regAF.A, regI)(m); // LD A,I
                    case 0x4F:
                        return Load(regR, regAF.A, 2)(m); // LD R,A
                    case 0x5F:
                        return LoadIR(regAF.A, regR)(m); // LD A,R

                    case 0x67:
                        return RotateDigitRight(regAF.A, Registers.HL)(m);
                    case 0x6F:
                        return RotateDigitLeft(regAF.A, Registers.HL)(m);

                    case 0x48: return In(Registers.BC.Low, Registers.BC.Low)(m); // IN C,(C)
                    case 0x58: return In(Registers.DE.Low, Registers.BC.Low)(m); // IN E,(C)
                    case 0x68: return In(Registers.HL.Low, Registers.BC.Low)(m); // IN L,(C)
                    case 0x78: return In(regAF.High, Registers.BC.Low)(m); // IN A, (C)

                    case 0x49: return Out(Registers.BC.Low, Registers.BC.Low, 2)(m); // OUT (C),C
                    case 0x59: return Out(Registers.DE.Low, Registers.BC.Low, 2)(m); // OUT (C),E
                    case 0x69: return Out(Registers.HL.Low, Registers.BC.Low, 2)(m); // OUT (C),L
                    case 0x79: return Out(regAF.High, Registers.BC.Low, 2)(m); // OUT (C),A

                    case 0x4A: return Adc(Registers.HL, Registers.BC)(m); // ADC HL,BC
                    case 0x5A: return Adc(Registers.HL, Registers.DE)(m); // ADC HL,DE
                    case 0x6A: return Adc(Registers.HL, Registers.HL)(m); // ADC HL,HL
                    case 0x7A: return Adc(Registers.HL, regSP)(m); // ADC HL,SP

                    case 0x4B: return Load(Registers.BC, regPC.AsWordPtr(1), 4)(m); // LD BC,[**]
                    case 0x5B: return Load(Registers.DE, regPC.AsWordPtr(1), 4)(m); // LD DE,[**]
                    case 0x6B: return Load(Registers.HL, regPC.AsWordPtr(1), 4)(m); // LD HL,[**]
                    case 0x7B: return Load(regSP, regPC.AsWordPtr(1), 4)(m); // LD SP,[**]

                    case 0xA0: return BlockLoad(Registers.DE, Registers.HL, Registers.BC, BlockMode.IO)(m); // LDI
                    case 0xA1: return BlockCompare(regAF.A, Registers.HL, Registers.BC, BlockMode.IO)(m); // CPI
                    case 0xA2: return BlockInput(Registers.HL, Registers.BC, BlockMode.IO)(m); // INI
                    case 0xA3: return BlockOutput(Registers.HL, Registers.BC, BlockMode.IO)(m); // OUTI

                    case 0xA8: return BlockLoad(Registers.DE, Registers.HL, Registers.BC, BlockMode.DO)(m); // LDD
                    case 0xA9: return BlockCompare(regAF.A, Registers.HL, Registers.BC, BlockMode.DO)(m); // CPD
                    case 0xAA: return BlockInput(Registers.HL, Registers.BC, BlockMode.DO)(m); // IND
                    case 0xAB: return BlockOutput(Registers.HL, Registers.BC, BlockMode.DO)(m); // OUTD

                    case 0xB0: return BlockLoad(Registers.DE, Registers.HL, Registers.BC, BlockMode.IR)(m); // LDIR
                    case 0xB1: return BlockCompare(regAF.A, Registers.HL, Registers.BC, BlockMode.IR)(m);// CPIR
                    case 0xB2: return BlockInput(Registers.HL, Registers.BC, BlockMode.IR)(m); // INI
                    case 0xB3: return BlockOutput(Registers.HL, Registers.BC, BlockMode.IR)(m); // OTIR

                    case 0xB8: return BlockLoad(Registers.DE, Registers.HL, Registers.BC, BlockMode.DR)(m); // LDDR
                    case 0xB9: return BlockCompare(regAF.A, Registers.HL, Registers.BC, BlockMode.DR)(m); // CPDR
                    case 0xBA: return BlockInput(Registers.HL, Registers.BC, BlockMode.DR)(m); // INDR
                    case 0xBB: return BlockOutput(Registers.HL, Registers.BC, BlockMode.DR)(m); // OTDR
                }

                return 2;
            };
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

        [Flags]
        private enum BlockMode
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
    }
}
