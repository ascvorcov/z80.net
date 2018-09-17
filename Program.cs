using System;
using System.Diagnostics;

namespace z80emu
{
    class App
    {
        static void Main()
        {
            Test0x01();
            Test0x02();
            Test0x03();
            Test0x04();
            Test0x05();
            Test0x06();
            Test0x07();
            Test0x08();
            Test0x09();
            Test0x0A();
            Test0x0B();
            Test0x0C();
            Test0x0D();
            Test0x0E();
            Test0x0F();
            Test0x10();
            Test0x11();
            Test0x12();
            Test0x13();
            Test0x14();
            Test0x15();
            Test0x16();
            Test0x17();
            Test0x18();
            Test0x19();
            Test0x1A();
            Test0x1B();
            Test0x1C();
            Test0x1D();
            Test0x1E();
            Test0x1F();
            Test0x20();
            Test0x21();
            Test0x22();
            Test0x23();
            Test0x24();
            Test0x25();
            Test0x26();
            Test0x27();
            Test0x28();
            Test0x29();
            Test0x2A();
            Test0x2B();
            Test0x2C();
            Test0x2D();
            Test0x2E();
            Test0x2F();
            Test0x30();
            Test0x31();
            Test0x32();
            Test0x33();
            Test0x34();
        }

        static void Test0x34() // INC [HL]
        {
            var mem = new byte[0x10000];

            // LD HL,0x5678;INC [HL]; INC [HL]; HALT
            var code = new byte[] { 0x21,0x78,0x56,0x34,0x34,0x76 }; 
            Array.Copy(code, mem, code.Length);
            var cpu = Run(mem);

            Debug.Assert(cpu.Registers.HL.Value == 0x5678);
            Debug.Assert(cpu.Flags.Value == 0);
            Debug.Assert(mem[0x5678] == 2);

            // DEC HL;LD [0x5678],HL;LD HL,0x5678;INC [HL];
            code = new byte[] { 0x2B,0x22,0x78,0x56,0x21,0x78,0x56,0x34,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.HL.Value == 0x5678);
            Debug.Assert(cpu.Flags.Value == 0x50); // half-carry,zero
            Debug.Assert(mem[0x5678] == 0);
            Debug.Assert(mem[0x5679] == 0xFF);
        }

        static void Test0x33() // INC SP
        {
            var cpu = Run(0x33,0x33,0x33,0x76); // INC SP;INC SP;INC SP;HALT
            Debug.Assert(cpu.regSP.Value == 3);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x31,0xFF,0xFF,0x33,0x76); // LD SP,0xFFFF;INC SP;HALT
            Debug.Assert(cpu.regSP.Value == 0);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x32() // LD [**],A
        {
            var mem = new byte[0x10000];
            mem[0] = 0x1A; // LD A,[DE] -> A=0x1A
            mem[1] = 0x2D; // DEC L - to affect flags
            mem[2] = 0x32; // LD [0xABCD],A
            mem[3] = 0xCD;
            mem[4] = 0xAB;
            mem[5] = 0x76; // HALT
            var cpu = Run(mem);
            Debug.Assert(cpu.regAF.A.Value == 0x1A);
            Debug.Assert(cpu.Flags.Value == 0x92);
            Debug.Assert(mem[0xABCC] == 0);
            Debug.Assert(mem[0xABCD] == 0x1A);
            Debug.Assert(mem[0xABCE] == 0);
        }

        static void Test0x31() // LD SP,**
        {
            var cpu = Run(0x31,0xCD,0xAB,0x76);
            Debug.Assert(cpu.regSP.Value == 0xABCD);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x30() // JR NC,*
        {
            //LD HL,0xFFFF;INC DE;ADD HL,DE;JR NC,1;HALT;
            
            var cpu = Run(0x21,0xFF,0xFF,0x13,0x19,0x30,0x01,0x76); 
            Debug.Assert(cpu.regPC.Value == 7);
            Debug.Assert(cpu.Registers.HL.Value == 0);

            cpu = Run(0x21,0xFE,0xFF,0x13,0x19,0x30,0x02,0x30,0x01,0x76); 
            Debug.Assert(cpu.regPC.Value == 9);
            Debug.Assert(cpu.Registers.HL.Value == 0xFFFF);
        }

        static void Test0x2F() // CPL
        {
            var cpu = Run(0x1A,0x2F,0x76); // LD A,[DE];CPL;HALT  (1A->E5)
            Debug.Assert(cpu.regAF.A.Value == 0xE5);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(cpu.Flags.AddSub);
        }

        static void Test0x2E() // LD L,*
        {
            var cpu = Run(0x2E,0x34,0x76); // LD L,0x34;HALT
            Debug.Assert(cpu.Registers.HL.Value == 0x0034);
            Debug.Assert(cpu.Flags.Value == 0);
        }
        
        static void Test0x2D() // DEC L
        {
            var cpu = Run(0x2D,0x2D,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x00FE);

            cpu = new CPU();
            cpu.Registers.HL.Value = 0xFF01;
            cpu.Run(new Memory(0x2D,0x76));
            Debug.Assert(cpu.Registers.HL.Value == 0xFF00);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);            
        }
        
        static void Test0x2C() // INC L
        {
            var cpu = Run(0x2C, 0x2C, 0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x0002);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x2E,0x0F,0x2C,0x76); // LD L,0x0F;INC L;HALT
            Debug.Assert(cpu.Registers.HL.Value == 0x0010);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);

            cpu = Run(0x2E,0x7F,0x2C,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x0080);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);
        }
        
        static void Test0x2B() // DEC HL
        {
            var cpu = Run(0x21,0x00,0x00,0x2B,0x2B,0x76); // LD HL,0;DEC HL;DEC HL;HALT
            Debug.Assert(cpu.Registers.HL.Value == 0xFFFE);
            Debug.Assert(cpu.Flags.Value == 0);
        }
        
        static void Test0x2A() // LD HL,[**]
        {
            var cpu = Run(0x00,0x2A,0x01,0x00,0x76); // NOP;LD HL,[1];HALT
            Debug.Assert(cpu.Registers.HL.Value == 0x012A);
            Debug.Assert(cpu.Flags.Value == 0);
        }
        
        static void Test0x29() // ADD HL,HL
        {
            var cpu = Run(0x21,0x34,0x12,0x29,0x29,0x76); // LD HL,0x1234;ADD HL,HL;ADD HL,HL;HALT
            Debug.Assert(cpu.Registers.HL.Value == 0x48D0);

            cpu = Run(0x2B,0x29,0x76); // DEC HL;ADD HL,HL;HALT
            Debug.Assert(cpu.Registers.HL.Value == 0xFFFE);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }
        
        static void Test0x28() // JR Z,*
        {
            //LD D,3;INC B;DEC D;JR Z,2;JR -6;HALT
            var cpu = Run(0x16,0x03,0x04,0x15,0x28,0x02,0x18,0xFA,0x76); 
            Debug.Assert(cpu.Registers.DE.Value == 0);
            Debug.Assert(cpu.Registers.BC.Value == 0x0300);
        }
        
        static void Test0x27() // DAA
        {
            var cpu = new CPU();
            cpu.regAF.A.Value = 0x3C;
            cpu.Run(new Memory(0x27,0x76));
            Debug.Assert(cpu.regAF.A.Value == 0x42);
        }
        
        static void Test0x26() // LD H,*
        {
            var cpu = Run(0x26,0xAB,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xAB00);
            Debug.Assert(cpu.Flags.Value == 0);
        }
        
        static void Test0x25() // DEC H
        {
            var cpu = Run(0x25,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xFF00);
            Debug.Assert(cpu.Flags.Value == 146); // addsub,halfcarry,sign

            cpu = Run(0x26,0x03,0x25,0x25,0x25,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0);
            Debug.Assert(cpu.Flags.Value == 66); // addsub,zero
        }
        
        static void Test0x24() // INC H
        {
            var cpu = Run(0x24,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x0100);
            Debug.Assert(cpu.Flags.Value == 0); // !addsub

            cpu = Run(0x26,0xFE,0x24,0x24,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x0000);
            Debug.Assert(cpu.Flags.Value == 80); // halfcarry,zero
        }
        
        static void Test0x23() // INC HL
        {
            var cpu = Run(0x23,0x23,0x23,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x0003);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x2E,0xFF,0x23,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x0100);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x21,0xFF,0xFF,0x23,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0);
            Debug.Assert(cpu.Flags.Value == 0);
        }
        
        static void Test0x22() // LD [**],HL
        {
            var code = new byte[0x10000];
            code[0] = 0x21; // LD HL,0xABCD
            code[1] = 0xCD; 
            code[2] = 0xAB; 
            code[3] = 0x22; // LD [0xABCD],HL
            code[4] = 0xCD;
            code[5] = 0xAB;
            code[6] = 0x76; // HALT

            var cpu = Run(code);
            Debug.Assert(cpu.Registers.HL.Value == 0xABCD);
            Debug.Assert(cpu.Flags.Value == 0);
            Debug.Assert(code[0xABCD] == 0xCD);
            Debug.Assert(code[0xABCE] == 0xAB);
        }
        
        static void Test0x21() // LD HL,**
        {
            var cpu = Run(0x21,0xFF,0xFF,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xFFFF);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x21,0xCD,0xAB,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xABCD);
            Debug.Assert(cpu.Flags.Value == 0);
        }
        
        static void Test0x20()// JR NZ,*
        {
            var cpu = Run(0x16,0x03,0x15,0x20,0xFD,0x76); // LD D,3;DEC D;JR NZ,-3;HALT
            Debug.Assert(cpu.Registers.DE.Value == 0);
            Debug.Assert(cpu.Flags.Zero);
        }

        static void Test0x1F()// RRA
        {
            var cpu = new CPU();
            cpu.regAF.Value = 0x5500; // RRA 01010101 (55) = 00101010 (2A),C=1
            cpu.Run(new Memory(0x1F,0x76));
            Debug.Assert(cpu.regAF.A.Value == 0x2A);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(cpu.Flags.Carry);

            cpu = new CPU();
            cpu.regAF.Value = 0xAA00; // RRCA 10101010 (AA) = 01010101 (55),C=0
            cpu.Run(new Memory(0x1F,0x76));
            Debug.Assert(cpu.regAF.A.Value == 0x55);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x1E()// LD E,*
        {
            var cpu = Run(0x1E,0x12,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x0012);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x1E,0x34,0x1E,0xFF,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x00FF);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0x1234;
            cpu.Run(new Memory(0x1E,0x33,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0x1233);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x1D()// DEC E
        {
            var cpu = Run(0x1D,0x1D,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x00FE);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0xFF01;
            cpu.Run(new Memory(0x1D,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0xFF00);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x1C()// INC E
        {
            var cpu = Run(0x1C, 0x1C, 0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x0002);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x1E,0x0F,0x1C,0x76); // LD E,0x0F;INC E;HALT
            Debug.Assert(cpu.Registers.DE.Value == 0x0010);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);

            cpu = Run(0x1E,0x7F,0x1C,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x0080);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);
        }

        static void Test0x1B()// DEC DE
        {
            var cpu = Run(0x11,0x00,0x80,0x1B,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x7FFF);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x1B,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xFFFF);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x1A()// LD A,[DE]
        {
            var cpu = Run(0x1A,0x76);
            Debug.Assert(cpu.regAF.Value == 0x1A00);

            var mem = new byte[0x10000];
            mem[0] = 0x11; // LD DE,0x1234
            mem[1] = 0x34;
            mem[2] = 0x12;
            mem[3] = 0x1A; // LD A,[DE]
            mem[4] = 0x76; // HALT
            mem[0x1234] = 0xFF;
            
            cpu = new CPU();
            cpu.Run(new Memory(mem));
            Debug.Assert(cpu.regAF.Value == 0xFF00);
        }

        static void Test0x19() // ADD HL,DE
        {
            var cpu = Run(0x11,0x34,0x12,0x19,0x19,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x2468);

            cpu = new CPU();
            cpu.Registers.HL.Value = 0x4242;
            cpu.Registers.DE.Value = 0x1111;
            cpu.Run(new Memory(0x19,0x76));
            Debug.Assert(cpu.Registers.HL.Value == 0x5353);
            Debug.Assert(cpu.Registers.DE.Value == 0x1111);
            Debug.Assert(cpu.regAF.Value == 0);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0x1111;
            cpu.Run(new Memory(0x19,0x19,0x19,0x19,0x19,0x76));
            Debug.Assert(cpu.Registers.HL.Value == 0x5555);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);

            cpu = new CPU();
            cpu.Registers.HL.Value = 0xFFFF;
            cpu.Registers.DE.Value = 0x0001;
            cpu.Run(new Memory(0x19,0x76));
            Debug.Assert(cpu.Registers.HL.Value == 0);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Carry);
        }

        static void Test0x18() // JR *
        {
            var cpu = Run(0x00,0x18,0x02,0x76,0x00,0x18,0xFC); //nop,jr +2,halt,nop,jr -4
            Debug.Assert(cpu.regPC.Value == 3);
            Debug.Assert(cpu.regAF.Value == 0);
        }

        static void Test0x17() // RLA
        {
            var cpu = Run(0x0A,0x17,0x17,0x17,0x17,0x76);
            Debug.Assert(cpu.regAF.Value == 0xA000);

            cpu = Run(0x0A,0x17,0x17,0x17,0x17,0x17,0x76);
            Debug.Assert(cpu.regAF.A.Value == 0x40);
            Debug.Assert(cpu.Flags.Carry);

            cpu = Run(0x0A,0x17,0x17,0x17,0x17,0x17,0x17,0x76);
            Debug.Assert(cpu.regAF.A.Value == 0x81);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x16() // LD D,*
        {
            var cpu = Run(0x16,0x12,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x1200);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x16,0x34,0x16,0xFF,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xFF00);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0x1234;
            cpu.Run(new Memory(0x16,0x33,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0x3334);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x15() // DEC D
        {
            var cpu = Run(0x15,0x15,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xFE00);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0x01FF;
            cpu.Run(new Memory(0x15,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0x00FF);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x14() // INC D
        {
            var cpu = Run(0x14,0x14,0x14,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x0300);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0xFFFF;
            cpu.Run(new Memory(0x14,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0x00FF);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Zero);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0x7F01;
            cpu.Flags.Carry = true;
            cpu.Run(new Memory(0x14,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0x8001);
            Debug.Assert(cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(cpu.Flags.Carry); // should be preserved
        }

        static void Test0x13() // INC DE
        {
            var cpu = Run(0x13,0x13,0x13,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 3);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0xFF;
            cpu.Run(new Memory(0x13,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0x100);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = new CPU();
            cpu.Registers.DE.Value = 0xFFFF;
            cpu.Run(new Memory(0x13,0x76));
            Debug.Assert(cpu.Registers.DE.Value == 0);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x12() // LD [DE],A
        {
            var mem = new byte[0x10000];
            mem[0] = 0x0A; // LD A (A=0x0A)
            mem[1] = 0x07; // RLCA (A=0x14)
            mem[2] = 0x11; // LD DE,0x9ABC
            mem[3] = 0xBC;
            mem[4] = 0x9A;
            mem[5] = 0x12; // LD [DE],A
            mem[6] = 0x76; // HALT
            var cpu = new CPU();
            cpu.Run(new Memory(mem));
            Debug.Assert(cpu.Registers.DE.Value == 0x9ABC);
            Debug.Assert(cpu.Flags.Value == 0);
            Debug.Assert(cpu.regAF.Value == 0x1400);
            Debug.Assert(mem[0x9ABC] == 0x14);
        }

        static void Test0x11() // LD DE,**
        {
            var cpu = Run(0x11,0x34,0x12,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x1234);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x10() // DJNZ *
        {
            //LD B,10;INC BC;INC BC;DJNZ -4;INC BC;HALT
            var cpu = Run(0x06,0x0A,0x03,0x03,0x10,0xFC,0x03,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x0015);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x06,0x01,0x10,0x7F,0x76);//LD B,1;DJNZ 7F;HALT
            Debug.Assert(cpu.Registers.BC.Value == 0x0000);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x0F() // RRCA
        {
            var cpu = new CPU();
            cpu.regAF.Value = 0x5500; // RRCA 01010101 (55) = 10101010 (AA),C=1
            cpu.Run(new Memory(0x0F,0x76));
            Debug.Assert(cpu.regAF.A.Value == 0xAA);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(cpu.Flags.Carry);

            cpu = new CPU();
            cpu.regAF.Value = 0xAA00; // RRCA 10101010 (AA) = 01010101 (55),C=0
            cpu.Run(new Memory(0x0F,0x76));
            Debug.Assert(cpu.regAF.A.Value == 0x55);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x0E() // LD C,*
        {
            var cpu = Run(0x0E,0x12,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x0012);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x0E,0x34,0x0E,0xFF,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x00FF);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0x1234;
            cpu.Run(new Memory(0x0E,0x33,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0x1233);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x0D() // DEC C
        {
            var cpu = Run(0x0D,0x0D,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x00FE);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0xFF01;
            cpu.Run(new Memory(0x0D,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0xFF00);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x0C()// INC C
        {
            var cpu = Run(0x0C, 0x0C, 0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x0002);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x0E,0x0F,0x0C,0x76); // LD C,0x0F;INC CB;HALT
            Debug.Assert(cpu.Registers.BC.Value == 0x0010);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);

            cpu = Run(0x0E,0x7F,0x0C,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x0080);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);
        }

        static void Test0x0B()// DEC BC
        {
            var cpu = Run(0x0B, 0x0B, 0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xFFFE);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = new CPU();
            var mem = new Memory(0x0B, 0x76);
            cpu.Registers.BC.Value = 0x0001;
            cpu.Run(mem);
            Debug.Assert(cpu.Registers.BC.Value == 0);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x0A()// LD A,[BC]
        {
            var cpu = Run(0x0A, 0x76);
            Debug.Assert(cpu.regAF.A.Value == 0x0A);

            var mem = new byte[0x10000];
            mem[0] = 0x0A; // LD A,[BC]
            mem[1] = 0x07; // RLCA
            mem[2] = 0x03; // INC BC
            mem[3] = 0x02; // LD [BC],A
            mem[4] = 0x76; // HALT
            mem[0x5678] = 0x12;
            cpu = new CPU();
            cpu.Registers.BC.Value = 0x5678;
            cpu.Run(new Memory(mem));
            Debug.Assert(cpu.regAF.Value == 0x2400);
            Debug.Assert(mem[0x5678] == 0x12);
            Debug.Assert(mem[0x5679] == 0x24);
        }
        
        static void Test0x09()// ADD HL,BC
        {
            var cpu = Run(0x09, 0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0);
            Debug.Assert(cpu.regAF.Value == 0);

            cpu = new CPU();
            cpu.Registers.HL.Value = 0x4242;
            cpu.Registers.BC.Value = 0x1111;
            cpu.Run(new Memory(0x09,0x76));
            Debug.Assert(cpu.Registers.HL.Value == 0x5353);
            Debug.Assert(cpu.Registers.BC.Value == 0x1111);
            Debug.Assert(cpu.regAF.Value == 0);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0x1111;
            cpu.Run(new Memory(0x09,0x09,0x09,0x09,0x09,0x76));
            Debug.Assert(cpu.Registers.HL.Value == 0x5555);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);

            cpu = new CPU();
            cpu.Registers.HL.Value = 0xFFFF;
            cpu.Registers.BC.Value = 0x0001;
            cpu.Run(new Memory(0x09,0x76));
            Debug.Assert(cpu.Registers.HL.Value == 0);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Carry);
        }

        static void Test0x08() // EX AF,AF'
        {
            var cpu = new CPU();
            cpu.regAF.Value = 0x1234;
            cpu.regAFx.Value = 0x5678;
            cpu.Run(new Memory(0x08,0x76));
            Debug.Assert(cpu.regAF.Value == 0x5678);
            Debug.Assert(cpu.regAFx.Value == 0x1234);

            cpu = new CPU();
            cpu.regAF.Value = 0x1234;
            cpu.regAFx.Value = 0x5678;
            cpu.Run(new Memory(0x08,0x08,0x08,0x08,0x76));
            Debug.Assert(cpu.regAF.Value == 0x1234);
            Debug.Assert(cpu.regAFx.Value == 0x5678);
        }

        static void Test0x07() // RLCA
        {
            var cpu = Run(0x07,0x76);
            Debug.Assert(cpu.regAF.Value == 0);
            
            cpu = new CPU();
            cpu.regAF.Value = 0x8100; // 10000001 << 1 = 00000011,C=1
            cpu.Run(new Memory(0x07,0x76));
            Debug.Assert(cpu.regAF.A.Value == 3);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(cpu.Flags.Carry);

            cpu = new CPU();
            cpu.regAF.Value = 0x5500; // 01010101 << 1 = 10101010,C=0
            cpu.Run(new Memory(0x07,0x76));
            Debug.Assert(cpu.regAF.A.Value == 0xAA);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x06() // LD B,*
        {
            var cpu = Run(0x06,0x12,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x1200);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = Run(0x06,0x34,0x06,0xFF,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xFF00);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0x1234;
            cpu.Run(new Memory(0x06,0x33,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0x3334);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x05() // DEC B
        {
            var cpu = Run(0x05,0x05,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xFE00);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0x01FF;
            cpu.Run(new Memory(0x05,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0x00FF);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Zero);
            Debug.Assert(!cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);
        }

        static void Test0x04() // INC B
        {
            var cpu = Run(0x04,0x04,0x04,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x0300);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0xFFFF;
            cpu.Run(new Memory(0x04,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0x00FF);
            Debug.Assert(!cpu.Flags.Sign);
            Debug.Assert(cpu.Flags.Zero);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(!cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(!cpu.Flags.Carry);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0x7F01;
            cpu.Flags.Carry = true;
            cpu.Run(new Memory(0x04,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0x8001);
            Debug.Assert(cpu.Flags.Sign);
            Debug.Assert(!cpu.Flags.Zero);
            Debug.Assert(cpu.Flags.HalfCarry);
            Debug.Assert(cpu.Flags.ParityOverflow);
            Debug.Assert(!cpu.Flags.AddSub);
            Debug.Assert(cpu.Flags.Carry); // should be preserved
        }

        static void Test0x03() // INC BC
        {
            var cpu = Run(0x03,0x03,0x03,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 3);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0xFF;
            cpu.Run(new Memory(0x03,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0x100);
            Debug.Assert(cpu.Flags.Value == 0);

            cpu = new CPU();
            cpu.Registers.BC.Value = 0xFFFF;
            cpu.Run(new Memory(0x03,0x76));
            Debug.Assert(cpu.Registers.BC.Value == 0);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x02() // LD [BC],A
        {
            var cpu = new CPU();
            var data = new byte[0x10000];
            data[0] = 0x02; // ld [bc],a
            data[1] = 0x76; // halt
            data[0x5678] = 0xFF;
            cpu.Registers.BC.Value = 0x5678;
            cpu.regAF.A.Value = 0x12;
            var mem = new Memory(data);
            cpu.Run(mem);
            Debug.Assert(data[0x5678] == 0x12);
        }

        static void Test0x01() // LD BC,**
        {
            CPU cpu;
            cpu = Run(0x01,0x00,0x00,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0);

            cpu = Run(0x01,0xFF,0x00,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x00FF);

            cpu = Run(0x01,0x00,0xFF,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xFF00);

            cpu = Run(0x01,0xFF,0xFF,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xFFFF);

            cpu = Run(0x01,0x34,0x12,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x1234);
        }

        static CPU Run(params byte[] program)
        {
            var cpu = new CPU();
            cpu.Run(new Memory(program));
            return cpu;
        }
    }
}