using System;
using System.Linq;

namespace z80emu
{
    class InstructionTable
    {
        private readonly CPU cpu;
        public InstructionTable(CPU cpu)
        {
            this.cpu = cpu;
        }

        public IInstruction BuildTable()
        {
            var PORT = cpu.Port;
            var PC = cpu.regPC;
            var SP = cpu.regSP;
            var IX = cpu.regIX;
            var IY = cpu.regIY;
            var IXH = IX.High;
            var IXL = IX.Low;
            var IYH = IY.High;
            var IYL = IY.Low;
            
            var AF = cpu.regAF;
            var BC = cpu.Registers.BC;
            var DE = cpu.Registers.DE;
            var HL = cpu.Registers.HL;
            var I = cpu.regI;
            var R = cpu.regR;
            var A = cpu.regAF.A;
            var F = cpu.regAF.F;

            var B = BC.High;
            var C = BC.Low;
            var D = DE.High;
            var E = DE.Low;
            var H = HL.High;
            var L = HL.Low;

            var AFx = cpu.regAFx;
            var BCx = cpu.RegistersCopy.BC;
            var DEx = cpu.RegistersCopy.DE;
            var HLx = cpu.RegistersCopy.HL;
            var IMMW = PC.WordRef(1); // immediate word
            var IMMB = PC.ByteRef(1); // immediate byte
            var IMMW2= PC.WordRef(2); // immediate word (2 bytes offset)
            var IMMB2= PC.ByteRef(2); // immediate byte (2 bytes offset)
            var IMMB3= PC.ByteRef(3); // immediate byte (3 bytes offset)

            var IMMBP = PC.AsBytePtr(1); // immediate byte ptr
            var IMMWP = PC.AsWordPtr(1); // immediate word ptr
            var IMMWP2 = PC.AsWordPtr(2); // immediate word ptr (2 bytes offset)

            var BCBP = BC.ByteRef(); // BC byte ptr
            var BCWP = BC.WordRef(); // BC word ptr

            var DEBP = DE.ByteRef(); // DE byte ptr
            var DEWP = DE.WordRef(); // DE word ptr

            var HLBP = HL.ByteRef(); // HL byte ptr
            var HLWP = HL.WordRef(); // HL word ptr

            var SPWP = SP.WordRef(); // SP word ptr
            var IXIMM = IX.ByteRef(IMMB2); // [IX+*]
            var IYIMM = IY.ByteRef(IMMB2); // [IY+*]

            // fallback for cases like invalid sequences: DD DD, DD ED, DD FD etc
            IInstruction fallback = New().Time(4).Size(1).Nop().Label("Fallback 1");

            // fallback for undefined ED instructions - 8 t-states, pc+1, r+2
            IInstruction extendedNop = New().Time(8).Size(1).Nop().Label("Fallback 2");

            IInstruction[] table = new IInstruction[0x100];
            IInstruction[] extended = new IInstruction[0x100];
            IInstruction[] lookupIX = new IInstruction[0x100];
            IInstruction[] lookupIY = new IInstruction[0x100];
            IInstruction[] bits = new IInstruction[0x100];
            IInstruction[] exBitsIX = new IInstruction[0x100];
            IInstruction[] exBitsIY = new IInstruction[0x100];

            table[0x00] = New().Time(4).Size(1).Nop().Label("NOP");
            table[0x01] = New().Time(10).Size(3).Load(BC, IMMW).Label("LD BC,**");
            table[0x02] = New().Time(7).Size(1).Load(BCBP, A).Label("LD [BC],A");
            table[0x03] = New().Time(6).Size(1).Increment(BC).Label("INC BC ");
            table[0x04] = New().Time(4).Size(1).Increment(B).Label("INC B");
            table[0x05] = New().Time(4).Size(1).Decrement(B).Label("DEC B");
            table[0x06] = New().Time(7).Size(2).Load(B, IMMB).Label("LD B,*");
            table[0x07] = New().Time(4).Size(1).RotateLeftCarry(A, null, false).Label("RLCA");
            table[0x08] = New().Time(4).Size(1).Exchange(AF, AFx).Label("EX AF,AF'");
            table[0x09] = New().Time(11).Size(1).Add(HL, BC).Label("ADD HL,BC");
            table[0x0A] = New().Time(7).Size(1).Load(A, BCBP).Label("LD A,[BC]");
            table[0x0B] = New().Time(6).Size(1).Decrement(BC).Label("DEC BC");
            table[0x0C] = New().Time(4).Size(1).Increment(C).Label("INC C");
            table[0x0D] = New().Time(4).Size(1).Decrement(C).Label("DEC C");
            table[0x0E] = New().Time(7).Size(2).Load(C, IMMB).Label("LD C,*");
            table[0x0F] = New().Time(4).Size(1).RotateRightCarry(A, null, false).Label("RRCA");

            table[0x10] = New().Time(13,8).Size(2).DecrementJumpIfZero(B, IMMB).Label("DJNZ *");
            table[0x11] = New().Time(10).Size(3).Load(DE, IMMW).Label("LD DE,**");
            table[0x12] = New().Time(7).Size(1).Load(DEBP, A).Label("LD [DE],A");
            table[0x13] = New().Time(6).Size(1).Increment(DE).Label("INC DE");
            table[0x14] = New().Time(4).Size(1).Increment(D).Label("INC D");
            table[0x15] = New().Time(4).Size(1).Decrement(D).Label("DEC D");
            table[0x16] = New().Time(7).Size(2).Load(D, IMMB).Label("LD D,*");
            table[0x17] = New().Time(4).Size(1).RotateLeft(A, null, false).Label("RLA");
            table[0x18] = New().Time(12).Size(2).JumpRelative(IMMB).Label("JR *");
            table[0x19] = New().Time(11).Size(1).Add(HL, DE).Label("ADD HL,DE");
            table[0x1A] = New().Time(7).Size(1).Load(A, DEBP).Label("LD A,[DE]");
            table[0x1B] = New().Time(6).Size(1).Decrement(DE).Label("DEC DE");
            table[0x1C] = New().Time(4).Size(1).Increment(E).Label("INC E");
            table[0x1D] = New().Time(4).Size(1).Decrement(E).Label("DEC E");
            table[0x1E] = New().Time(7).Size(2).Load(E, IMMB).Label("LD E,*");
            table[0x1F] = New().Time(4).Size(1).RotateRight(A, null, false).Label("RRA");

            table[0x20] = New().Time(12,7).Size(2).JumpRelative(IMMB, () => !F.Zero).Label("JR NZ,*");
            table[0x21] = New().Time(10).Size(3).Load(HL, IMMW).Label("LD HL,**");
            table[0x22] = New().Time(16).Size(3).Load(IMMWP, HL).Label("LD [**],HL");
            table[0x23] = New().Time(6).Size(1).Increment(HL).Label("INC HL");
            table[0x24] = New().Time(4).Size(1).Increment(H).Label("INC H");
            table[0x25] = New().Time(4).Size(1).Decrement(H).Label("DEC H");
            table[0x26] = New().Time(7).Size(2).Load(H, IMMB).Label("LD H,*");
            table[0x27] = New().Time(4).Size(1).BinaryCodedDecimalCorrection(A).Label("DAA");
            table[0x28] = New().Time(12,7).Size(2).JumpRelative(IMMB, () => F.Zero).Label("JR Z,*");
            table[0x29] = New().Time(11).Size(1).Add(HL, HL).Label("ADD HL,HL");
            table[0x2A] = New().Time(16).Size(3).Load(HL, IMMWP).Label("LD HL,[**]");
            table[0x2B] = New().Time(6).Size(1).Decrement(HL).Label("DEC HL");
            table[0x2C] = New().Time(4).Size(1).Increment(L).Label("INC L");
            table[0x2D] = New().Time(4).Size(1).Decrement(L).Label("DEC L");
            table[0x2E] = New().Time(7).Size(2).Load(L, IMMB).Label("LD L,*");
            table[0x2F] = New().Time(4).Size(1).Invert(A).Label("CPL");
            
            table[0x30] = New().Time(12,7).Size(2).JumpRelative(IMMB, () => !F.Carry).Label("JR NC,*");
            table[0x31] = New().Time(10).Size(3).Load(SP, IMMW).Label("LD SP,**");
            table[0x32] = New().Time(13).Size(3).Load(IMMBP, A).Label("LD [**],A");
            table[0x33] = New().Time(6).Size(1).Increment(SP).Label("INC SP");
            table[0x34] = New().Time(11).Size(1).Increment(HLBP).Label("INC [HL]");
            table[0x35] = New().Time(11).Size(1).Decrement(HLBP).Label("DEC [HL]");
            table[0x36] = New().Time(10).Size(2).Load(HLBP, IMMB).Label("LD [HL],*");
            table[0x37] = New().Time(4).Size(1).SetCarryFlag().Label("SCF");
            table[0x38] = New().Time(12,7).Size(2).JumpRelative(IMMB, () => F.Carry).Label("JR C,*");
            table[0x39] = New().Time(11).Size(1).Add(HL, SP).Label("ADD HL,SP");
            table[0x3A] = New().Time(13).Size(3).Load(A, IMMBP).Label("LD A,[**]");
            table[0x3B] = New().Time(6).Size(1).Decrement(SP).Label("DEC SP");
            table[0x3C] = New().Time(4).Size(1).Increment(A).Label("INC A");
            table[0x3D] = New().Time(4).Size(1).Decrement(A).Label("DEC A");
            table[0x3E] = New().Time(7).Size(2).Load(A, IMMB).Label("LD A,*");
            table[0x3F] = New().Time(4).Size(1).InvertCarryFlag().Label("CCF");

            table[0x40] = New().Time(4).Size(1).Load(B, B).Label("LD B,B");
            table[0x41] = New().Time(4).Size(1).Load(B, C).Label("LD B,C");
            table[0x42] = New().Time(4).Size(1).Load(B, D).Label("LD B,D");
            table[0x43] = New().Time(4).Size(1).Load(B, E).Label("LD B,E");
            table[0x44] = New().Time(4).Size(1).Load(B, H).Label("LD B,H");
            table[0x45] = New().Time(4).Size(1).Load(B, L).Label("LD B,L");
            table[0x46] = New().Time(7).Size(1).Load(B, HLBP).Label("LD B,[HL]");
            table[0x47] = New().Time(4).Size(1).Load(B, A).Label("LD B,A");
            table[0x48] = New().Time(4).Size(1).Load(C, B).Label("LD C,B");
            table[0x49] = New().Time(4).Size(1).Load(C, C).Label("LD C,C");
            table[0x4A] = New().Time(4).Size(1).Load(C, D).Label("LD C,D");
            table[0x4B] = New().Time(4).Size(1).Load(C, E).Label("LD C,E");
            table[0x4C] = New().Time(4).Size(1).Load(C, H).Label("LD C,H");
            table[0x4D] = New().Time(4).Size(1).Load(C, L).Label("LD C,L");
            table[0x4E] = New().Time(7).Size(1).Load(C, HLBP).Label("LD C,[HL]");
            table[0x4F] = New().Time(4).Size(1).Load(C, A).Label("LD C,A");

            table[0x50] = New().Time(4).Size(1).Load(D, B).Label("LD D,B");
            table[0x51] = New().Time(4).Size(1).Load(D, C).Label("LD D,C");
            table[0x52] = New().Time(4).Size(1).Load(D, D).Label("LD D,D");
            table[0x53] = New().Time(4).Size(1).Load(D, E).Label("LD D,E");
            table[0x54] = New().Time(4).Size(1).Load(D, H).Label("LD D,H");
            table[0x55] = New().Time(4).Size(1).Load(D, L).Label("LD D,L");
            table[0x56] = New().Time(7).Size(1).Load(D, HLBP).Label("LD D,[HL]");
            table[0x57] = New().Time(4).Size(1).Load(D, A).Label("LD D,A");
            table[0x58] = New().Time(4).Size(1).Load(E, B).Label("LD E,B");
            table[0x59] = New().Time(4).Size(1).Load(E, C).Label("LD E,C");
            table[0x5A] = New().Time(4).Size(1).Load(E, D).Label("LD E,D");
            table[0x5B] = New().Time(4).Size(1).Load(E, E).Label("LD E,E");
            table[0x5C] = New().Time(4).Size(1).Load(E, H).Label("LD E,H");
            table[0x5D] = New().Time(4).Size(1).Load(E, L).Label("LD E,L");
            table[0x5E] = New().Time(7).Size(1).Load(E, HLBP).Label("LD E,[HL]");
            table[0x5F] = New().Time(4).Size(1).Load(E, A).Label("LD E,A");

            table[0x60] = New().Time(4).Size(1).Load(H, B).Label("LD H,B");
            table[0x61] = New().Time(4).Size(1).Load(H, C).Label("LD H,C");
            table[0x62] = New().Time(4).Size(1).Load(H, D).Label("LD H,D");
            table[0x63] = New().Time(4).Size(1).Load(H, E).Label("LD H,E");
            table[0x64] = New().Time(4).Size(1).Load(H, H).Label("LD H,H");
            table[0x65] = New().Time(4).Size(1).Load(H, L).Label("LD H,L");
            table[0x66] = New().Time(7).Size(1).Load(H, HLBP).Label("LD H,[HL]");
            table[0x67] = New().Time(4).Size(1).Load(H, A).Label("LD H,A");
            table[0x68] = New().Time(4).Size(1).Load(L, B).Label("LD L,B");
            table[0x69] = New().Time(4).Size(1).Load(L, C).Label("LD L,C");
            table[0x6A] = New().Time(4).Size(1).Load(L, D).Label("LD L,D");
            table[0x6B] = New().Time(4).Size(1).Load(L, E).Label("LD L,E");
            table[0x6C] = New().Time(4).Size(1).Load(L, H).Label("LD L,H");
            table[0x6D] = New().Time(4).Size(1).Load(L, L).Label("LD L,L");
            table[0x6E] = New().Time(7).Size(1).Load(L, HLBP).Label("LD L,[HL]");
            table[0x6F] = New().Time(4).Size(1).Load(L, A).Label("LD L,A");

            table[0x70] = New().Time(7).Size(1).Load(HLBP, B).Label("LD [HL],B");
            table[0x71] = New().Time(7).Size(1).Load(HLBP, C).Label("LD [HL],C");
            table[0x72] = New().Time(7).Size(1).Load(HLBP, D).Label("LD [HL],D");
            table[0x73] = New().Time(7).Size(1).Load(HLBP, E).Label("LD [HL],E");
            table[0x74] = New().Time(7).Size(1).Load(HLBP, H).Label("LD [HL],H");
            table[0x75] = New().Time(7).Size(1).Load(HLBP, L).Label("LD [HL],L");
            table[0x76] = New().Time(4).Size(1).Halt(cpu).Label("HALT");
            table[0x77] = New().Time(7).Size(1).Load(HLBP, A).Label("LD [HL],A");
            table[0x78] = New().Time(4).Size(1).Load(A, B).Label("LD A,B");
            table[0x79] = New().Time(4).Size(1).Load(A, C).Label("LD A,C");
            table[0x7A] = New().Time(4).Size(1).Load(A, D).Label("LD A,D");
            table[0x7B] = New().Time(4).Size(1).Load(A, E).Label("LD A,E");
            table[0x7C] = New().Time(4).Size(1).Load(A, H).Label("LD A,H");
            table[0x7D] = New().Time(4).Size(1).Load(A, L).Label("LD A,L");
            table[0x7E] = New().Time(7).Size(1).Load(A, HLBP).Label("LD A,[HL]");
            table[0x7F] = New().Time(4).Size(1).Load(A, A).Label("LD A,A");

            table[0x80] = New().Time(4).Size(1).Add(A, B).Label("ADD A,B");
            table[0x81] = New().Time(4).Size(1).Add(A, C).Label("ADD A,C");
            table[0x82] = New().Time(4).Size(1).Add(A, D).Label("ADD A,D");
            table[0x83] = New().Time(4).Size(1).Add(A, E).Label("ADD A,E");
            table[0x84] = New().Time(4).Size(1).Add(A, H).Label("ADD A,H");
            table[0x85] = New().Time(4).Size(1).Add(A, L).Label("ADD A,L");
            table[0x86] = New().Time(7).Size(1).Add(A, HLBP).Label("ADD A,[HL]");
            table[0x87] = New().Time(4).Size(1).Add(A, A).Label("ADD A,A");
            table[0x88] = New().Time(4).Size(1).Adc(A, B).Label("ADC A,B");
            table[0x89] = New().Time(4).Size(1).Adc(A, C).Label("ADC A,C");
            table[0x8A] = New().Time(4).Size(1).Adc(A, D).Label("ADC A,D");
            table[0x8B] = New().Time(4).Size(1).Adc(A, E).Label("ADC A,E");
            table[0x8C] = New().Time(4).Size(1).Adc(A, H).Label("ADC A,H");
            table[0x8D] = New().Time(4).Size(1).Adc(A, L).Label("ADC A,L");
            table[0x8E] = New().Time(7).Size(1).Adc(A, HLBP).Label("ADC A,[HL]");
            table[0x8F] = New().Time(4).Size(1).Adc(A, A).Label("ADC A,A");

            table[0x90] = New().Time(4).Size(1).Sub(A, B).Label("SUB B");
            table[0x91] = New().Time(4).Size(1).Sub(A, C).Label("SUB C");
            table[0x92] = New().Time(4).Size(1).Sub(A, D).Label("SUB D");
            table[0x93] = New().Time(4).Size(1).Sub(A, E).Label("SUB E");
            table[0x94] = New().Time(4).Size(1).Sub(A, H).Label("SUB H");
            table[0x95] = New().Time(4).Size(1).Sub(A, L).Label("SUB L");
            table[0x96] = New().Time(7).Size(1).Sub(A, HLBP).Label("SUB [HL]");
            table[0x97] = New().Time(4).Size(1).Sub(A, A).Label("SUB A");
            table[0x98] = New().Time(4).Size(1).Sbc(A, B).Label("SBC A,B");
            table[0x99] = New().Time(4).Size(1).Sbc(A, C).Label("SBC A,C");
            table[0x9A] = New().Time(4).Size(1).Sbc(A, D).Label("SBC A,D");
            table[0x9B] = New().Time(4).Size(1).Sbc(A, E).Label("SBC A,E");
            table[0x9C] = New().Time(4).Size(1).Sbc(A, H).Label("SBC A,H");
            table[0x9D] = New().Time(4).Size(1).Sbc(A, L).Label("SBC A,L");
            table[0x9E] = New().Time(7).Size(1).Sbc(A, HLBP).Label("SBC A,[HL]");
            table[0x9F] = New().Time(4).Size(1).Sbc(A, A).Label("SBC A,A");

            table[0xA0] = New().Time(4).Size(1).And(A, B).Label("AND B");
            table[0xA1] = New().Time(4).Size(1).And(A, C).Label("AND C");
            table[0xA2] = New().Time(4).Size(1).And(A, D).Label("AND D");
            table[0xA3] = New().Time(4).Size(1).And(A, E).Label("AND E");
            table[0xA4] = New().Time(4).Size(1).And(A, H).Label("AND H");
            table[0xA5] = New().Time(4).Size(1).And(A, L).Label("AND L");
            table[0xA6] = New().Time(7).Size(1).And(A, HLBP).Label("AND [HL]");
            table[0xA7] = New().Time(4).Size(1).And(A, A).Label("AND A");
            table[0xA8] = New().Time(4).Size(1).Xor(A, B).Label("XOR B");
            table[0xA9] = New().Time(4).Size(1).Xor(A, C).Label("XOR C");
            table[0xAA] = New().Time(4).Size(1).Xor(A, D).Label("XOR D");
            table[0xAB] = New().Time(4).Size(1).Xor(A, E).Label("XOR E");
            table[0xAC] = New().Time(4).Size(1).Xor(A, H).Label("XOR H");
            table[0xAD] = New().Time(4).Size(1).Xor(A, L).Label("XOR L");
            table[0xAE] = New().Time(7).Size(1).Xor(A, HLBP).Label("XOR [HL]");
            table[0xAF] = New().Time(4).Size(1).Xor(A, A).Label("XOR A");

            table[0xB0] = New().Time(4).Size(1).Or(A, B).Label("OR B");
            table[0xB1] = New().Time(4).Size(1).Or(A, C).Label("OR C");
            table[0xB2] = New().Time(4).Size(1).Or(A, D).Label("OR D");
            table[0xB3] = New().Time(4).Size(1).Or(A, E).Label("OR E");
            table[0xB4] = New().Time(4).Size(1).Or(A, H).Label("OR H");
            table[0xB5] = New().Time(4).Size(1).Or(A, L).Label("OR L");
            table[0xB6] = New().Time(7).Size(1).Or(A, HLBP).Label("OR [HL]");
            table[0xB7] = New().Time(4).Size(1).Or(A, A).Label("OR A");
            table[0xB8] = New().Time(4).Size(1).Cp(A, B).Label("CP B");
            table[0xB9] = New().Time(4).Size(1).Cp(A, C).Label("CP C");
            table[0xBA] = New().Time(4).Size(1).Cp(A, D).Label("CP D");
            table[0xBB] = New().Time(4).Size(1).Cp(A, E).Label("CP E");
            table[0xBC] = New().Time(4).Size(1).Cp(A, H).Label("CP H");
            table[0xBD] = New().Time(4).Size(1).Cp(A, L).Label("CP L");
            table[0xBE] = New().Time(7).Size(1).Cp(A, HLBP).Label("CP [HL]");
            table[0xBF] = New().Time(4).Size(1).Cp(A, A).Label("CP A");

            table[0xC0] = New().Time(11,5).Size(1).Ret(SP, () => !F.Zero).Label("RET NZ");
            table[0xC1] = New().Time(10).Size(1).Pop(SP, BC).Label("POP BC");
            table[0xC2] = New().Time(10).Size(3).Jump(IMMW,() => !F.Zero).Label("JP NZ,**");
            table[0xC3] = New().Time(10).Size(3).Jump(IMMW).Label("JP **");
            table[0xC4] = New().Time(17,10).Size(3).Call(SP, () => !F.Zero).Label("CALL NZ,**");
            table[0xC5] = New().Time(11).Size(1).Push(SP, BC).Label("PUSH BC");
            table[0xC6] = New().Time(7).Size(2).Add(A, IMMB).Label("ADD A,*");
            table[0xC7] = New().Time(11).Size(1).Reset(SP, 0).Label("RST 0x00");
            table[0xC8] = New().Time(11,5).Size(1).Ret(SP, () => F.Zero).Label("RET Z");
            table[0xC9] = New().Time(10).Size(1).Ret(SP).Label("RET");
            table[0xCA] = New().Time(10).Size(3).Jump(IMMW,() => F.Zero).Label("JP Z,**");
            table[0xCB] = new InstructionBuilderComposite(1, PC, bits);
            table[0xCC] = New().Time(17,10).Size(3).Call(SP, () => F.Zero).Label("CALL Z,**");
            table[0xCD] = New().Time(17).Size(3).Call(SP).Label("CALL **");
            table[0xCE] = New().Time(7).Size(2).Adc(A, IMMB).Label("ADC A,*");
            table[0xCF] = New().Time(11).Size(1).Reset(SP, 0x08).Label("RST 0x08");

            table[0xD0] = New().Time(11,5).Size(1).Ret(SP, () => !F.Carry).Label("RET NC");
            table[0xD1] = New().Time(10).Size(1).Pop(SP, DE).Label("POP DE");
            table[0xD2] = New().Time(10).Size(3).Jump(IMMW, () => !F.Carry).Label("JP NC,**");
            table[0xD3] = New().Time(11).Size(2).Out(PORT, A, IMMB, A).Label("OUT [*],A");
            table[0xD4] = New().Time(17,10).Size(3).Call(SP, () => !F.Carry).Label("CALL NC,**");
            table[0xD5] = New().Time(11).Size(1).Push(SP, DE).Label("PUSH DE");
            table[0xD6] = New().Time(7).Size(2).Sub(A, IMMB).Label("SUB *");
            table[0xD7] = New().Time(11).Size(1).Reset(SP, 0x10).Label("RST 0x10");
            table[0xD8] = New().Time(11,5).Size(1).Ret(SP, () => F.Carry).Label("RET C");
            table[0xD9] = New().Time(4).Size(1).Exx(cpu.Registers, cpu.RegistersCopy).Label("EXX");
            table[0xDA] = New().Time(10).Size(3).Jump(IMMW, () => F.Carry).Label("JP C,**");
            table[0xDB] = New().Time(11).Size(2).In(PORT, A, IMMB, A, false).Label("IN A,[*]");
            table[0xDC] = New().Time(17,10).Size(3).Call(SP, () => F.Carry).Label("CALL C,**");
            table[0xDD] = new InstructionBuilderComposite(1, PC, lookupIX, fallback);
            table[0xDE] = New().Time(7).Size(2).Sbc(A, IMMB).Label("SBC A,*");
            table[0xDF] = New().Time(11).Size(1).Reset(SP, 0x18).Label("RST 0x18");

            table[0xE0] = New().Time(11,5).Size(1).Ret(SP, () => !F.Parity).Label("RET PO");
            table[0xE1] = New().Time(10).Size(1).Pop(SP, HL).Label("POP HL");
            table[0xE2] = New().Time(10).Size(3).Jump(IMMW, () => !F.Parity).Label("JP PO,**");
            table[0xE3] = New().Time(19).Size(1).Exchange(SPWP, HL).Label("EX [SP],HL");
            table[0xE4] = New().Time(17,10).Size(3).Call(SP, () => !F.Parity).Label("CALL PO,**");
            table[0xE5] = New().Time(11).Size(1).Push(SP, HL).Label("PUSH HL");
            table[0xE6] = New().Time(7).Size(2).And(A, IMMB).Label("AND *");
            table[0xE7] = New().Time(11).Size(1).Reset(SP, 0x20).Label("RST 0x20");
            table[0xE8] = New().Time(11,5).Size(1).Ret(SP, () => F.Parity).Label("RET PE");
            table[0xE9] = New().Time(4).Size(1).Jump(HL).Label("JP [HL]");
            table[0xEA] = New().Time(10).Size(3).Jump(IMMW,() => F.Parity).Label("JP PE,**");
            table[0xEB] = New().Time(4).Size(1).Exchange(DE, HL).Label("EX DE,HL");
            table[0xEC] = New().Time(17,10).Size(3).Call(SP, () => F.Parity).Label("CALL PE,**");
            table[0xED] = new InstructionBuilderComposite(1, PC, extended, extendedNop);
            table[0xEE] = New().Time(7).Size(2).Xor(A, IMMB).Label("XOR *");
            table[0xEF] = New().Time(11).Size(1).Reset(SP, 0x28).Label("RST 0x28");

            table[0xF0] = New().Time(11,5).Size(1).Ret(SP, () => !F.Sign).Label("RET P");
            table[0xF1] = New().Time(10).Size(1).Pop(SP, AF).Label("POP AF");
            table[0xF2] = New().Time(10).Size(3).Jump(IMMW, () => !F.Sign).Label("JP P,**");
            table[0xF3] = New().Time(4).Size(1).DisableInterrupts(cpu).Label("DI");
            table[0xF4] = New().Time(17,10).Size(3).Call(SP, () => !F.Sign).Label("CALL P,**");
            table[0xF5] = New().Time(11).Size(1).Push(SP, AF).Label("PUSH AF");
            table[0xF6] = New().Time(7).Size(2).Or(A, IMMB).Label("OR *");
            table[0xF7] = New().Time(11).Size(1).Reset(SP, 0x30).Label("RST 0x30");
            table[0xF8] = New().Time(11,5).Size(1).Ret(SP, () => F.Sign).Label("RET M");
            table[0xF9] = New().Time(6).Size(1).Load(SP, HL).Label("LD SP,HL");
            table[0xFA] = New().Time(10).Size(3).Jump(IMMW,() => F.Sign).Label("JP M,**");
            table[0xFB] = New().Time(4).Size(1).EnableInterrupts(cpu).Label("EI");
            table[0xFC] = New().Time(17,10).Size(3).Call(SP, () => F.Sign).Label("CALL M,**");
            table[0xFD] = new InstructionBuilderComposite(1, PC, lookupIY, fallback);
            table[0xFE] = New().Time(7).Size(2).Cp(A, IMMB).Label("CP *");
            table[0xFF] = New().Time(11).Size(1).Reset(SP, 0x38).Label("RST 0x38");

            //////////////////////////////////////////////////////////////////////////////////
            extended[0x40] = New().Time(12).Size(2).In(PORT, B, C, B).Label("IN B,(C)");
            extended[0x50] = New().Time(12).Size(2).In(PORT, D, C, B).Label("IN D,(C)");
            extended[0x60] = New().Time(12).Size(2).In(PORT, H, C, B).Label("IN H,(C)");
            extended[0x70] = New().Time(12).Size(2).In(PORT, null, C, B).Label("IN (C)");

            extended[0x41] = New().Time(12).Size(2).Out(PORT, B, C, B).Label("OUT (C),B");
            extended[0x51] = New().Time(12).Size(2).Out(PORT, D, C, B).Label("OUT (C),D");
            extended[0x61] = New().Time(12).Size(2).Out(PORT, H, C, B).Label("OUT (C),H");
            extended[0x71] = New().Time(12).Size(2).Out(PORT, null, C, B).Label("OUT (C),0");

            extended[0x42] = New().Time(15).Size(2).Sbc(HL, BC).Label("SBC HL,BC");
            extended[0x52] = New().Time(15).Size(2).Sbc(HL, DE).Label("SBC HL,DE");
            extended[0x62] = New().Time(15).Size(2).Sbc(HL, HL).Label("SBC HL,HL");
            extended[0x72] = New().Time(15).Size(2).Sbc(HL, SP).Label("SBC HL,SP");

            extended[0x43] = New().Time(20).Size(4).Load(IMMWP2, BC).Label("LD [**],BC");
            extended[0x53] = New().Time(20).Size(4).Load(IMMWP2, DE).Label("LD [**],DE");
            extended[0x63] = New().Time(20).Size(4).Load(IMMWP2, HL).Label("LD [**],HL");
            extended[0x73] = New().Time(20).Size(4).Load(IMMWP2, SP).Label("LD [**],SP");

            extended[0x44] = New().Time(8).Size(2).Neg(A).Label("NEG");
            extended[0x54] = New().Time(8).Size(2).Neg(A).Label("NEG");
            extended[0x64] = New().Time(8).Size(2).Neg(A).Label("NEG");
            extended[0x74] = New().Time(8).Size(2).Neg(A).Label("NEG");
            extended[0x4C] = New().Time(8).Size(2).Neg(A).Label("NEG");
            extended[0x5C] = New().Time(8).Size(2).Neg(A).Label("NEG");
            extended[0x6C] = New().Time(8).Size(2).Neg(A).Label("NEG");
            extended[0x7C] = New().Time(8).Size(2).Neg(A).Label("NEG");

            extended[0x45] = New().Time(14).Size(2).Retn(SP, cpu);
            extended[0x55] = New().Time(14).Size(2).Retn(SP, cpu);
            extended[0x65] = New().Time(14).Size(2).Retn(SP, cpu);
            extended[0x75] = New().Time(14).Size(2).Retn(SP, cpu);
            extended[0x4D] = New().Time(14).Size(2).Retn(SP, cpu).Label("RETI"); // NMI not emulated, so RETN
            extended[0x5D] = New().Time(14).Size(2).Retn(SP, cpu).Label("RETN");
            extended[0x6D] = New().Time(14).Size(2).Retn(SP, cpu).Label("RETN");
            extended[0x7D] = New().Time(14).Size(2).Retn(SP, cpu).Label("RETN");

            extended[0x46] = New().Time(8).Size(2).SetInterruptMode(cpu, 0).Label("IM 0");
            extended[0x4E] = New().Time(8).Size(2).SetInterruptMode(cpu, 0).Label("IM 0 undefined");
            extended[0x66] = New().Time(8).Size(2).SetInterruptMode(cpu, 0).Label("IM 0");
            extended[0x6E] = New().Time(8).Size(2).SetInterruptMode(cpu, 0).Label("IM 0 undefined");

            extended[0x56] = New().Time(8).Size(2).SetInterruptMode(cpu, 1).Label("IM 1");
            extended[0x76] = New().Time(8).Size(2).SetInterruptMode(cpu, 1).Label("IM 1");
            extended[0x5E] = New().Time(8).Size(2).SetInterruptMode(cpu, 2).Label("IM 2");
            extended[0x7E] = New().Time(8).Size(2).SetInterruptMode(cpu, 2).Label("IM 2");

            extended[0x47] = New().Time(9).Size(2).Load(I, A).Label("LD I,A");
            extended[0x57] = New().Time(9).Size(2).LoadIR(A, I, cpu).Label("LD A,I");
            extended[0x4F] = New().Time(9).Size(2).Load(R, A).Label("LD R,A");
            extended[0x5F] = New().Time(9).Size(2).LoadIR(A, R, cpu).Label("LD A,R");

            extended[0x67] = New().Time(18).Size(2).RotateDigitRight(A, HL).Label("RRD");
            extended[0x6F] = New().Time(18).Size(2).RotateDigitLeft(A, HL).Label("RLD");

            extended[0x48] = New().Time(12).Size(2).In(PORT, C, C, B).Label("IN C,(C)");
            extended[0x58] = New().Time(12).Size(2).In(PORT, E, C, B).Label("IN E,(C)");
            extended[0x68] = New().Time(12).Size(2).In(PORT, L, C, B).Label("IN L,(C)");
            extended[0x78] = New().Time(12).Size(2).In(PORT, A, C, B).Label("IN A, (C)");

            extended[0x49] = New().Time(12).Size(2).Out(PORT, C, C, B).Label("OUT (C),C");
            extended[0x59] = New().Time(12).Size(2).Out(PORT, E, C, B).Label("OUT (C),E");
            extended[0x69] = New().Time(12).Size(2).Out(PORT, L, C, B).Label("OUT (C),L");
            extended[0x79] = New().Time(12).Size(2).Out(PORT, A, C, B).Label("OUT (C),A");

            extended[0x4A] = New().Time(15).Size(2).Adc(HL, BC).Label("ADC HL,BC");
            extended[0x5A] = New().Time(15).Size(2).Adc(HL, DE).Label("ADC HL,DE");
            extended[0x6A] = New().Time(15).Size(2).Adc(HL, HL).Label("ADC HL,HL");
            extended[0x7A] = New().Time(15).Size(2).Adc(HL, SP).Label("ADC HL,SP");

            extended[0x4B] = New().Time(20).Size(4).Load(BC, IMMWP2).Label("LD BC,[**]");
            extended[0x5B] = New().Time(20).Size(4).Load(DE, IMMWP2).Label("LD DE,[**]");
            extended[0x6B] = New().Time(20).Size(4).Load(HL, IMMWP2).Label("LD HL,[**]");
            extended[0x7B] = New().Time(20).Size(4).Load(SP, IMMWP2).Label("LD SP,[**]");

            extended[0xA0] = New().Time(16).Size(2).BlockLoad(DE, HL, BC, BlockMode.IO).Label("LDI");
            extended[0xA1] = New().Time(16).Size(2).BlockCompare(A, HL, BC, BlockMode.IO).Label("CPI");
            extended[0xA2] = New().Time(16).Size(2).BlockInput(PORT, HL, BC, BlockMode.IO).Label("INI");
            extended[0xA3] = New().Time(16).Size(2).BlockOutput(PORT, HL, BC, BlockMode.IO).Label("OUTI");

            extended[0xA8] = New().Time(16).Size(2).BlockLoad(DE, HL, BC, BlockMode.DO).Label("LDD");
            extended[0xA9] = New().Time(16).Size(2).BlockCompare(A, HL, BC, BlockMode.DO).Label("CPD");
            extended[0xAA] = New().Time(16).Size(2).BlockInput(PORT, HL, BC, BlockMode.DO).Label("IND");
            extended[0xAB] = New().Time(16).Size(2).BlockOutput(PORT, HL, BC, BlockMode.DO).Label("OUTD");

            extended[0xB0] = New().Time(21,16).Size(2).BlockLoad(DE, HL, BC, BlockMode.IR).Label("LDIR");
            extended[0xB1] = New().Time(21,16).Size(2).BlockCompare(A, HL, BC, BlockMode.IR).Label("CPIR");
            extended[0xB2] = New().Time(21,16).Size(2).BlockInput(PORT, HL, BC, BlockMode.IR).Label("INI");
            extended[0xB3] = New().Time(21,16).Size(2).BlockOutput(PORT, HL, BC, BlockMode.IR).Label("OTIR");

            extended[0xB8] = New().Time(21,16).Size(2).BlockLoad(DE, HL, BC, BlockMode.DR).Label("LDDR");
            extended[0xB9] = New().Time(21,16).Size(2).BlockCompare(A, HL, BC, BlockMode.DR).Label("CPDR");
            extended[0xBA] = New().Time(21,16).Size(2).BlockInput(PORT, HL, BC, BlockMode.DR).Label("INDR");
            extended[0xBB] = New().Time(21,16).Size(2).BlockOutput(PORT, HL, BC, BlockMode.DR).Label("OTDR");

            //////////////////////////////////////////////////////////////////////////////////
            lookupIX[0x09] = New().Time(15).Size(2).Add(IX, BC).Label("ADD IX,BC");
            lookupIX[0x19] = New().Time(15).Size(2).Add(IX, DE).Label("ADD IX,DE");
            lookupIX[0x29] = New().Time(15).Size(2).Add(IX, IX).Label("ADD IX,IX");
            lookupIX[0x39] = New().Time(15).Size(2).Add(IX, SP).Label("ADD IX,SP");
            lookupIX[0x21] = New().Time(14).Size(4).Load(IX, IMMW2).Label("LD IX,**");
            lookupIX[0x22] = New().Time(20).Size(4).Load(IMMWP2, IX).Label("LD [**],IX");
            lookupIX[0x23] = New().Time(10).Size(2).Increment(IX).Label("INC IX");
            lookupIX[0x24] = New().Time(08).Size(2).Increment(IXH).Label("INC IXH");
            lookupIX[0x25] = New().Time(08).Size(2).Decrement(IXH).Label("DEC IXH");
            lookupIX[0x26] = New().Time(11).Size(3).Load(IXH, IMMB2).Label("LD IXH,*");

            lookupIX[0x2A] = New().Time(20).Size(4).Load(IX, IMMWP2).Label("LD IX,[**]");
            lookupIX[0x2B] = New().Time(10).Size(2).Decrement(IX).Label("DEC IX");
            lookupIX[0x2C] = New().Time(08).Size(2).Increment(IXL).Label("INC IXL");
            lookupIX[0x2D] = New().Time(08).Size(2).Decrement(IXL).Label("DEC IXL");
            lookupIX[0x2E] = New().Time(11).Size(3).Load(IXL, IMMB2).Label("LD IXL,*");

            lookupIX[0x34] = New().Time(23).Size(3).Increment(IXIMM).Label("INC [IX+*]");
            lookupIX[0x35] = New().Time(23).Size(3).Decrement(IXIMM).Label("DEC [IX+*]");
            lookupIX[0x36] = New().Time(19).Size(4).Load(IXIMM, IMMB3).Label("LD [IX+*],*");

            lookupIX[0x44] = New().Time(08).Size(2).Load(B, IXH).Label("LD B,IXH");
            lookupIX[0x45] = New().Time(08).Size(2).Load(B, IXL).Label("LD B,IXL");
            lookupIX[0x46] = New().Time(19).Size(3).Load(B, IXIMM).Label("LD B,[IX+*]");
            lookupIX[0x4C] = New().Time(08).Size(2).Load(C, IXH).Label("LD C,IXH");
            lookupIX[0x4D] = New().Time(08).Size(2).Load(C, IXL).Label("LD C,IXL");
            lookupIX[0x4E] = New().Time(19).Size(3).Load(C, IXIMM).Label("LD C,[IX+*]");
            lookupIX[0x54] = New().Time(08).Size(2).Load(D, IXH).Label("LD D,IXH");
            lookupIX[0x55] = New().Time(08).Size(2).Load(D, IXL).Label("LD D,IXL");
            lookupIX[0x56] = New().Time(19).Size(3).Load(D, IXIMM).Label("LD D,[IX+*]");
            lookupIX[0x5C] = New().Time(08).Size(2).Load(E, IXH).Label("LD E,IXH");
            lookupIX[0x5D] = New().Time(08).Size(2).Load(E, IXL).Label("LD E,IXL");
            lookupIX[0x5E] = New().Time(19).Size(3).Load(E, IXIMM).Label("LD E,[IX+*]");

            lookupIX[0x67] = New().Time(08).Size(2).Load(IXH, A).Label("LD IXH,A");
            lookupIX[0x60] = New().Time(08).Size(2).Load(IXH, B).Label("LD IXH,B");
            lookupIX[0x61] = New().Time(08).Size(2).Load(IXH, C).Label("LD IXH,C");
            lookupIX[0x62] = New().Time(08).Size(2).Load(IXH, D).Label("LD IXH,D");
            lookupIX[0x63] = New().Time(08).Size(2).Load(IXH, E).Label("LD IXH,E");
            lookupIX[0x64] = New().Time(08).Size(2).Load(IXH, IXH).Label("LD IXH,IXH");
            lookupIX[0x65] = New().Time(08).Size(2).Load(IXH, IXL).Label("LD IXH,IXL");
            lookupIX[0x66] = New().Time(19).Size(3).Load(H, IXIMM).Label("LD H,[IX+*]");

            lookupIX[0x6F] = New().Time(08).Size(2).Load(IXL, A).Label("LD IXL,A");
            lookupIX[0x68] = New().Time(08).Size(2).Load(IXL, B).Label("LD IXL,B");
            lookupIX[0x69] = New().Time(08).Size(2).Load(IXL, C).Label("LD IXL,C");
            lookupIX[0x6A] = New().Time(08).Size(2).Load(IXL, D).Label("LD IXL,D");
            lookupIX[0x6B] = New().Time(08).Size(2).Load(IXL, E).Label("LD IXL,E");
            lookupIX[0x6C] = New().Time(08).Size(2).Load(IXL, IXH).Label("LD IXL,IXH");
            lookupIX[0x6D] = New().Time(08).Size(2).Load(IXL, IXL).Label("LD IXL,IXL");
            lookupIX[0x6E] = New().Time(19).Size(3).Load(L, IXIMM).Label("LD L,[IX+*]");

            lookupIX[0x77] = New().Time(19).Size(3).Load(IXIMM, A).Label("LD [IX+*],A");
            lookupIX[0x70] = New().Time(19).Size(3).Load(IXIMM, B).Label("LD [IX+*],B");
            lookupIX[0x71] = New().Time(19).Size(3).Load(IXIMM, C).Label("LD [IX+*],C");
            lookupIX[0x72] = New().Time(19).Size(3).Load(IXIMM, D).Label("LD [IX+*],D");
            lookupIX[0x73] = New().Time(19).Size(3).Load(IXIMM, E).Label("LD [IX+*],E");
            lookupIX[0x74] = New().Time(19).Size(3).Load(IXIMM, H).Label("LD [IX+*],H");
            lookupIX[0x75] = New().Time(19).Size(3).Load(IXIMM, L).Label("LD [IX+*],L");

            lookupIX[0x7C] = New().Time(08).Size(2).Load(A, IXH).Label("LD A,IXH");
            lookupIX[0x7D] = New().Time(08).Size(2).Load(A, IXL).Label("LD A,IXL");
            lookupIX[0x7E] = New().Time(19).Size(3).Load(A, IXIMM).Label("LD A,[IX+*]");

            lookupIX[0x84] = New().Time(08).Size(2).Add(A, IXH).Label("ADD A,IXH");
            lookupIX[0x85] = New().Time(08).Size(2).Add(A, IXL).Label("ADD A,IXL");
            lookupIX[0x86] = New().Time(19).Size(3).Add(A, IXIMM).Label("ADD A,[IX+*]");
            lookupIX[0x8C] = New().Time(08).Size(2).Adc(A, IXH).Label("ADC A,IXH");
            lookupIX[0x8D] = New().Time(08).Size(2).Adc(A, IXL).Label("ADC A,IXL");
            lookupIX[0x8E] = New().Time(19).Size(3).Adc(A, IXIMM).Label("ADC A,[IX+*]");

            lookupIX[0x94] = New().Time(08).Size(2).Sub(A, IXH).Label("SUB A,IXH");
            lookupIX[0x95] = New().Time(08).Size(2).Sub(A, IXL).Label("SUB A,IXL");
            lookupIX[0x96] = New().Time(19).Size(3).Sub(A, IXIMM).Label("SUB A,[IX+*]");
            lookupIX[0x9C] = New().Time(08).Size(2).Sbc(A, IXH).Label("SBC A,IXH");
            lookupIX[0x9D] = New().Time(08).Size(2).Sbc(A, IXL).Label("SBC A,IXL");
            lookupIX[0x9E] = New().Time(19).Size(3).Sbc(A, IXIMM).Label("SBC A,[IX+*]");

            lookupIX[0xA4] = New().Time(08).Size(2).And(A, IXH).Label("AND A,IXH");
            lookupIX[0xA5] = New().Time(08).Size(2).And(A, IXL).Label("AND A,IXL");
            lookupIX[0xA6] = New().Time(19).Size(3).And(A, IXIMM).Label("AND A,[IX+*]");
            lookupIX[0xAC] = New().Time(08).Size(2).Xor(A, IXH).Label("XOR A,IXH");
            lookupIX[0xAD] = New().Time(08).Size(2).Xor(A, IXL).Label("XOR A,IXL");
            lookupIX[0xAE] = New().Time(19).Size(3).Xor(A, IXIMM).Label("XOR A,[IX+*]");

            lookupIX[0xB4] = New().Time(08).Size(2).Or(A, IXH).Label("OR A,IXH");
            lookupIX[0xB5] = New().Time(08).Size(2).Or(A, IXL).Label("OR A,IXL");
            lookupIX[0xB6] = New().Time(19).Size(3).Or(A, IXIMM).Label("OR A,[IX+*]");
            lookupIX[0xBC] = New().Time(08).Size(2).Cp(A, IXH).Label("CP A,IXH");
            lookupIX[0xBD] = New().Time(08).Size(2).Cp(A, IXL).Label("CP A,IXL");
            lookupIX[0xBE] = New().Time(19).Size(3).Cp(A, IXIMM).Label("CP A,[IX+*]");

            lookupIX[0xE1] = New().Time(14).Size(2).Pop(SP, IX).Label("POP IX");
            lookupIX[0xE3] = New().Time(23).Size(2).Exchange(SPWP, IX).Label("EX [SP],IX");
            lookupIX[0xE5] = New().Time(15).Size(2).Push(SP, IX).Label("PUSH IX");
            lookupIX[0xE9] = New().Time(08).Size(2).Jump(IX).Label("JP [IX]");
            lookupIX[0xF9] = New().Time(10).Size(2).Load(SP, IX).Label("LD SP,IX");
            
            lookupIX[0xCB] = new InstructionBuilderComposite(3, PC, exBitsIX);

            //////////////////////////////////////////////////////////////////////////////////
            lookupIY[0x09] = New().Time(15).Size(2).Add(IY, BC).Label("ADD IY,BC");
            lookupIY[0x19] = New().Time(15).Size(2).Add(IY, DE).Label("ADD IY,DE");
            lookupIY[0x29] = New().Time(15).Size(2).Add(IY, IY).Label("ADD IY,IY");
            lookupIY[0x39] = New().Time(15).Size(2).Add(IY, SP).Label("ADD IY,SP");
            lookupIY[0x21] = New().Time(14).Size(4).Load(IY, IMMW2).Label("LD IY,**");
            lookupIY[0x22] = New().Time(20).Size(4).Load(IMMWP2, IY).Label("LD [**],IY");
            lookupIY[0x23] = New().Time(10).Size(2).Increment(IY).Label("INC IY");
            lookupIY[0x24] = New().Time(08).Size(2).Increment(IYH).Label("INC IYH");
            lookupIY[0x25] = New().Time(08).Size(2).Decrement(IYH).Label("DEC IYH");
            lookupIY[0x26] = New().Time(11).Size(3).Load(IYH, IMMB2).Label("LD IYH,*");

            lookupIY[0x2A] = New().Time(20).Size(4).Load(IY, IMMWP2).Label("LD IY,[**]");
            lookupIY[0x2B] = New().Time(10).Size(2).Decrement(IY).Label("DEC IY");
            lookupIY[0x2C] = New().Time(08).Size(2).Increment(IYL).Label("INC IYL");
            lookupIY[0x2D] = New().Time(08).Size(2).Decrement(IYL).Label("DEC IYL");
            lookupIY[0x2E] = New().Time(11).Size(3).Load(IYL, IMMB2).Label("LD IYL,*");

            lookupIY[0x34] = New().Time(23).Size(3).Increment(IYIMM).Label("INC [IY+*]");
            lookupIY[0x35] = New().Time(23).Size(3).Decrement(IYIMM).Label("DEC [IY+*]");
            lookupIY[0x36] = New().Time(19).Size(4).Load(IYIMM, IMMB3).Label("LD [IY+*],*");

            lookupIY[0x44] = New().Time(08).Size(2).Load(B, IYH).Label("LD B,IYH");
            lookupIY[0x45] = New().Time(08).Size(2).Load(B, IYL).Label("LD B,IYL");
            lookupIY[0x46] = New().Time(19).Size(3).Load(B, IYIMM).Label("LD B,[IY+*]");
            lookupIY[0x4C] = New().Time(08).Size(2).Load(C, IYH).Label("LD C,IYH");
            lookupIY[0x4D] = New().Time(08).Size(2).Load(C, IYL).Label("LD C,IYL");
            lookupIY[0x4E] = New().Time(19).Size(3).Load(C, IYIMM).Label("LD C,[IY+*]");
            lookupIY[0x54] = New().Time(08).Size(2).Load(D, IYH).Label("LD D,IYH");
            lookupIY[0x55] = New().Time(08).Size(2).Load(D, IYL).Label("LD D,IYL");
            lookupIY[0x56] = New().Time(19).Size(3).Load(D, IYIMM).Label("LD D,[IY+*]");
            lookupIY[0x5C] = New().Time(08).Size(2).Load(E, IYH).Label("LD E,IYH");
            lookupIY[0x5D] = New().Time(08).Size(2).Load(E, IYL).Label("LD E,IYL");
            lookupIY[0x5E] = New().Time(19).Size(3).Load(E, IYIMM).Label("LD E,[IY+*]");

            lookupIY[0x67] = New().Time(08).Size(2).Load(IYH, A).Label("LD IYH,A");
            lookupIY[0x60] = New().Time(08).Size(2).Load(IYH, B).Label("LD IYH,B");
            lookupIY[0x61] = New().Time(08).Size(2).Load(IYH, C).Label("LD IYH,C");
            lookupIY[0x62] = New().Time(08).Size(2).Load(IYH, D).Label("LD IYH,D");
            lookupIY[0x63] = New().Time(08).Size(2).Load(IYH, E).Label("LD IYH,E");
            lookupIY[0x64] = New().Time(08).Size(2).Load(IYH, IYH).Label("LD IYH,IYH");
            lookupIY[0x65] = New().Time(08).Size(2).Load(IYH, IYL).Label("LD IYH,IYL");
            lookupIY[0x66] = New().Time(19).Size(3).Load(H, IYIMM).Label("LD H,[IY+*]");

            lookupIY[0x6F] = New().Time(08).Size(2).Load(IYL, A).Label("LD IYL,A");
            lookupIY[0x68] = New().Time(08).Size(2).Load(IYL, B).Label("LD IYL,B");
            lookupIY[0x69] = New().Time(08).Size(2).Load(IYL, C).Label("LD IYL,C");
            lookupIY[0x6A] = New().Time(08).Size(2).Load(IYL, D).Label("LD IYL,D");
            lookupIY[0x6B] = New().Time(08).Size(2).Load(IYL, E).Label("LD IYL,E");
            lookupIY[0x6C] = New().Time(08).Size(2).Load(IYL, IYH).Label("LD IYL,IYH");
            lookupIY[0x6D] = New().Time(08).Size(2).Load(IYL, IYL).Label("LD IYL,IYL");
            lookupIY[0x6E] = New().Time(19).Size(3).Load(L, IYIMM).Label("LD L,[IY+*]");

            lookupIY[0x77] = New().Time(19).Size(3).Load(IYIMM, A).Label("LD [IY+*],A");
            lookupIY[0x70] = New().Time(19).Size(3).Load(IYIMM, B).Label("LD [IY+*],B");
            lookupIY[0x71] = New().Time(19).Size(3).Load(IYIMM, C).Label("LD [IY+*],C");
            lookupIY[0x72] = New().Time(19).Size(3).Load(IYIMM, D).Label("LD [IY+*],D");
            lookupIY[0x73] = New().Time(19).Size(3).Load(IYIMM, E).Label("LD [IY+*],E");
            lookupIY[0x74] = New().Time(19).Size(3).Load(IYIMM, H).Label("LD [IY+*],H");
            lookupIY[0x75] = New().Time(19).Size(3).Load(IYIMM, L).Label("LD [IY+*],L");

            lookupIY[0x7C] = New().Time(08).Size(2).Load(A, IYH).Label("LD A,IYH");
            lookupIY[0x7D] = New().Time(08).Size(2).Load(A, IYL).Label("LD A,IYL");
            lookupIY[0x7E] = New().Time(19).Size(3).Load(A, IYIMM).Label("LD A,[IY+*]");

            lookupIY[0x84] = New().Time(08).Size(2).Add(A, IYH).Label("ADD A,IYH");
            lookupIY[0x85] = New().Time(08).Size(2).Add(A, IYL).Label("ADD A,IYL");
            lookupIY[0x86] = New().Time(19).Size(3).Add(A, IYIMM).Label("ADD A,[IY+*]");
            lookupIY[0x8C] = New().Time(08).Size(2).Adc(A, IYH).Label("ADC A,IYH");
            lookupIY[0x8D] = New().Time(08).Size(2).Adc(A, IYL).Label("ADC A,IYL");
            lookupIY[0x8E] = New().Time(19).Size(3).Adc(A, IYIMM).Label("ADC A,[IY+*]");

            lookupIY[0x94] = New().Time(08).Size(2).Sub(A, IYH).Label("SUB A,IYH");
            lookupIY[0x95] = New().Time(08).Size(2).Sub(A, IYL).Label("SUB A,IYL");
            lookupIY[0x96] = New().Time(19).Size(3).Sub(A, IYIMM).Label("SUB A,[IY+*]");
            lookupIY[0x9C] = New().Time(08).Size(2).Sbc(A, IYH).Label("SBC A,IYH");
            lookupIY[0x9D] = New().Time(08).Size(2).Sbc(A, IYL).Label("SBC A,IYL");
            lookupIY[0x9E] = New().Time(19).Size(3).Sbc(A, IYIMM).Label("SBC A,[IY+*]");

            lookupIY[0xA4] = New().Time(08).Size(2).And(A, IYH).Label("AND A,IYH");
            lookupIY[0xA5] = New().Time(08).Size(2).And(A, IYL).Label("AND A,IYL");
            lookupIY[0xA6] = New().Time(19).Size(3).And(A, IYIMM).Label("AND A,[IY+*]");
            lookupIY[0xAC] = New().Time(08).Size(2).Xor(A, IYH).Label("XOR A,IYH");
            lookupIY[0xAD] = New().Time(08).Size(2).Xor(A, IYL).Label("XOR A,IYL");
            lookupIY[0xAE] = New().Time(19).Size(3).Xor(A, IYIMM).Label("XOR A,[IY+*]");

            lookupIY[0xB4] = New().Time(08).Size(2).Or(A, IYH).Label("OR A,IYH");
            lookupIY[0xB5] = New().Time(08).Size(2).Or(A, IYL).Label("OR A,IYL");
            lookupIY[0xB6] = New().Time(19).Size(3).Or(A, IYIMM).Label("OR A,[IY+*]");
            lookupIY[0xBC] = New().Time(08).Size(2).Cp(A, IYH).Label("CP A,IYH");
            lookupIY[0xBD] = New().Time(08).Size(2).Cp(A, IYL).Label("CP A,IYL");
            lookupIY[0xBE] = New().Time(19).Size(3).Cp(A, IYIMM).Label("CP A,[IY+*]");

            lookupIY[0xE1] = New().Time(14).Size(2).Pop(SP, IY).Label("POP IY");
            lookupIY[0xE3] = New().Time(23).Size(2).Exchange(SPWP, IY).Label("EX [SP],IY");
            lookupIY[0xE5] = New().Time(15).Size(2).Push(SP, IY).Label("PUSH IY");
            lookupIY[0xE9] = New().Time(08).Size(2).Jump(IY).Label("JP [IY]");
            lookupIY[0xF9] = New().Time(10).Size(2).Load(SP, IY).Label("LD SP,IY");
            
            lookupIY[0xCB] = new InstructionBuilderComposite(3, PC, exBitsIY);

            //////////////////////////////////////////////////////////////////////////////////
            bits[0x00] = New().Time(8 ).Size(2).RotateLeftCarry(B).Label("RLC B");
            bits[0x01] = New().Time(8 ).Size(2).RotateLeftCarry(C).Label("RLC C");
            bits[0x02] = New().Time(8 ).Size(2).RotateLeftCarry(D).Label("RLC D");
            bits[0x03] = New().Time(8 ).Size(2).RotateLeftCarry(E).Label("RLC E");
            bits[0x04] = New().Time(8 ).Size(2).RotateLeftCarry(H).Label("RLC H");
            bits[0x05] = New().Time(8 ).Size(2).RotateLeftCarry(L).Label("RLC L");
            bits[0x06] = New().Time(15).Size(2).RotateLeftCarry(HLBP).Label("RLC (HL)");
            bits[0x07] = New().Time(8 ).Size(2).RotateLeftCarry(A).Label("RLC A");

            bits[0x08] = New().Time(8 ).Size(2).RotateRightCarry(B).Label("RRC B");
            bits[0x09] = New().Time(8 ).Size(2).RotateRightCarry(C).Label("RRC C");
            bits[0x0A] = New().Time(8 ).Size(2).RotateRightCarry(D).Label("RRC D");
            bits[0x0B] = New().Time(8 ).Size(2).RotateRightCarry(E).Label("RRC E");
            bits[0x0C] = New().Time(8 ).Size(2).RotateRightCarry(H).Label("RRC H");
            bits[0x0D] = New().Time(8 ).Size(2).RotateRightCarry(L).Label("RRC L");
            bits[0x0E] = New().Time(15).Size(2).RotateRightCarry(HLBP).Label("RRC (HL)");
            bits[0x0F] = New().Time(8 ).Size(2).RotateRightCarry(A).Label("RRC A");

            bits[0x10] = New().Time(8 ).Size(2).RotateLeft(B).Label("RL B"); 
            bits[0x11] = New().Time(8 ).Size(2).RotateLeft(C).Label("RL C"); 
            bits[0x12] = New().Time(8 ).Size(2).RotateLeft(D).Label("RL D"); 
            bits[0x13] = New().Time(8 ).Size(2).RotateLeft(E).Label("RL E"); 
            bits[0x14] = New().Time(8 ).Size(2).RotateLeft(H).Label("RL H"); 
            bits[0x15] = New().Time(8 ).Size(2).RotateLeft(L).Label("RL L"); 
            bits[0x16] = New().Time(15).Size(2).RotateLeft(HLBP).Label("RL (HL)");
            bits[0x17] = New().Time(8 ).Size(2).RotateLeft(A).Label("RL A"); 

            bits[0x18] = New().Time(8 ).Size(2).RotateRight(B).Label("RR B"); 
            bits[0x19] = New().Time(8 ).Size(2).RotateRight(C).Label("RR C"); 
            bits[0x1A] = New().Time(8 ).Size(2).RotateRight(D).Label("RR D"); 
            bits[0x1B] = New().Time(8 ).Size(2).RotateRight(E).Label("RR E"); 
            bits[0x1C] = New().Time(8 ).Size(2).RotateRight(H).Label("RR H"); 
            bits[0x1D] = New().Time(8 ).Size(2).RotateRight(L).Label("RR L"); 
            bits[0x1E] = New().Time(15).Size(2).RotateRight(HLBP).Label("RR (HL)");
            bits[0x1F] = New().Time(8 ).Size(2).RotateRight(A).Label("RR A"); 

            bits[0x20] = New().Time(8 ).Size(2).ShiftLeft(0,B).Label("SLA B");
            bits[0x21] = New().Time(8 ).Size(2).ShiftLeft(0,C).Label("SLA C");
            bits[0x22] = New().Time(8 ).Size(2).ShiftLeft(0,D).Label("SLA D");
            bits[0x23] = New().Time(8 ).Size(2).ShiftLeft(0,E).Label("SLA E");
            bits[0x24] = New().Time(8 ).Size(2).ShiftLeft(0,H).Label("SLA H");
            bits[0x25] = New().Time(8 ).Size(2).ShiftLeft(0,L).Label("SLA L");
            bits[0x26] = New().Time(15).Size(2).ShiftLeft(0,HLBP).Label("SLA (HL)");
            bits[0x27] = New().Time(8 ).Size(2).ShiftLeft(0,A).Label("SLA A");

            bits[0x28] = New().Time(8 ).Size(2).ShiftRight(1,B).Label("SRA B");
            bits[0x29] = New().Time(8 ).Size(2).ShiftRight(1,C).Label("SRA C");
            bits[0x2A] = New().Time(8 ).Size(2).ShiftRight(1,D).Label("SRA D");
            bits[0x2B] = New().Time(8 ).Size(2).ShiftRight(1,E).Label("SRA E");
            bits[0x2C] = New().Time(8 ).Size(2).ShiftRight(1,H).Label("SRA H");
            bits[0x2D] = New().Time(8 ).Size(2).ShiftRight(1,L).Label("SRA L");
            bits[0x2E] = New().Time(15).Size(2).ShiftRight(1,HLBP).Label("SRA (HL)");
            bits[0x2F] = New().Time(8 ).Size(2).ShiftRight(1,A).Label("SRA A");

            bits[0x30] = New().Time(8 ).Size(2).ShiftLeft(1,B).Label("SLL B");
            bits[0x31] = New().Time(8 ).Size(2).ShiftLeft(1,C).Label("SLL C");
            bits[0x32] = New().Time(8 ).Size(2).ShiftLeft(1,D).Label("SLL D");
            bits[0x33] = New().Time(8 ).Size(2).ShiftLeft(1,E).Label("SLL E");
            bits[0x34] = New().Time(8 ).Size(2).ShiftLeft(1,H).Label("SLL H");
            bits[0x35] = New().Time(8 ).Size(2).ShiftLeft(1,L).Label("SLL L");
            bits[0x36] = New().Time(15).Size(2).ShiftLeft(1,HLBP).Label("SLL (HL)");
            bits[0x37] = New().Time(8 ).Size(2).ShiftLeft(1,A).Label("SLL A");

            bits[0x38] = New().Time(8 ).Size(2).ShiftRight(0,B).Label("SRL B");
            bits[0x39] = New().Time(8 ).Size(2).ShiftRight(0,C).Label("SRL C");
            bits[0x3A] = New().Time(8 ).Size(2).ShiftRight(0,D).Label("SRL D");
            bits[0x3B] = New().Time(8 ).Size(2).ShiftRight(0,E).Label("SRL E");
            bits[0x3C] = New().Time(8 ).Size(2).ShiftRight(0,H).Label("SRL H");
            bits[0x3D] = New().Time(8 ).Size(2).ShiftRight(0,L).Label("SRL L");
            bits[0x3E] = New().Time(15).Size(2).ShiftRight(0,HLBP).Label("SRL (HL)");
            bits[0x3F] = New().Time(8 ).Size(2).ShiftRight(0,A).Label("SRL A");

            bits[0x40] = New().Time(8 ).Size(2).TestBit(0,B).Label("BIT 0,B");
            bits[0x41] = New().Time(8 ).Size(2).TestBit(0,C).Label("BIT 0,C");
            bits[0x42] = New().Time(8 ).Size(2).TestBit(0,D).Label("BIT 0,D");
            bits[0x43] = New().Time(8 ).Size(2).TestBit(0,E).Label("BIT 0,E");
            bits[0x44] = New().Time(8 ).Size(2).TestBit(0,H).Label("BIT 0,H");
            bits[0x45] = New().Time(8 ).Size(2).TestBit(0,L).Label("BIT 0,L");
            bits[0x46] = New().Time(12).Size(2).TestBit(0,HLBP).Label("BIT 0,(HL)");
            bits[0x47] = New().Time(8 ).Size(2).TestBit(0,A).Label("BIT 0,A");

            bits[0x48] = New().Time(8 ).Size(2).TestBit(1,B).Label("BIT 1,B");
            bits[0x49] = New().Time(8 ).Size(2).TestBit(1,C).Label("BIT 1,C");
            bits[0x4A] = New().Time(8 ).Size(2).TestBit(1,D).Label("BIT 1,D");
            bits[0x4B] = New().Time(8 ).Size(2).TestBit(1,E).Label("BIT 1,E");
            bits[0x4C] = New().Time(8 ).Size(2).TestBit(1,H).Label("BIT 1,H");
            bits[0x4D] = New().Time(8 ).Size(2).TestBit(1,L).Label("BIT 1,L");
            bits[0x4E] = New().Time(12).Size(2).TestBit(1,HLBP).Label("BIT 1,(HL)");
            bits[0x4F] = New().Time(8 ).Size(2).TestBit(1,A).Label("BIT 1,A");

            bits[0x50] = New().Time(8 ).Size(2).TestBit(2,B).Label("BIT 2,B");
            bits[0x51] = New().Time(8 ).Size(2).TestBit(2,C).Label("BIT 2,C");
            bits[0x52] = New().Time(8 ).Size(2).TestBit(2,D).Label("BIT 2,D");
            bits[0x53] = New().Time(8 ).Size(2).TestBit(2,E).Label("BIT 2,E");
            bits[0x54] = New().Time(8 ).Size(2).TestBit(2,H).Label("BIT 2,H");
            bits[0x55] = New().Time(8 ).Size(2).TestBit(2,L).Label("BIT 2,L");
            bits[0x56] = New().Time(12).Size(2).TestBit(2,HLBP).Label("BIT 2,(HL)");
            bits[0x57] = New().Time(8 ).Size(2).TestBit(2,A).Label("BIT 2,A");

            bits[0x58] = New().Time(8 ).Size(2).TestBit(3,B).Label("BIT 3,B");
            bits[0x59] = New().Time(8 ).Size(2).TestBit(3,C).Label("BIT 3,C");
            bits[0x5A] = New().Time(8 ).Size(2).TestBit(3,D).Label("BIT 3,D");
            bits[0x5B] = New().Time(8 ).Size(2).TestBit(3,E).Label("BIT 3,E");
            bits[0x5C] = New().Time(8 ).Size(2).TestBit(3,H).Label("BIT 3,H");
            bits[0x5D] = New().Time(8 ).Size(2).TestBit(3,L).Label("BIT 3,L");
            bits[0x5E] = New().Time(12).Size(2).TestBit(3,HLBP).Label("BIT 3,(HL)");
            bits[0x5F] = New().Time(8 ).Size(2).TestBit(3,A).Label("BIT 3,A");

            bits[0x60] = New().Time(8 ).Size(2).TestBit(4,B).Label("BIT 4,B");
            bits[0x61] = New().Time(8 ).Size(2).TestBit(4,C).Label("BIT 4,C");
            bits[0x62] = New().Time(8 ).Size(2).TestBit(4,D).Label("BIT 4,D");
            bits[0x63] = New().Time(8 ).Size(2).TestBit(4,E).Label("BIT 4,E");
            bits[0x64] = New().Time(8 ).Size(2).TestBit(4,H).Label("BIT 4,H");
            bits[0x65] = New().Time(8 ).Size(2).TestBit(4,L).Label("BIT 4,L");
            bits[0x66] = New().Time(12).Size(2).TestBit(4,HLBP).Label("BIT 4,(HL)");
            bits[0x67] = New().Time(8 ).Size(2).TestBit(4,A).Label("BIT 4,A");

            bits[0x68] = New().Time(8 ).Size(2).TestBit(5,B).Label("BIT 5,B");
            bits[0x69] = New().Time(8 ).Size(2).TestBit(5,C).Label("BIT 5,C");
            bits[0x6A] = New().Time(8 ).Size(2).TestBit(5,D).Label("BIT 5,D");
            bits[0x6B] = New().Time(8 ).Size(2).TestBit(5,E).Label("BIT 5,E");
            bits[0x6C] = New().Time(8 ).Size(2).TestBit(5,H).Label("BIT 5,H");
            bits[0x6D] = New().Time(8 ).Size(2).TestBit(5,L).Label("BIT 5,L");
            bits[0x6E] = New().Time(12).Size(2).TestBit(5,HLBP).Label("BIT 5,(HL)");
            bits[0x6F] = New().Time(8 ).Size(2).TestBit(5,A).Label("BIT 5,A");

            bits[0x70] = New().Time(8 ).Size(2).TestBit(6,B).Label("BIT 6,B");
            bits[0x71] = New().Time(8 ).Size(2).TestBit(6,C).Label("BIT 6,C");
            bits[0x72] = New().Time(8 ).Size(2).TestBit(6,D).Label("BIT 6,D");
            bits[0x73] = New().Time(8 ).Size(2).TestBit(6,E).Label("BIT 6,E");
            bits[0x74] = New().Time(8 ).Size(2).TestBit(6,H).Label("BIT 6,H");
            bits[0x75] = New().Time(8 ).Size(2).TestBit(6,L).Label("BIT 6,L");
            bits[0x76] = New().Time(12).Size(2).TestBit(6,HLBP).Label("BIT 6,(HL)");
            bits[0x77] = New().Time(8 ).Size(2).TestBit(6,A).Label("BIT 6,A");
                    
            bits[0x78] = New().Time(8 ).Size(2).TestBit(7,B).Label("BIT 7,B");
            bits[0x79] = New().Time(8 ).Size(2).TestBit(7,C).Label("BIT 7,C");
            bits[0x7A] = New().Time(8 ).Size(2).TestBit(7,D).Label("BIT 7,D");
            bits[0x7B] = New().Time(8 ).Size(2).TestBit(7,E).Label("BIT 7,E");
            bits[0x7C] = New().Time(8 ).Size(2).TestBit(7,H).Label("BIT 7,H");
            bits[0x7D] = New().Time(8 ).Size(2).TestBit(7,L).Label("BIT 7,L");
            bits[0x7E] = New().Time(12).Size(2).TestBit(7,HLBP).Label("BIT 7,(HL)");
            bits[0x7F] = New().Time(8 ).Size(2).TestBit(7,A).Label("BIT 7,A");

            bits[0x80] = New().Time(8 ).Size(2).ResetBit(0,B).Label("RES 0,B");
            bits[0x81] = New().Time(8 ).Size(2).ResetBit(0,C).Label("RES 0,C");
            bits[0x82] = New().Time(8 ).Size(2).ResetBit(0,D).Label("RES 0,D");
            bits[0x83] = New().Time(8 ).Size(2).ResetBit(0,E).Label("RES 0,E");
            bits[0x84] = New().Time(8 ).Size(2).ResetBit(0,H).Label("RES 0,H");
            bits[0x85] = New().Time(8 ).Size(2).ResetBit(0,L).Label("RES 0,L");
            bits[0x86] = New().Time(15).Size(2).ResetBit(0,HLBP).Label("RES 0,(HL)");
            bits[0x87] = New().Time(8 ).Size(2).ResetBit(0,A).Label("RES 0,A");
  
            bits[0x88] = New().Time(8 ).Size(2).ResetBit(1,B).Label("RES 1,B");
            bits[0x89] = New().Time(8 ).Size(2).ResetBit(1,C).Label("RES 1,C");
            bits[0x8A] = New().Time(8 ).Size(2).ResetBit(1,D).Label("RES 1,D");
            bits[0x8B] = New().Time(8 ).Size(2).ResetBit(1,E).Label("RES 1,E");
            bits[0x8C] = New().Time(8 ).Size(2).ResetBit(1,H).Label("RES 1,H");
            bits[0x8D] = New().Time(8 ).Size(2).ResetBit(1,L).Label("RES 1,L");
            bits[0x8E] = New().Time(15).Size(2).ResetBit(1,HLBP).Label("RES 1,(HL)");
            bits[0x8F] = New().Time(8 ).Size(2).ResetBit(1,A).Label("RES 1,A");
  
            bits[0x90] = New().Time(8 ).Size(2).ResetBit(2,B).Label("RES 2,B");
            bits[0x91] = New().Time(8 ).Size(2).ResetBit(2,C).Label("RES 2,C");
            bits[0x92] = New().Time(8 ).Size(2).ResetBit(2,D).Label("RES 2,D");
            bits[0x93] = New().Time(8 ).Size(2).ResetBit(2,E).Label("RES 2,E");
            bits[0x94] = New().Time(8 ).Size(2).ResetBit(2,H).Label("RES 2,H");
            bits[0x95] = New().Time(8 ).Size(2).ResetBit(2,L).Label("RES 2,L");
            bits[0x96] = New().Time(15).Size(2).ResetBit(2,HLBP).Label("RES 2,(HL)");
            bits[0x97] = New().Time(8 ).Size(2).ResetBit(2,A).Label("RES 2,A");
  
            bits[0x98] = New().Time(8 ).Size(2).ResetBit(3,B).Label("RES 3,B");
            bits[0x99] = New().Time(8 ).Size(2).ResetBit(3,C).Label("RES 3,C");
            bits[0x9A] = New().Time(8 ).Size(2).ResetBit(3,D).Label("RES 3,D");
            bits[0x9B] = New().Time(8 ).Size(2).ResetBit(3,E).Label("RES 3,E");
            bits[0x9C] = New().Time(8 ).Size(2).ResetBit(3,H).Label("RES 3,H");
            bits[0x9D] = New().Time(8 ).Size(2).ResetBit(3,L).Label("RES 3,L");
            bits[0x9E] = New().Time(15).Size(2).ResetBit(3,HLBP).Label("RES 3,(HL)");
            bits[0x9F] = New().Time(8 ).Size(2).ResetBit(3,A).Label("RES 3,A");
  
            bits[0xA0] = New().Time(8 ).Size(2).ResetBit(4,B).Label("RES 4,B");
            bits[0xA1] = New().Time(8 ).Size(2).ResetBit(4,C).Label("RES 4,C");
            bits[0xA2] = New().Time(8 ).Size(2).ResetBit(4,D).Label("RES 4,D");
            bits[0xA3] = New().Time(8 ).Size(2).ResetBit(4,E).Label("RES 4,E");
            bits[0xA4] = New().Time(8 ).Size(2).ResetBit(4,H).Label("RES 4,H");
            bits[0xA5] = New().Time(8 ).Size(2).ResetBit(4,L).Label("RES 4,L");
            bits[0xA6] = New().Time(15).Size(2).ResetBit(4,HLBP).Label("RES 4,(HL)");
            bits[0xA7] = New().Time(8 ).Size(2).ResetBit(4,A).Label("RES 4,A");
  
            bits[0xA8] = New().Time(8 ).Size(2).ResetBit(5,B).Label("RES 5,B");
            bits[0xA9] = New().Time(8 ).Size(2).ResetBit(5,C).Label("RES 5,C");
            bits[0xAA] = New().Time(8 ).Size(2).ResetBit(5,D).Label("RES 5,D");
            bits[0xAB] = New().Time(8 ).Size(2).ResetBit(5,E).Label("RES 5,E");
            bits[0xAC] = New().Time(8 ).Size(2).ResetBit(5,H).Label("RES 5,H");
            bits[0xAD] = New().Time(8 ).Size(2).ResetBit(5,L).Label("RES 5,L");
            bits[0xAE] = New().Time(15).Size(2).ResetBit(5,HLBP).Label("RES 5,(HL)");
            bits[0xAF] = New().Time(8 ).Size(2).ResetBit(5,A).Label("RES 5,A");
  
            bits[0xB0] = New().Time(8 ).Size(2).ResetBit(6,B).Label("RES 6,B");
            bits[0xB1] = New().Time(8 ).Size(2).ResetBit(6,C).Label("RES 6,C");
            bits[0xB2] = New().Time(8 ).Size(2).ResetBit(6,D).Label("RES 6,D");
            bits[0xB3] = New().Time(8 ).Size(2).ResetBit(6,E).Label("RES 6,E");
            bits[0xB4] = New().Time(8 ).Size(2).ResetBit(6,H).Label("RES 6,H");
            bits[0xB5] = New().Time(8 ).Size(2).ResetBit(6,L).Label("RES 6,L");
            bits[0xB6] = New().Time(15).Size(2).ResetBit(6,HLBP).Label("RES 6,(HL)");
            bits[0xB7] = New().Time(8 ).Size(2).ResetBit(6,A).Label("RES 6,A");
  
            bits[0xB8] = New().Time(8 ).Size(2).ResetBit(7,B).Label("RES 7,B");
            bits[0xB9] = New().Time(8 ).Size(2).ResetBit(7,C).Label("RES 7,C");
            bits[0xBA] = New().Time(8 ).Size(2).ResetBit(7,D).Label("RES 7,D");
            bits[0xBB] = New().Time(8 ).Size(2).ResetBit(7,E).Label("RES 7,E");
            bits[0xBC] = New().Time(8 ).Size(2).ResetBit(7,H).Label("RES 7,H");
            bits[0xBD] = New().Time(8 ).Size(2).ResetBit(7,L).Label("RES 7,L");
            bits[0xBE] = New().Time(15).Size(2).ResetBit(7,HLBP).Label("RES 7,(HL)");
            bits[0xBF] = New().Time(8 ).Size(2).ResetBit(7,A).Label("RES 7,A");

            bits[0xC0] = New().Time(8 ).Size(2).SetBit(0,B).Label("SET 0,B");
            bits[0xC1] = New().Time(8 ).Size(2).SetBit(0,C).Label("SET 0,C");
            bits[0xC2] = New().Time(8 ).Size(2).SetBit(0,D).Label("SET 0,D");
            bits[0xC3] = New().Time(8 ).Size(2).SetBit(0,E).Label("SET 0,E");
            bits[0xC4] = New().Time(8 ).Size(2).SetBit(0,H).Label("SET 0,H");
            bits[0xC5] = New().Time(8 ).Size(2).SetBit(0,L).Label("SET 0,L");
            bits[0xC6] = New().Time(15).Size(2).SetBit(0,HLBP).Label("SET 0,(HL)");
            bits[0xC7] = New().Time(8 ).Size(2).SetBit(0,A).Label("SET 0,A");

            bits[0xC8] = New().Time(8 ).Size(2).SetBit(1,B).Label("SET 1,B");
            bits[0xC9] = New().Time(8 ).Size(2).SetBit(1,C).Label("SET 1,C");
            bits[0xCA] = New().Time(8 ).Size(2).SetBit(1,D).Label("SET 1,D");
            bits[0xCB] = New().Time(8 ).Size(2).SetBit(1,E).Label("SET 1,E");
            bits[0xCC] = New().Time(8 ).Size(2).SetBit(1,H).Label("SET 1,H");
            bits[0xCD] = New().Time(8 ).Size(2).SetBit(1,L).Label("SET 1,L");
            bits[0xCE] = New().Time(15).Size(2).SetBit(1,HLBP).Label("SET 1,(HL)");
            bits[0xCF] = New().Time(8 ).Size(2).SetBit(1,A).Label("SET 1,A");

            bits[0xD0] = New().Time(8 ).Size(2).SetBit(2,B).Label("SET 2,B");
            bits[0xD1] = New().Time(8 ).Size(2).SetBit(2,C).Label("SET 2,C");
            bits[0xD2] = New().Time(8 ).Size(2).SetBit(2,D).Label("SET 2,D");
            bits[0xD3] = New().Time(8 ).Size(2).SetBit(2,E).Label("SET 2,E");
            bits[0xD4] = New().Time(8 ).Size(2).SetBit(2,H).Label("SET 2,H");
            bits[0xD5] = New().Time(8 ).Size(2).SetBit(2,L).Label("SET 2,L");
            bits[0xD6] = New().Time(15).Size(2).SetBit(2,HLBP).Label("SET 2,(HL)");
            bits[0xD7] = New().Time(8 ).Size(2).SetBit(2,A).Label("SET 2,A");

            bits[0xD8] = New().Time(8 ).Size(2).SetBit(3,B).Label("SET 3,B");
            bits[0xD9] = New().Time(8 ).Size(2).SetBit(3,C).Label("SET 3,C");
            bits[0xDA] = New().Time(8 ).Size(2).SetBit(3,D).Label("SET 3,D");
            bits[0xDB] = New().Time(8 ).Size(2).SetBit(3,E).Label("SET 3,E");
            bits[0xDC] = New().Time(8 ).Size(2).SetBit(3,H).Label("SET 3,H");
            bits[0xDD] = New().Time(8 ).Size(2).SetBit(3,L).Label("SET 3,L");
            bits[0xDE] = New().Time(15).Size(2).SetBit(3,HLBP).Label("SET 3,(HL)");
            bits[0xDF] = New().Time(8 ).Size(2).SetBit(3,A).Label("SET 3,A");

            bits[0xE0] = New().Time(8 ).Size(2).SetBit(4,B).Label("SET 4,B");
            bits[0xE1] = New().Time(8 ).Size(2).SetBit(4,C).Label("SET 4,C");
            bits[0xE2] = New().Time(8 ).Size(2).SetBit(4,D).Label("SET 4,D");
            bits[0xE3] = New().Time(8 ).Size(2).SetBit(4,E).Label("SET 4,E");
            bits[0xE4] = New().Time(8 ).Size(2).SetBit(4,H).Label("SET 4,H");
            bits[0xE5] = New().Time(8 ).Size(2).SetBit(4,L).Label("SET 4,L");
            bits[0xE6] = New().Time(15).Size(2).SetBit(4,HLBP).Label("SET 4,(HL)");
            bits[0xE7] = New().Time(8 ).Size(2).SetBit(4,A).Label("SET 4,A");

            bits[0xE8] = New().Time(8 ).Size(2).SetBit(5,B).Label("SET 5,B");
            bits[0xE9] = New().Time(8 ).Size(2).SetBit(5,C).Label("SET 5,C");
            bits[0xEA] = New().Time(8 ).Size(2).SetBit(5,D).Label("SET 5,D");
            bits[0xEB] = New().Time(8 ).Size(2).SetBit(5,E).Label("SET 5,E");
            bits[0xEC] = New().Time(8 ).Size(2).SetBit(5,H).Label("SET 5,H");
            bits[0xED] = New().Time(8 ).Size(2).SetBit(5,L).Label("SET 5,L");
            bits[0xEE] = New().Time(15).Size(2).SetBit(5,HLBP).Label("SET 5,(HL)");
            bits[0xEF] = New().Time(8 ).Size(2).SetBit(5,A).Label("SET 5,A");

            bits[0xF0] = New().Time(8 ).Size(2).SetBit(6,B).Label("SET 6,B");
            bits[0xF1] = New().Time(8 ).Size(2).SetBit(6,C).Label("SET 6,C");
            bits[0xF2] = New().Time(8 ).Size(2).SetBit(6,D).Label("SET 6,D");
            bits[0xF3] = New().Time(8 ).Size(2).SetBit(6,E).Label("SET 6,E");
            bits[0xF4] = New().Time(8 ).Size(2).SetBit(6,H).Label("SET 6,H");
            bits[0xF5] = New().Time(8 ).Size(2).SetBit(6,L).Label("SET 6,L");
            bits[0xF6] = New().Time(15).Size(2).SetBit(6,HLBP).Label("SET 6,(HL)");
            bits[0xF7] = New().Time(8 ).Size(2).SetBit(6,A).Label("SET 6,A");

            bits[0xF8] = New().Time(8 ).Size(2).SetBit(7,B).Label("SET 7,B");
            bits[0xF9] = New().Time(8 ).Size(2).SetBit(7,C).Label("SET 7,C");
            bits[0xFA] = New().Time(8 ).Size(2).SetBit(7,D).Label("SET 7,D");
            bits[0xFB] = New().Time(8 ).Size(2).SetBit(7,E).Label("SET 7,E");
            bits[0xFC] = New().Time(8 ).Size(2).SetBit(7,H).Label("SET 7,H");
            bits[0xFD] = New().Time(8 ).Size(2).SetBit(7,L).Label("SET 7,L");
            bits[0xFE] = New().Time(15).Size(2).SetBit(7,HLBP).Label("SET 7,(HL)");
            bits[0xFF] = New().Time(8 ).Size(2).SetBit(7,A).Label("SET 7,A"); 

            //////////////////////////////////////////////////////////////////////////////////
            exBitsIX[0x00] = New().Time(23).Size(4).RotateLeftCarry(IXIMM,B).Label("RLC [IX+*],B");
            exBitsIX[0x01] = New().Time(23).Size(4).RotateLeftCarry(IXIMM,C).Label("RLC [IX+*],C");
            exBitsIX[0x02] = New().Time(23).Size(4).RotateLeftCarry(IXIMM,D).Label("RLC [IX+*],D");
            exBitsIX[0x03] = New().Time(23).Size(4).RotateLeftCarry(IXIMM,E).Label("RLC [IX+*],E");
            exBitsIX[0x04] = New().Time(23).Size(4).RotateLeftCarry(IXIMM,H).Label("RLC [IX+*],H");
            exBitsIX[0x05] = New().Time(23).Size(4).RotateLeftCarry(IXIMM,L).Label("RLC [IX+*],L");
            exBitsIX[0x06] = New().Time(23).Size(4).RotateLeftCarry(IXIMM  ).Label("RLC [IX+*]");
            exBitsIX[0x07] = New().Time(23).Size(4).RotateLeftCarry(IXIMM,A).Label("RLC [IX+*],A");

            exBitsIX[0x08] = New().Time(23).Size(4).RotateRightCarry(IXIMM,B).Label("RRC [IX+*],B");
            exBitsIX[0x09] = New().Time(23).Size(4).RotateRightCarry(IXIMM,C).Label("RRC [IX+*],C");
            exBitsIX[0x0A] = New().Time(23).Size(4).RotateRightCarry(IXIMM,D).Label("RRC [IX+*],D");
            exBitsIX[0x0B] = New().Time(23).Size(4).RotateRightCarry(IXIMM,E).Label("RRC [IX+*],E");
            exBitsIX[0x0C] = New().Time(23).Size(4).RotateRightCarry(IXIMM,H).Label("RRC [IX+*],H");
            exBitsIX[0x0D] = New().Time(23).Size(4).RotateRightCarry(IXIMM,L).Label("RRC [IX+*],L");
            exBitsIX[0x0E] = New().Time(23).Size(4).RotateRightCarry(IXIMM  ).Label("RRC [IX+*]");
            exBitsIX[0x0F] = New().Time(23).Size(4).RotateRightCarry(IXIMM,A).Label("RRC [IX+*],A");

            exBitsIX[0x10] = New().Time(23).Size(4).RotateLeft(IXIMM,B).Label("RL [IX+*],B");
            exBitsIX[0x11] = New().Time(23).Size(4).RotateLeft(IXIMM,C).Label("RL [IX+*],C");
            exBitsIX[0x12] = New().Time(23).Size(4).RotateLeft(IXIMM,D).Label("RL [IX+*],D");
            exBitsIX[0x13] = New().Time(23).Size(4).RotateLeft(IXIMM,E).Label("RL [IX+*],E");
            exBitsIX[0x14] = New().Time(23).Size(4).RotateLeft(IXIMM,H).Label("RL [IX+*],H");
            exBitsIX[0x15] = New().Time(23).Size(4).RotateLeft(IXIMM,L).Label("RL [IX+*],L");
            exBitsIX[0x16] = New().Time(23).Size(4).RotateLeft(IXIMM  ).Label("RL [IX+*]");
            exBitsIX[0x17] = New().Time(23).Size(4).RotateLeft(IXIMM,A).Label("RL [IX+*],A");

            exBitsIX[0x18] = New().Time(23).Size(4).RotateRight(IXIMM,B).Label("RR [IX+*],B");
            exBitsIX[0x19] = New().Time(23).Size(4).RotateRight(IXIMM,C).Label("RR [IX+*],C");
            exBitsIX[0x1A] = New().Time(23).Size(4).RotateRight(IXIMM,D).Label("RR [IX+*],D");
            exBitsIX[0x1B] = New().Time(23).Size(4).RotateRight(IXIMM,E).Label("RR [IX+*],E");
            exBitsIX[0x1C] = New().Time(23).Size(4).RotateRight(IXIMM,H).Label("RR [IX+*],H");
            exBitsIX[0x1D] = New().Time(23).Size(4).RotateRight(IXIMM,L).Label("RR [IX+*],L");
            exBitsIX[0x1E] = New().Time(23).Size(4).RotateRight(IXIMM  ).Label("RR [IX+*]");
            exBitsIX[0x1F] = New().Time(23).Size(4).RotateRight(IXIMM,A).Label("RR [IX+*],A");

            exBitsIX[0x20] = New().Time(23).Size(4).ShiftLeft(0,IXIMM,B).Label("SLA [IX+*],B");
            exBitsIX[0x21] = New().Time(23).Size(4).ShiftLeft(0,IXIMM,C).Label("SLA [IX+*],C");
            exBitsIX[0x22] = New().Time(23).Size(4).ShiftLeft(0,IXIMM,D).Label("SLA [IX+*],D");
            exBitsIX[0x23] = New().Time(23).Size(4).ShiftLeft(0,IXIMM,E).Label("SLA [IX+*],E");
            exBitsIX[0x24] = New().Time(23).Size(4).ShiftLeft(0,IXIMM,H).Label("SLA [IX+*],H");
            exBitsIX[0x25] = New().Time(23).Size(4).ShiftLeft(0,IXIMM,L).Label("SLA [IX+*],L");
            exBitsIX[0x26] = New().Time(23).Size(4).ShiftLeft(0,IXIMM  ).Label("SLA [IX+*]");
            exBitsIX[0x27] = New().Time(23).Size(4).ShiftLeft(0,IXIMM,A).Label("SLA [IX+*],A");

            exBitsIX[0x28] = New().Time(23).Size(4).ShiftRight(1,IXIMM,B).Label("SRA [IX+*],B");
            exBitsIX[0x29] = New().Time(23).Size(4).ShiftRight(1,IXIMM,C).Label("SRA [IX+*],C");
            exBitsIX[0x2A] = New().Time(23).Size(4).ShiftRight(1,IXIMM,D).Label("SRA [IX+*],D");
            exBitsIX[0x2B] = New().Time(23).Size(4).ShiftRight(1,IXIMM,E).Label("SRA [IX+*],E");
            exBitsIX[0x2C] = New().Time(23).Size(4).ShiftRight(1,IXIMM,H).Label("SRA [IX+*],H");
            exBitsIX[0x2D] = New().Time(23).Size(4).ShiftRight(1,IXIMM,L).Label("SRA [IX+*],L");
            exBitsIX[0x2E] = New().Time(23).Size(4).ShiftRight(1,IXIMM  ).Label("SRA [IX+*]");
            exBitsIX[0x2F] = New().Time(23).Size(4).ShiftRight(1,IXIMM,A).Label("SRA [IX+*],A");

            exBitsIX[0x30] = New().Time(23).Size(4).ShiftLeft(1,IXIMM,B).Label("SLL [IX+*],B");
            exBitsIX[0x31] = New().Time(23).Size(4).ShiftLeft(1,IXIMM,C).Label("SLL [IX+*],C");
            exBitsIX[0x32] = New().Time(23).Size(4).ShiftLeft(1,IXIMM,D).Label("SLL [IX+*],D");
            exBitsIX[0x33] = New().Time(23).Size(4).ShiftLeft(1,IXIMM,E).Label("SLL [IX+*],E");
            exBitsIX[0x34] = New().Time(23).Size(4).ShiftLeft(1,IXIMM,H).Label("SLL [IX+*],H");
            exBitsIX[0x35] = New().Time(23).Size(4).ShiftLeft(1,IXIMM,L).Label("SLL [IX+*],L");
            exBitsIX[0x36] = New().Time(23).Size(4).ShiftLeft(1,IXIMM  ).Label("SLL [IX+*]");
            exBitsIX[0x37] = New().Time(23).Size(4).ShiftLeft(1,IXIMM,A).Label("SLL [IX+*],A");

            exBitsIX[0x38] = New().Time(23).Size(4).ShiftRight(0,IXIMM,B).Label("SRL [IX+*],B");
            exBitsIX[0x39] = New().Time(23).Size(4).ShiftRight(0,IXIMM,C).Label("SRL [IX+*],C");
            exBitsIX[0x3A] = New().Time(23).Size(4).ShiftRight(0,IXIMM,D).Label("SRL [IX+*],D");
            exBitsIX[0x3B] = New().Time(23).Size(4).ShiftRight(0,IXIMM,E).Label("SRL [IX+*],E");
            exBitsIX[0x3C] = New().Time(23).Size(4).ShiftRight(0,IXIMM,H).Label("SRL [IX+*],H");
            exBitsIX[0x3D] = New().Time(23).Size(4).ShiftRight(0,IXIMM,L).Label("SRL [IX+*],L");
            exBitsIX[0x3E] = New().Time(23).Size(4).ShiftRight(0,IXIMM  ).Label("SRL [IX+*]");
            exBitsIX[0x3F] = New().Time(23).Size(4).ShiftRight(0,IXIMM,A).Label("SRL [IX+*],A");

            exBitsIX[0x40] = 
            exBitsIX[0x41] = 
            exBitsIX[0x42] = 
            exBitsIX[0x43] = 
            exBitsIX[0x44] = 
            exBitsIX[0x45] = 
            exBitsIX[0x46] = 
            exBitsIX[0x47] = New().Time(20).Size(4).TestBit(0,IXIMM).Label("BIT 0,[IX+*]");

            exBitsIX[0x48] = 
            exBitsIX[0x49] = 
            exBitsIX[0x4A] = 
            exBitsIX[0x4B] = 
            exBitsIX[0x4C] = 
            exBitsIX[0x4D] = 
            exBitsIX[0x4E] = 
            exBitsIX[0x4F] = New().Time(20).Size(4).TestBit(1,IXIMM).Label("BIT 1,[IX+*]");

            exBitsIX[0x50] = 
            exBitsIX[0x51] = 
            exBitsIX[0x52] = 
            exBitsIX[0x53] = 
            exBitsIX[0x54] = 
            exBitsIX[0x55] = 
            exBitsIX[0x56] = 
            exBitsIX[0x57] = New().Time(20).Size(4).TestBit(2,IXIMM).Label("BIT 2,[IX+*]");

            exBitsIX[0x58] = 
            exBitsIX[0x59] = 
            exBitsIX[0x5A] = 
            exBitsIX[0x5B] = 
            exBitsIX[0x5C] = 
            exBitsIX[0x5D] = 
            exBitsIX[0x5E] = 
            exBitsIX[0x5F] = New().Time(20).Size(4).TestBit(3,IXIMM).Label("BIT 3,[IX+*]");

            exBitsIX[0x60] = 
            exBitsIX[0x61] = 
            exBitsIX[0x62] = 
            exBitsIX[0x63] = 
            exBitsIX[0x64] = 
            exBitsIX[0x65] = 
            exBitsIX[0x66] = 
            exBitsIX[0x67] = New().Time(20).Size(4).TestBit(4,IXIMM).Label("BIT 4,[IX+*]");

            exBitsIX[0x68] = 
            exBitsIX[0x69] = 
            exBitsIX[0x6A] = 
            exBitsIX[0x6B] = 
            exBitsIX[0x6C] = 
            exBitsIX[0x6D] = 
            exBitsIX[0x6E] = 
            exBitsIX[0x6F] = New().Time(20).Size(4).TestBit(5,IXIMM).Label("BIT 5,[IX+*]");

            exBitsIX[0x70] = 
            exBitsIX[0x71] = 
            exBitsIX[0x72] = 
            exBitsIX[0x73] = 
            exBitsIX[0x74] = 
            exBitsIX[0x75] = 
            exBitsIX[0x76] = 
            exBitsIX[0x77] = New().Time(20).Size(4).TestBit(6,IXIMM).Label("BIT 6,[IX+*]");

            exBitsIX[0x78] = 
            exBitsIX[0x79] = 
            exBitsIX[0x7A] = 
            exBitsIX[0x7B] = 
            exBitsIX[0x7C] = 
            exBitsIX[0x7D] = 
            exBitsIX[0x7E] = 
            exBitsIX[0x7F] = New().Time(20).Size(4).TestBit(7,IXIMM).Label("BIT 7,[IX+*]");

            exBitsIX[0x80] = New().Time(23).Size(4).ResetBit(0,IXIMM,B).Label("RES 0,[IX+*],B");
            exBitsIX[0x81] = New().Time(23).Size(4).ResetBit(0,IXIMM,C).Label("RES 0,[IX+*],C");
            exBitsIX[0x82] = New().Time(23).Size(4).ResetBit(0,IXIMM,D).Label("RES 0,[IX+*],D");
            exBitsIX[0x83] = New().Time(23).Size(4).ResetBit(0,IXIMM,E).Label("RES 0,[IX+*],E");
            exBitsIX[0x84] = New().Time(23).Size(4).ResetBit(0,IXIMM,H).Label("RES 0,[IX+*],H");
            exBitsIX[0x85] = New().Time(23).Size(4).ResetBit(0,IXIMM,L).Label("RES 0,[IX+*],L");
            exBitsIX[0x86] = New().Time(23).Size(4).ResetBit(0,IXIMM  ).Label("RES 0,[IX+*]");
            exBitsIX[0x87] = New().Time(23).Size(4).ResetBit(0,IXIMM,A).Label("RES 0,[IX+*],A");

            exBitsIX[0x88] = New().Time(23).Size(4).ResetBit(1,IXIMM,B).Label("RES 1,[IX+*],B");
            exBitsIX[0x89] = New().Time(23).Size(4).ResetBit(1,IXIMM,C).Label("RES 1,[IX+*],C");
            exBitsIX[0x8A] = New().Time(23).Size(4).ResetBit(1,IXIMM,D).Label("RES 1,[IX+*],D");
            exBitsIX[0x8B] = New().Time(23).Size(4).ResetBit(1,IXIMM,E).Label("RES 1,[IX+*],E");
            exBitsIX[0x8C] = New().Time(23).Size(4).ResetBit(1,IXIMM,H).Label("RES 1,[IX+*],H");
            exBitsIX[0x8D] = New().Time(23).Size(4).ResetBit(1,IXIMM,L).Label("RES 1,[IX+*],L");
            exBitsIX[0x8E] = New().Time(23).Size(4).ResetBit(1,IXIMM  ).Label("RES 1,[IX+*]");
            exBitsIX[0x8F] = New().Time(23).Size(4).ResetBit(1,IXIMM,A).Label("RES 1,[IX+*],A");

            exBitsIX[0x90] = New().Time(23).Size(4).ResetBit(2,IXIMM,B).Label("RES 2,[IX+*],B");
            exBitsIX[0x91] = New().Time(23).Size(4).ResetBit(2,IXIMM,C).Label("RES 2,[IX+*],C");
            exBitsIX[0x92] = New().Time(23).Size(4).ResetBit(2,IXIMM,D).Label("RES 2,[IX+*],D");
            exBitsIX[0x93] = New().Time(23).Size(4).ResetBit(2,IXIMM,E).Label("RES 2,[IX+*],E");
            exBitsIX[0x94] = New().Time(23).Size(4).ResetBit(2,IXIMM,H).Label("RES 2,[IX+*],H");
            exBitsIX[0x95] = New().Time(23).Size(4).ResetBit(2,IXIMM,L).Label("RES 2,[IX+*],L");
            exBitsIX[0x96] = New().Time(23).Size(4).ResetBit(2,IXIMM  ).Label("RES 2,[IX+*]");
            exBitsIX[0x97] = New().Time(23).Size(4).ResetBit(2,IXIMM,A).Label("RES 2,[IX+*],A");

            exBitsIX[0x98] = New().Time(23).Size(4).ResetBit(3,IXIMM,B).Label("RES 3,[IX+*],B");
            exBitsIX[0x99] = New().Time(23).Size(4).ResetBit(3,IXIMM,C).Label("RES 3,[IX+*],C");
            exBitsIX[0x9A] = New().Time(23).Size(4).ResetBit(3,IXIMM,D).Label("RES 3,[IX+*],D");
            exBitsIX[0x9B] = New().Time(23).Size(4).ResetBit(3,IXIMM,E).Label("RES 3,[IX+*],E");
            exBitsIX[0x9C] = New().Time(23).Size(4).ResetBit(3,IXIMM,H).Label("RES 3,[IX+*],H");
            exBitsIX[0x9D] = New().Time(23).Size(4).ResetBit(3,IXIMM,L).Label("RES 3,[IX+*],L");
            exBitsIX[0x9E] = New().Time(23).Size(4).ResetBit(3,IXIMM  ).Label("RES 3,[IX+*]");
            exBitsIX[0x9F] = New().Time(23).Size(4).ResetBit(3,IXIMM,A).Label("RES 3,[IX+*],A");

            exBitsIX[0xA0] = New().Time(23).Size(4).ResetBit(4,IXIMM,B).Label("RES 4,[IX+*],B");
            exBitsIX[0xA1] = New().Time(23).Size(4).ResetBit(4,IXIMM,C).Label("RES 4,[IX+*],C");
            exBitsIX[0xA2] = New().Time(23).Size(4).ResetBit(4,IXIMM,D).Label("RES 4,[IX+*],D");
            exBitsIX[0xA3] = New().Time(23).Size(4).ResetBit(4,IXIMM,E).Label("RES 4,[IX+*],E");
            exBitsIX[0xA4] = New().Time(23).Size(4).ResetBit(4,IXIMM,H).Label("RES 4,[IX+*],H");
            exBitsIX[0xA5] = New().Time(23).Size(4).ResetBit(4,IXIMM,L).Label("RES 4,[IX+*],L");
            exBitsIX[0xA6] = New().Time(23).Size(4).ResetBit(4,IXIMM  ).Label("RES 4,[IX+*]");
            exBitsIX[0xA7] = New().Time(23).Size(4).ResetBit(4,IXIMM,A).Label("RES 4,[IX+*],A");

            exBitsIX[0xA8] = New().Time(23).Size(4).ResetBit(5,IXIMM,B).Label("RES 5,[IX+*],B");
            exBitsIX[0xA9] = New().Time(23).Size(4).ResetBit(5,IXIMM,C).Label("RES 5,[IX+*],C");
            exBitsIX[0xAA] = New().Time(23).Size(4).ResetBit(5,IXIMM,D).Label("RES 5,[IX+*],D");
            exBitsIX[0xAB] = New().Time(23).Size(4).ResetBit(5,IXIMM,E).Label("RES 5,[IX+*],E");
            exBitsIX[0xAC] = New().Time(23).Size(4).ResetBit(5,IXIMM,H).Label("RES 5,[IX+*],H");
            exBitsIX[0xAD] = New().Time(23).Size(4).ResetBit(5,IXIMM,L).Label("RES 5,[IX+*],L");
            exBitsIX[0xAE] = New().Time(23).Size(4).ResetBit(5,IXIMM  ).Label("RES 5,[IX+*]");
            exBitsIX[0xAF] = New().Time(23).Size(4).ResetBit(5,IXIMM,A).Label("RES 5,[IX+*],A");
            
            exBitsIX[0xB0] = New().Time(23).Size(4).ResetBit(6,IXIMM,B).Label("RES 6,[IX+*],B");
            exBitsIX[0xB1] = New().Time(23).Size(4).ResetBit(6,IXIMM,C).Label("RES 6,[IX+*],C");
            exBitsIX[0xB2] = New().Time(23).Size(4).ResetBit(6,IXIMM,D).Label("RES 6,[IX+*],D");
            exBitsIX[0xB3] = New().Time(23).Size(4).ResetBit(6,IXIMM,E).Label("RES 6,[IX+*],E");
            exBitsIX[0xB4] = New().Time(23).Size(4).ResetBit(6,IXIMM,H).Label("RES 6,[IX+*],H");
            exBitsIX[0xB5] = New().Time(23).Size(4).ResetBit(6,IXIMM,L).Label("RES 6,[IX+*],L");
            exBitsIX[0xB6] = New().Time(23).Size(4).ResetBit(6,IXIMM  ).Label("RES 6,[IX+*]");
            exBitsIX[0xB7] = New().Time(23).Size(4).ResetBit(6,IXIMM,A).Label("RES 6,[IX+*],A");

            exBitsIX[0xB8] = New().Time(23).Size(4).ResetBit(7,IXIMM,B).Label("RES 7,[IX+*],B");
            exBitsIX[0xB9] = New().Time(23).Size(4).ResetBit(7,IXIMM,C).Label("RES 7,[IX+*],C");
            exBitsIX[0xBA] = New().Time(23).Size(4).ResetBit(7,IXIMM,D).Label("RES 7,[IX+*],D");
            exBitsIX[0xBB] = New().Time(23).Size(4).ResetBit(7,IXIMM,E).Label("RES 7,[IX+*],E");
            exBitsIX[0xBC] = New().Time(23).Size(4).ResetBit(7,IXIMM,H).Label("RES 7,[IX+*],H");
            exBitsIX[0xBD] = New().Time(23).Size(4).ResetBit(7,IXIMM,L).Label("RES 7,[IX+*],L");
            exBitsIX[0xBE] = New().Time(23).Size(4).ResetBit(7,IXIMM  ).Label("RES 7,[IX+*]");
            exBitsIX[0xBF] = New().Time(23).Size(4).ResetBit(7,IXIMM,A).Label("RES 7,[IX+*],A");

            exBitsIX[0xC0] = New().Time(23).Size(4).SetBit(0,IXIMM,B).Label("SET 0,[IX+*],B");
            exBitsIX[0xC1] = New().Time(23).Size(4).SetBit(0,IXIMM,C).Label("SET 0,[IX+*],C");
            exBitsIX[0xC2] = New().Time(23).Size(4).SetBit(0,IXIMM,D).Label("SET 0,[IX+*],D");
            exBitsIX[0xC3] = New().Time(23).Size(4).SetBit(0,IXIMM,E).Label("SET 0,[IX+*],E");
            exBitsIX[0xC4] = New().Time(23).Size(4).SetBit(0,IXIMM,H).Label("SET 0,[IX+*],H");
            exBitsIX[0xC5] = New().Time(23).Size(4).SetBit(0,IXIMM,L).Label("SET 0,[IX+*],L");
            exBitsIX[0xC6] = New().Time(23).Size(4).SetBit(0,IXIMM  ).Label("SET 0,[IX+*]");
            exBitsIX[0xC7] = New().Time(23).Size(4).SetBit(0,IXIMM,A).Label("SET 0,[IX+*],A");
        
            exBitsIX[0xC8] = New().Time(23).Size(4).SetBit(1,IXIMM,B).Label("SET 1,[IX+*],B");
            exBitsIX[0xC9] = New().Time(23).Size(4).SetBit(1,IXIMM,C).Label("SET 1,[IX+*],C");
            exBitsIX[0xCA] = New().Time(23).Size(4).SetBit(1,IXIMM,D).Label("SET 1,[IX+*],D");
            exBitsIX[0xCB] = New().Time(23).Size(4).SetBit(1,IXIMM,E).Label("SET 1,[IX+*],E");
            exBitsIX[0xCC] = New().Time(23).Size(4).SetBit(1,IXIMM,H).Label("SET 1,[IX+*],H");
            exBitsIX[0xCD] = New().Time(23).Size(4).SetBit(1,IXIMM,L).Label("SET 1,[IX+*],L");
            exBitsIX[0xCE] = New().Time(23).Size(4).SetBit(1,IXIMM  ).Label("SET 1,[IX+*]");
            exBitsIX[0xCF] = New().Time(23).Size(4).SetBit(1,IXIMM,A).Label("SET 1,[IX+*],A");
                  
            exBitsIX[0xD0] = New().Time(23).Size(4).SetBit(2,IXIMM,B).Label("SET 2,[IX+*],B");
            exBitsIX[0xD1] = New().Time(23).Size(4).SetBit(2,IXIMM,C).Label("SET 2,[IX+*],C");
            exBitsIX[0xD2] = New().Time(23).Size(4).SetBit(2,IXIMM,D).Label("SET 2,[IX+*],D");
            exBitsIX[0xD3] = New().Time(23).Size(4).SetBit(2,IXIMM,E).Label("SET 2,[IX+*],E");
            exBitsIX[0xD4] = New().Time(23).Size(4).SetBit(2,IXIMM,H).Label("SET 2,[IX+*],H");
            exBitsIX[0xD5] = New().Time(23).Size(4).SetBit(2,IXIMM,L).Label("SET 2,[IX+*],L");
            exBitsIX[0xD6] = New().Time(23).Size(4).SetBit(2,IXIMM  ).Label("SET 2,[IX+*]");
            exBitsIX[0xD7] = New().Time(23).Size(4).SetBit(2,IXIMM,A).Label("SET 2,[IX+*],A");
                  
            exBitsIX[0xD8] = New().Time(23).Size(4).SetBit(3,IXIMM,B).Label("SET 3,[IX+*],B");
            exBitsIX[0xD9] = New().Time(23).Size(4).SetBit(3,IXIMM,C).Label("SET 3,[IX+*],C");
            exBitsIX[0xDA] = New().Time(23).Size(4).SetBit(3,IXIMM,D).Label("SET 3,[IX+*],D");
            exBitsIX[0xDB] = New().Time(23).Size(4).SetBit(3,IXIMM,E).Label("SET 3,[IX+*],E");
            exBitsIX[0xDC] = New().Time(23).Size(4).SetBit(3,IXIMM,H).Label("SET 3,[IX+*],H");
            exBitsIX[0xDD] = New().Time(23).Size(4).SetBit(3,IXIMM,L).Label("SET 3,[IX+*],L");
            exBitsIX[0xDE] = New().Time(23).Size(4).SetBit(3,IXIMM  ).Label("SET 3,[IX+*]");
            exBitsIX[0xDF] = New().Time(23).Size(4).SetBit(3,IXIMM,A).Label("SET 3,[IX+*],A");
                  
            exBitsIX[0xE0] = New().Time(23).Size(4).SetBit(4,IXIMM,B).Label("SET 4,[IX+*],B");
            exBitsIX[0xE1] = New().Time(23).Size(4).SetBit(4,IXIMM,C).Label("SET 4,[IX+*],C");
            exBitsIX[0xE2] = New().Time(23).Size(4).SetBit(4,IXIMM,D).Label("SET 4,[IX+*],D");
            exBitsIX[0xE3] = New().Time(23).Size(4).SetBit(4,IXIMM,E).Label("SET 4,[IX+*],E");
            exBitsIX[0xE4] = New().Time(23).Size(4).SetBit(4,IXIMM,H).Label("SET 4,[IX+*],H");
            exBitsIX[0xE5] = New().Time(23).Size(4).SetBit(4,IXIMM,L).Label("SET 4,[IX+*],L");
            exBitsIX[0xE6] = New().Time(23).Size(4).SetBit(4,IXIMM  ).Label("SET 4,[IX+*]");
            exBitsIX[0xE7] = New().Time(23).Size(4).SetBit(4,IXIMM,A).Label("SET 4,[IX+*],A");
                  
            exBitsIX[0xE8] = New().Time(23).Size(4).SetBit(5,IXIMM,B).Label("SET 5,[IX+*],B");
            exBitsIX[0xE9] = New().Time(23).Size(4).SetBit(5,IXIMM,C).Label("SET 5,[IX+*],C");
            exBitsIX[0xEA] = New().Time(23).Size(4).SetBit(5,IXIMM,D).Label("SET 5,[IX+*],D");
            exBitsIX[0xEB] = New().Time(23).Size(4).SetBit(5,IXIMM,E).Label("SET 5,[IX+*],E");
            exBitsIX[0xEC] = New().Time(23).Size(4).SetBit(5,IXIMM,H).Label("SET 5,[IX+*],H");
            exBitsIX[0xED] = New().Time(23).Size(4).SetBit(5,IXIMM,L).Label("SET 5,[IX+*],L");
            exBitsIX[0xEE] = New().Time(23).Size(4).SetBit(5,IXIMM  ).Label("SET 5,[IX+*]");
            exBitsIX[0xEF] = New().Time(23).Size(4).SetBit(5,IXIMM,A).Label("SET 5,[IX+*],A");
                  
            exBitsIX[0xF0] = New().Time(23).Size(4).SetBit(6,IXIMM,B).Label("SET 6,[IX+*],B");
            exBitsIX[0xF1] = New().Time(23).Size(4).SetBit(6,IXIMM,C).Label("SET 6,[IX+*],C");
            exBitsIX[0xF2] = New().Time(23).Size(4).SetBit(6,IXIMM,D).Label("SET 6,[IX+*],D");
            exBitsIX[0xF3] = New().Time(23).Size(4).SetBit(6,IXIMM,E).Label("SET 6,[IX+*],E");
            exBitsIX[0xF4] = New().Time(23).Size(4).SetBit(6,IXIMM,H).Label("SET 6,[IX+*],H");
            exBitsIX[0xF5] = New().Time(23).Size(4).SetBit(6,IXIMM,L).Label("SET 6,[IX+*],L");
            exBitsIX[0xF6] = New().Time(23).Size(4).SetBit(6,IXIMM  ).Label("SET 6,[IX+*]");
            exBitsIX[0xF7] = New().Time(23).Size(4).SetBit(6,IXIMM,A).Label("SET 6,[IX+*],A");
                  
            exBitsIX[0xF8] = New().Time(23).Size(4).SetBit(7,IXIMM,B).Label("SET 7,[IX+*],B");
            exBitsIX[0xF9] = New().Time(23).Size(4).SetBit(7,IXIMM,C).Label("SET 7,[IX+*],C");
            exBitsIX[0xFA] = New().Time(23).Size(4).SetBit(7,IXIMM,D).Label("SET 7,[IX+*],D");
            exBitsIX[0xFB] = New().Time(23).Size(4).SetBit(7,IXIMM,E).Label("SET 7,[IX+*],E");
            exBitsIX[0xFC] = New().Time(23).Size(4).SetBit(7,IXIMM,H).Label("SET 7,[IX+*],H");
            exBitsIX[0xFD] = New().Time(23).Size(4).SetBit(7,IXIMM,L).Label("SET 7,[IX+*],L");
            exBitsIX[0xFE] = New().Time(23).Size(4).SetBit(7,IXIMM  ).Label("SET 7,[IX+*]");
            exBitsIX[0xFF] = New().Time(23).Size(4).SetBit(7,IXIMM,A).Label("SET 7,[IX+*],A");

            //////////////////////////////////////////////////////////////////////////////////
            exBitsIY[0x00] = New().Time(23).Size(4).RotateLeftCarry(IYIMM,B).Label("RLC [IY+*],B");
            exBitsIY[0x01] = New().Time(23).Size(4).RotateLeftCarry(IYIMM,C).Label("RLC [IY+*],C");
            exBitsIY[0x02] = New().Time(23).Size(4).RotateLeftCarry(IYIMM,D).Label("RLC [IY+*],D");
            exBitsIY[0x03] = New().Time(23).Size(4).RotateLeftCarry(IYIMM,E).Label("RLC [IY+*],E");
            exBitsIY[0x04] = New().Time(23).Size(4).RotateLeftCarry(IYIMM,H).Label("RLC [IY+*],H");
            exBitsIY[0x05] = New().Time(23).Size(4).RotateLeftCarry(IYIMM,L).Label("RLC [IY+*],L");
            exBitsIY[0x06] = New().Time(23).Size(4).RotateLeftCarry(IYIMM  ).Label("RLC [IY+*]");
            exBitsIY[0x07] = New().Time(23).Size(4).RotateLeftCarry(IYIMM,A).Label("RLC [IY+*],A");

            exBitsIY[0x08] = New().Time(23).Size(4).RotateRightCarry(IYIMM,B).Label("RRC [IY+*],B");
            exBitsIY[0x09] = New().Time(23).Size(4).RotateRightCarry(IYIMM,C).Label("RRC [IY+*],C");
            exBitsIY[0x0A] = New().Time(23).Size(4).RotateRightCarry(IYIMM,D).Label("RRC [IY+*],D");
            exBitsIY[0x0B] = New().Time(23).Size(4).RotateRightCarry(IYIMM,E).Label("RRC [IY+*],E");
            exBitsIY[0x0C] = New().Time(23).Size(4).RotateRightCarry(IYIMM,H).Label("RRC [IY+*],H");
            exBitsIY[0x0D] = New().Time(23).Size(4).RotateRightCarry(IYIMM,L).Label("RRC [IY+*],L");
            exBitsIY[0x0E] = New().Time(23).Size(4).RotateRightCarry(IYIMM  ).Label("RRC [IY+*]");
            exBitsIY[0x0F] = New().Time(23).Size(4).RotateRightCarry(IYIMM,A).Label("RRC [IY+*],A");

            exBitsIY[0x10] = New().Time(23).Size(4).RotateLeft(IYIMM,B).Label("RL [IY+*],B");
            exBitsIY[0x11] = New().Time(23).Size(4).RotateLeft(IYIMM,C).Label("RL [IY+*],C");
            exBitsIY[0x12] = New().Time(23).Size(4).RotateLeft(IYIMM,D).Label("RL [IY+*],D");
            exBitsIY[0x13] = New().Time(23).Size(4).RotateLeft(IYIMM,E).Label("RL [IY+*],E");
            exBitsIY[0x14] = New().Time(23).Size(4).RotateLeft(IYIMM,H).Label("RL [IY+*],H");
            exBitsIY[0x15] = New().Time(23).Size(4).RotateLeft(IYIMM,L).Label("RL [IY+*],L");
            exBitsIY[0x16] = New().Time(23).Size(4).RotateLeft(IYIMM  ).Label("RL [IY+*]");
            exBitsIY[0x17] = New().Time(23).Size(4).RotateLeft(IYIMM,A).Label("RL [IY+*],A");

            exBitsIY[0x18] = New().Time(23).Size(4).RotateRight(IYIMM,B).Label("RR [IY+*],B");
            exBitsIY[0x19] = New().Time(23).Size(4).RotateRight(IYIMM,C).Label("RR [IY+*],C");
            exBitsIY[0x1A] = New().Time(23).Size(4).RotateRight(IYIMM,D).Label("RR [IY+*],D");
            exBitsIY[0x1B] = New().Time(23).Size(4).RotateRight(IYIMM,E).Label("RR [IY+*],E");
            exBitsIY[0x1C] = New().Time(23).Size(4).RotateRight(IYIMM,H).Label("RR [IY+*],H");
            exBitsIY[0x1D] = New().Time(23).Size(4).RotateRight(IYIMM,L).Label("RR [IY+*],L");
            exBitsIY[0x1E] = New().Time(23).Size(4).RotateRight(IYIMM  ).Label("RR [IY+*]");
            exBitsIY[0x1F] = New().Time(23).Size(4).RotateRight(IYIMM,A).Label("RR [IY+*],A");

            exBitsIY[0x20] = New().Time(23).Size(4).ShiftLeft(0,IYIMM,B).Label("SLA [IY+*],B");
            exBitsIY[0x21] = New().Time(23).Size(4).ShiftLeft(0,IYIMM,C).Label("SLA [IY+*],C");
            exBitsIY[0x22] = New().Time(23).Size(4).ShiftLeft(0,IYIMM,D).Label("SLA [IY+*],D");
            exBitsIY[0x23] = New().Time(23).Size(4).ShiftLeft(0,IYIMM,E).Label("SLA [IY+*],E");
            exBitsIY[0x24] = New().Time(23).Size(4).ShiftLeft(0,IYIMM,H).Label("SLA [IY+*],H");
            exBitsIY[0x25] = New().Time(23).Size(4).ShiftLeft(0,IYIMM,L).Label("SLA [IY+*],L");
            exBitsIY[0x26] = New().Time(23).Size(4).ShiftLeft(0,IYIMM  ).Label("SLA [IY+*]");
            exBitsIY[0x27] = New().Time(23).Size(4).ShiftLeft(0,IYIMM,A).Label("SLA [IY+*],A");

            exBitsIY[0x28] = New().Time(23).Size(4).ShiftRight(1,IYIMM,B).Label("SRA [IY+*],B");
            exBitsIY[0x29] = New().Time(23).Size(4).ShiftRight(1,IYIMM,C).Label("SRA [IY+*],C");
            exBitsIY[0x2A] = New().Time(23).Size(4).ShiftRight(1,IYIMM,D).Label("SRA [IY+*],D");
            exBitsIY[0x2B] = New().Time(23).Size(4).ShiftRight(1,IYIMM,E).Label("SRA [IY+*],E");
            exBitsIY[0x2C] = New().Time(23).Size(4).ShiftRight(1,IYIMM,H).Label("SRA [IY+*],H");
            exBitsIY[0x2D] = New().Time(23).Size(4).ShiftRight(1,IYIMM,L).Label("SRA [IY+*],L");
            exBitsIY[0x2E] = New().Time(23).Size(4).ShiftRight(1,IYIMM  ).Label("SRA [IY+*]");
            exBitsIY[0x2F] = New().Time(23).Size(4).ShiftRight(1,IYIMM,A).Label("SRA [IY+*],A");

            exBitsIY[0x30] = New().Time(23).Size(4).ShiftLeft(1,IYIMM,B).Label("SLL [IY+*],B");
            exBitsIY[0x31] = New().Time(23).Size(4).ShiftLeft(1,IYIMM,C).Label("SLL [IY+*],C");
            exBitsIY[0x32] = New().Time(23).Size(4).ShiftLeft(1,IYIMM,D).Label("SLL [IY+*],D");
            exBitsIY[0x33] = New().Time(23).Size(4).ShiftLeft(1,IYIMM,E).Label("SLL [IY+*],E");
            exBitsIY[0x34] = New().Time(23).Size(4).ShiftLeft(1,IYIMM,H).Label("SLL [IY+*],H");
            exBitsIY[0x35] = New().Time(23).Size(4).ShiftLeft(1,IYIMM,L).Label("SLL [IY+*],L");
            exBitsIY[0x36] = New().Time(23).Size(4).ShiftLeft(1,IYIMM  ).Label("SLL [IY+*]");
            exBitsIY[0x37] = New().Time(23).Size(4).ShiftLeft(1,IYIMM,A).Label("SLL [IY+*],A");

            exBitsIY[0x38] = New().Time(23).Size(4).ShiftRight(0,IYIMM,B).Label("SRL [IY+*],B");
            exBitsIY[0x39] = New().Time(23).Size(4).ShiftRight(0,IYIMM,C).Label("SRL [IY+*],C");
            exBitsIY[0x3A] = New().Time(23).Size(4).ShiftRight(0,IYIMM,D).Label("SRL [IY+*],D");
            exBitsIY[0x3B] = New().Time(23).Size(4).ShiftRight(0,IYIMM,E).Label("SRL [IY+*],E");
            exBitsIY[0x3C] = New().Time(23).Size(4).ShiftRight(0,IYIMM,H).Label("SRL [IY+*],H");
            exBitsIY[0x3D] = New().Time(23).Size(4).ShiftRight(0,IYIMM,L).Label("SRL [IY+*],L");
            exBitsIY[0x3E] = New().Time(23).Size(4).ShiftRight(0,IYIMM  ).Label("SRL [IY+*]");
            exBitsIY[0x3F] = New().Time(23).Size(4).ShiftRight(0,IYIMM,A).Label("SRL [IY+*],A");

            exBitsIY[0x40] = 
            exBitsIY[0x41] = 
            exBitsIY[0x42] = 
            exBitsIY[0x43] = 
            exBitsIY[0x44] = 
            exBitsIY[0x45] = 
            exBitsIY[0x46] = 
            exBitsIY[0x47] = New().Time(20).Size(4).TestBit(0,IYIMM).Label("BIT 0,[IY+*]");

            exBitsIY[0x48] = 
            exBitsIY[0x49] = 
            exBitsIY[0x4A] = 
            exBitsIY[0x4B] = 
            exBitsIY[0x4C] = 
            exBitsIY[0x4D] = 
            exBitsIY[0x4E] = 
            exBitsIY[0x4F] = New().Time(20).Size(4).TestBit(1,IYIMM).Label("BIT 1,[IY+*]");

            exBitsIY[0x50] = 
            exBitsIY[0x51] = 
            exBitsIY[0x52] = 
            exBitsIY[0x53] = 
            exBitsIY[0x54] = 
            exBitsIY[0x55] = 
            exBitsIY[0x56] = 
            exBitsIY[0x57] = New().Time(20).Size(4).TestBit(2,IYIMM).Label("BIT 2,[IY+*]");

            exBitsIY[0x58] = 
            exBitsIY[0x59] = 
            exBitsIY[0x5A] = 
            exBitsIY[0x5B] = 
            exBitsIY[0x5C] = 
            exBitsIY[0x5D] = 
            exBitsIY[0x5E] = 
            exBitsIY[0x5F] = New().Time(20).Size(4).TestBit(3,IYIMM).Label("BIT 3,[IY+*]");

            exBitsIY[0x60] = 
            exBitsIY[0x61] = 
            exBitsIY[0x62] = 
            exBitsIY[0x63] = 
            exBitsIY[0x64] = 
            exBitsIY[0x65] = 
            exBitsIY[0x66] = 
            exBitsIY[0x67] = New().Time(20).Size(4).TestBit(4,IYIMM).Label("BIT 4,[IY+*]");

            exBitsIY[0x68] = 
            exBitsIY[0x69] = 
            exBitsIY[0x6A] = 
            exBitsIY[0x6B] = 
            exBitsIY[0x6C] = 
            exBitsIY[0x6D] = 
            exBitsIY[0x6E] = 
            exBitsIY[0x6F] = New().Time(20).Size(4).TestBit(5,IYIMM).Label("BIT 5,[IY+*]");

            exBitsIY[0x70] = 
            exBitsIY[0x71] = 
            exBitsIY[0x72] = 
            exBitsIY[0x73] = 
            exBitsIY[0x74] = 
            exBitsIY[0x75] = 
            exBitsIY[0x76] = 
            exBitsIY[0x77] = New().Time(20).Size(4).TestBit(6,IYIMM).Label("BIT 6,[IY+*]");

            exBitsIY[0x78] = 
            exBitsIY[0x79] = 
            exBitsIY[0x7A] = 
            exBitsIY[0x7B] = 
            exBitsIY[0x7C] = 
            exBitsIY[0x7D] = 
            exBitsIY[0x7E] = 
            exBitsIY[0x7F] = New().Time(20).Size(4).TestBit(7,IYIMM).Label("BIT 7,[IY+*]");

            exBitsIY[0x80] = New().Time(23).Size(4).ResetBit(0,IYIMM,B).Label("RES 0,[IY+*],B");
            exBitsIY[0x81] = New().Time(23).Size(4).ResetBit(0,IYIMM,C).Label("RES 0,[IY+*],C");
            exBitsIY[0x82] = New().Time(23).Size(4).ResetBit(0,IYIMM,D).Label("RES 0,[IY+*],D");
            exBitsIY[0x83] = New().Time(23).Size(4).ResetBit(0,IYIMM,E).Label("RES 0,[IY+*],E");
            exBitsIY[0x84] = New().Time(23).Size(4).ResetBit(0,IYIMM,H).Label("RES 0,[IY+*],H");
            exBitsIY[0x85] = New().Time(23).Size(4).ResetBit(0,IYIMM,L).Label("RES 0,[IY+*],L");
            exBitsIY[0x86] = New().Time(23).Size(4).ResetBit(0,IYIMM  ).Label("RES 0,[IY+*]");
            exBitsIY[0x87] = New().Time(23).Size(4).ResetBit(0,IYIMM,A).Label("RES 0,[IY+*],A");

            exBitsIY[0x88] = New().Time(23).Size(4).ResetBit(1,IYIMM,B).Label("RES 1,[IY+*],B");
            exBitsIY[0x89] = New().Time(23).Size(4).ResetBit(1,IYIMM,C).Label("RES 1,[IY+*],C");
            exBitsIY[0x8A] = New().Time(23).Size(4).ResetBit(1,IYIMM,D).Label("RES 1,[IY+*],D");
            exBitsIY[0x8B] = New().Time(23).Size(4).ResetBit(1,IYIMM,E).Label("RES 1,[IY+*],E");
            exBitsIY[0x8C] = New().Time(23).Size(4).ResetBit(1,IYIMM,H).Label("RES 1,[IY+*],H");
            exBitsIY[0x8D] = New().Time(23).Size(4).ResetBit(1,IYIMM,L).Label("RES 1,[IY+*],L");
            exBitsIY[0x8E] = New().Time(23).Size(4).ResetBit(1,IYIMM  ).Label("RES 1,[IY+*]");
            exBitsIY[0x8F] = New().Time(23).Size(4).ResetBit(1,IYIMM,A).Label("RES 1,[IY+*],A");

            exBitsIY[0x90] = New().Time(23).Size(4).ResetBit(2,IYIMM,B).Label("RES 2,[IY+*],B");
            exBitsIY[0x91] = New().Time(23).Size(4).ResetBit(2,IYIMM,C).Label("RES 2,[IY+*],C");
            exBitsIY[0x92] = New().Time(23).Size(4).ResetBit(2,IYIMM,D).Label("RES 2,[IY+*],D");
            exBitsIY[0x93] = New().Time(23).Size(4).ResetBit(2,IYIMM,E).Label("RES 2,[IY+*],E");
            exBitsIY[0x94] = New().Time(23).Size(4).ResetBit(2,IYIMM,H).Label("RES 2,[IY+*],H");
            exBitsIY[0x95] = New().Time(23).Size(4).ResetBit(2,IYIMM,L).Label("RES 2,[IY+*],L");
            exBitsIY[0x96] = New().Time(23).Size(4).ResetBit(2,IYIMM  ).Label("RES 2,[IY+*]");
            exBitsIY[0x97] = New().Time(23).Size(4).ResetBit(2,IYIMM,A).Label("RES 2,[IY+*],A");

            exBitsIY[0x98] = New().Time(23).Size(4).ResetBit(3,IYIMM,B).Label("RES 3,[IY+*],B");
            exBitsIY[0x99] = New().Time(23).Size(4).ResetBit(3,IYIMM,C).Label("RES 3,[IY+*],C");
            exBitsIY[0x9A] = New().Time(23).Size(4).ResetBit(3,IYIMM,D).Label("RES 3,[IY+*],D");
            exBitsIY[0x9B] = New().Time(23).Size(4).ResetBit(3,IYIMM,E).Label("RES 3,[IY+*],E");
            exBitsIY[0x9C] = New().Time(23).Size(4).ResetBit(3,IYIMM,H).Label("RES 3,[IY+*],H");
            exBitsIY[0x9D] = New().Time(23).Size(4).ResetBit(3,IYIMM,L).Label("RES 3,[IY+*],L");
            exBitsIY[0x9E] = New().Time(23).Size(4).ResetBit(3,IYIMM  ).Label("RES 3,[IY+*]");
            exBitsIY[0x9F] = New().Time(23).Size(4).ResetBit(3,IYIMM,A).Label("RES 3,[IY+*],A");

            exBitsIY[0xA0] = New().Time(23).Size(4).ResetBit(4,IYIMM,B).Label("RES 4,[IY+*],B");
            exBitsIY[0xA1] = New().Time(23).Size(4).ResetBit(4,IYIMM,C).Label("RES 4,[IY+*],C");
            exBitsIY[0xA2] = New().Time(23).Size(4).ResetBit(4,IYIMM,D).Label("RES 4,[IY+*],D");
            exBitsIY[0xA3] = New().Time(23).Size(4).ResetBit(4,IYIMM,E).Label("RES 4,[IY+*],E");
            exBitsIY[0xA4] = New().Time(23).Size(4).ResetBit(4,IYIMM,H).Label("RES 4,[IY+*],H");
            exBitsIY[0xA5] = New().Time(23).Size(4).ResetBit(4,IYIMM,L).Label("RES 4,[IY+*],L");
            exBitsIY[0xA6] = New().Time(23).Size(4).ResetBit(4,IYIMM  ).Label("RES 4,[IY+*]");
            exBitsIY[0xA7] = New().Time(23).Size(4).ResetBit(4,IYIMM,A).Label("RES 4,[IY+*],A");

            exBitsIY[0xA8] = New().Time(23).Size(4).ResetBit(5,IYIMM,B).Label("RES 5,[IY+*],B");
            exBitsIY[0xA9] = New().Time(23).Size(4).ResetBit(5,IYIMM,C).Label("RES 5,[IY+*],C");
            exBitsIY[0xAA] = New().Time(23).Size(4).ResetBit(5,IYIMM,D).Label("RES 5,[IY+*],D");
            exBitsIY[0xAB] = New().Time(23).Size(4).ResetBit(5,IYIMM,E).Label("RES 5,[IY+*],E");
            exBitsIY[0xAC] = New().Time(23).Size(4).ResetBit(5,IYIMM,H).Label("RES 5,[IY+*],H");
            exBitsIY[0xAD] = New().Time(23).Size(4).ResetBit(5,IYIMM,L).Label("RES 5,[IY+*],L");
            exBitsIY[0xAE] = New().Time(23).Size(4).ResetBit(5,IYIMM  ).Label("RES 5,[IY+*]");
            exBitsIY[0xAF] = New().Time(23).Size(4).ResetBit(5,IYIMM,A).Label("RES 5,[IY+*],A");
            
            exBitsIY[0xB0] = New().Time(23).Size(4).ResetBit(6,IYIMM,B).Label("RES 6,[IY+*],B");
            exBitsIY[0xB1] = New().Time(23).Size(4).ResetBit(6,IYIMM,C).Label("RES 6,[IY+*],C");
            exBitsIY[0xB2] = New().Time(23).Size(4).ResetBit(6,IYIMM,D).Label("RES 6,[IY+*],D");
            exBitsIY[0xB3] = New().Time(23).Size(4).ResetBit(6,IYIMM,E).Label("RES 6,[IY+*],E");
            exBitsIY[0xB4] = New().Time(23).Size(4).ResetBit(6,IYIMM,H).Label("RES 6,[IY+*],H");
            exBitsIY[0xB5] = New().Time(23).Size(4).ResetBit(6,IYIMM,L).Label("RES 6,[IY+*],L");
            exBitsIY[0xB6] = New().Time(23).Size(4).ResetBit(6,IYIMM  ).Label("RES 6,[IY+*]");
            exBitsIY[0xB7] = New().Time(23).Size(4).ResetBit(6,IYIMM,A).Label("RES 6,[IY+*],A");

            exBitsIY[0xB8] = New().Time(23).Size(4).ResetBit(7,IYIMM,B).Label("RES 7,[IY+*],B");
            exBitsIY[0xB9] = New().Time(23).Size(4).ResetBit(7,IYIMM,C).Label("RES 7,[IY+*],C");
            exBitsIY[0xBA] = New().Time(23).Size(4).ResetBit(7,IYIMM,D).Label("RES 7,[IY+*],D");
            exBitsIY[0xBB] = New().Time(23).Size(4).ResetBit(7,IYIMM,E).Label("RES 7,[IY+*],E");
            exBitsIY[0xBC] = New().Time(23).Size(4).ResetBit(7,IYIMM,H).Label("RES 7,[IY+*],H");
            exBitsIY[0xBD] = New().Time(23).Size(4).ResetBit(7,IYIMM,L).Label("RES 7,[IY+*],L");
            exBitsIY[0xBE] = New().Time(23).Size(4).ResetBit(7,IYIMM  ).Label("RES 7,[IY+*]");
            exBitsIY[0xBF] = New().Time(23).Size(4).ResetBit(7,IYIMM,A).Label("RES 7,[IY+*],A");

            exBitsIY[0xC0] = New().Time(23).Size(4).SetBit(0,IYIMM,B).Label("SET 0,[IY+*],B");
            exBitsIY[0xC1] = New().Time(23).Size(4).SetBit(0,IYIMM,C).Label("SET 0,[IY+*],C");
            exBitsIY[0xC2] = New().Time(23).Size(4).SetBit(0,IYIMM,D).Label("SET 0,[IY+*],D");
            exBitsIY[0xC3] = New().Time(23).Size(4).SetBit(0,IYIMM,E).Label("SET 0,[IY+*],E");
            exBitsIY[0xC4] = New().Time(23).Size(4).SetBit(0,IYIMM,H).Label("SET 0,[IY+*],H");
            exBitsIY[0xC5] = New().Time(23).Size(4).SetBit(0,IYIMM,L).Label("SET 0,[IY+*],L");
            exBitsIY[0xC6] = New().Time(23).Size(4).SetBit(0,IYIMM  ).Label("SET 0,[IY+*]");
            exBitsIY[0xC7] = New().Time(23).Size(4).SetBit(0,IYIMM,A).Label("SET 0,[IY+*],A");
        
            exBitsIY[0xC8] = New().Time(23).Size(4).SetBit(1,IYIMM,B).Label("SET 1,[IY+*],B");
            exBitsIY[0xC9] = New().Time(23).Size(4).SetBit(1,IYIMM,C).Label("SET 1,[IY+*],C");
            exBitsIY[0xCA] = New().Time(23).Size(4).SetBit(1,IYIMM,D).Label("SET 1,[IY+*],D");
            exBitsIY[0xCB] = New().Time(23).Size(4).SetBit(1,IYIMM,E).Label("SET 1,[IY+*],E");
            exBitsIY[0xCC] = New().Time(23).Size(4).SetBit(1,IYIMM,H).Label("SET 1,[IY+*],H");
            exBitsIY[0xCD] = New().Time(23).Size(4).SetBit(1,IYIMM,L).Label("SET 1,[IY+*],L");
            exBitsIY[0xCE] = New().Time(23).Size(4).SetBit(1,IYIMM  ).Label("SET 1,[IY+*]");
            exBitsIY[0xCF] = New().Time(23).Size(4).SetBit(1,IYIMM,A).Label("SET 1,[IY+*],A");
                  
            exBitsIY[0xD0] = New().Time(23).Size(4).SetBit(2,IYIMM,B).Label("SET 2,[IY+*],B");
            exBitsIY[0xD1] = New().Time(23).Size(4).SetBit(2,IYIMM,C).Label("SET 2,[IY+*],C");
            exBitsIY[0xD2] = New().Time(23).Size(4).SetBit(2,IYIMM,D).Label("SET 2,[IY+*],D");
            exBitsIY[0xD3] = New().Time(23).Size(4).SetBit(2,IYIMM,E).Label("SET 2,[IY+*],E");
            exBitsIY[0xD4] = New().Time(23).Size(4).SetBit(2,IYIMM,H).Label("SET 2,[IY+*],H");
            exBitsIY[0xD5] = New().Time(23).Size(4).SetBit(2,IYIMM,L).Label("SET 2,[IY+*],L");
            exBitsIY[0xD6] = New().Time(23).Size(4).SetBit(2,IYIMM  ).Label("SET 2,[IY+*]");
            exBitsIY[0xD7] = New().Time(23).Size(4).SetBit(2,IYIMM,A).Label("SET 2,[IY+*],A");
                  
            exBitsIY[0xD8] = New().Time(23).Size(4).SetBit(3,IYIMM,B).Label("SET 3,[IY+*],B");
            exBitsIY[0xD9] = New().Time(23).Size(4).SetBit(3,IYIMM,C).Label("SET 3,[IY+*],C");
            exBitsIY[0xDA] = New().Time(23).Size(4).SetBit(3,IYIMM,D).Label("SET 3,[IY+*],D");
            exBitsIY[0xDB] = New().Time(23).Size(4).SetBit(3,IYIMM,E).Label("SET 3,[IY+*],E");
            exBitsIY[0xDC] = New().Time(23).Size(4).SetBit(3,IYIMM,H).Label("SET 3,[IY+*],H");
            exBitsIY[0xDD] = New().Time(23).Size(4).SetBit(3,IYIMM,L).Label("SET 3,[IY+*],L");
            exBitsIY[0xDE] = New().Time(23).Size(4).SetBit(3,IYIMM  ).Label("SET 3,[IY+*]");
            exBitsIY[0xDF] = New().Time(23).Size(4).SetBit(3,IYIMM,A).Label("SET 3,[IY+*],A");
                  
            exBitsIY[0xE0] = New().Time(23).Size(4).SetBit(4,IYIMM,B).Label("SET 4,[IY+*],B");
            exBitsIY[0xE1] = New().Time(23).Size(4).SetBit(4,IYIMM,C).Label("SET 4,[IY+*],C");
            exBitsIY[0xE2] = New().Time(23).Size(4).SetBit(4,IYIMM,D).Label("SET 4,[IY+*],D");
            exBitsIY[0xE3] = New().Time(23).Size(4).SetBit(4,IYIMM,E).Label("SET 4,[IY+*],E");
            exBitsIY[0xE4] = New().Time(23).Size(4).SetBit(4,IYIMM,H).Label("SET 4,[IY+*],H");
            exBitsIY[0xE5] = New().Time(23).Size(4).SetBit(4,IYIMM,L).Label("SET 4,[IY+*],L");
            exBitsIY[0xE6] = New().Time(23).Size(4).SetBit(4,IYIMM  ).Label("SET 4,[IY+*]");
            exBitsIY[0xE7] = New().Time(23).Size(4).SetBit(4,IYIMM,A).Label("SET 4,[IY+*],A");
                  
            exBitsIY[0xE8] = New().Time(23).Size(4).SetBit(5,IYIMM,B).Label("SET 5,[IY+*],B");
            exBitsIY[0xE9] = New().Time(23).Size(4).SetBit(5,IYIMM,C).Label("SET 5,[IY+*],C");
            exBitsIY[0xEA] = New().Time(23).Size(4).SetBit(5,IYIMM,D).Label("SET 5,[IY+*],D");
            exBitsIY[0xEB] = New().Time(23).Size(4).SetBit(5,IYIMM,E).Label("SET 5,[IY+*],E");
            exBitsIY[0xEC] = New().Time(23).Size(4).SetBit(5,IYIMM,H).Label("SET 5,[IY+*],H");
            exBitsIY[0xED] = New().Time(23).Size(4).SetBit(5,IYIMM,L).Label("SET 5,[IY+*],L");
            exBitsIY[0xEE] = New().Time(23).Size(4).SetBit(5,IYIMM  ).Label("SET 5,[IY+*]");
            exBitsIY[0xEF] = New().Time(23).Size(4).SetBit(5,IYIMM,A).Label("SET 5,[IY+*],A");
                  
            exBitsIY[0xF0] = New().Time(23).Size(4).SetBit(6,IYIMM,B).Label("SET 6,[IY+*],B");
            exBitsIY[0xF1] = New().Time(23).Size(4).SetBit(6,IYIMM,C).Label("SET 6,[IY+*],C");
            exBitsIY[0xF2] = New().Time(23).Size(4).SetBit(6,IYIMM,D).Label("SET 6,[IY+*],D");
            exBitsIY[0xF3] = New().Time(23).Size(4).SetBit(6,IYIMM,E).Label("SET 6,[IY+*],E");
            exBitsIY[0xF4] = New().Time(23).Size(4).SetBit(6,IYIMM,H).Label("SET 6,[IY+*],H");
            exBitsIY[0xF5] = New().Time(23).Size(4).SetBit(6,IYIMM,L).Label("SET 6,[IY+*],L");
            exBitsIY[0xF6] = New().Time(23).Size(4).SetBit(6,IYIMM  ).Label("SET 6,[IY+*]");
            exBitsIY[0xF7] = New().Time(23).Size(4).SetBit(6,IYIMM,A).Label("SET 6,[IY+*],A");
                  
            exBitsIY[0xF8] = New().Time(23).Size(4).SetBit(7,IYIMM,B).Label("SET 7,[IY+*],B");
            exBitsIY[0xF9] = New().Time(23).Size(4).SetBit(7,IYIMM,C).Label("SET 7,[IY+*],C");
            exBitsIY[0xFA] = New().Time(23).Size(4).SetBit(7,IYIMM,D).Label("SET 7,[IY+*],D");
            exBitsIY[0xFB] = New().Time(23).Size(4).SetBit(7,IYIMM,E).Label("SET 7,[IY+*],E");
            exBitsIY[0xFC] = New().Time(23).Size(4).SetBit(7,IYIMM,H).Label("SET 7,[IY+*],H");
            exBitsIY[0xFD] = New().Time(23).Size(4).SetBit(7,IYIMM,L).Label("SET 7,[IY+*],L");
            exBitsIY[0xFE] = New().Time(23).Size(4).SetBit(7,IYIMM  ).Label("SET 7,[IY+*]");
            exBitsIY[0xFF] = New().Time(23).Size(4).SetBit(7,IYIMM,A).Label("SET 7,[IY+*],A");

            return new InstructionBuilderComposite(0, PC, table);

            InstructionBuilder New()
            {
                return new InstructionBuilder(cpu, PC, F, R, cpu);
            }
        }
    }
}
