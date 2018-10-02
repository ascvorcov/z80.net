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
            Test0x35();
            Test0x36();
            Test0x37();
            Test0x38();
            Test0x39();
            Test0x3A();
            Test0x3B();
            Test0x3C();
            Test0x3D();
            Test0x3E();
            Test0x3F();
            Test0x40_0x47();
            Test0x48_0x4F();
            Test0x50_0x57();
            Test0x58_0x5F();
            Test0x60_0x67();
            Test0x68_0x6F();
            Test0x70_0x77();
            Test0x78();
            Test0x79();
            Test0x7A();
            Test0x7B();
            Test0x7C();
            Test0x7D();
            Test0x7E();
            Test0x7F();
            Test0x80_0x85();
            Test0x86();
            Test0x87();
            Test0x88();
            Test0x88_0x8D();
            Test0x8E();
            Test0x8F();
            Test0x90_0x95();
            Test0x96();
            Test0x97();
            Test0x98_0x9D();
            Test0x9E();
            Test0x9F();
        }

        static void Test0x9F() // SBC A,A
        {
            byte[] testValues = { 0,1,0x7F,0x80,0xFE,0xFF};
            foreach (var test in testValues)
            {
                var cpu = Run(0x3E, test, 0x37, 0x9F, 0x76);
                Debug.Assert(cpu.regAF.A.Value == 0xFF);
                Debug.Assert(cpu.Flags.Value == (byte)(F.Carry|F.AddSub|F.Sign|F.HalfCarry));

                cpu = Run(0x3E, test, 0x9F, 0x76);
                Debug.Assert(cpu.regAF.A.Value == 0);
                Debug.Assert(cpu.Flags.Value == (byte)(F.Zero|F.AddSub));
            }
        }

        static void Test0x9E() // SBC A,[HL]
        {
            TestSbc((a,b) =>
            {
                var mem = new byte[0x10000];
                mem[0xABCD] = b;
                var code = new byte[] { 0x21, 0xCD, 0xAB, 0x3E, a, 0x37, 0x9E, 0x76 };
                Array.Copy(code, mem, code.Length);
                var cpu = Run(mem);
                return (cpu.regAF.A.Value, (F)cpu.Flags.Value);
            });
            TestSub((a,b) =>
            {
                var mem = new byte[0x10000];
                mem[0xABCD] = b;
                var code = new byte[] { 0x21, 0xCD, 0xAB, 0x3E, a, 0x9E, 0x76 };
                Array.Copy(code, mem, code.Length);
                var cpu = Run(mem);
                return (cpu.regAF.A.Value, (F)cpu.Flags.Value);
            });
        }

        static void Test0x98_0x9D() // SBC BCDEHL
        {
            var opcodes = new (byte,byte)[]
            {
                (0x98, 0x06),
                (0x99, 0x0E),
                (0x9A, 0x16),
                (0x9B, 0x1E),
                (0x9C, 0x26),
                (0x9D, 0x2E)
            };

            foreach (var pair in opcodes)
            {
                TestSbc((a,b) =>
                {
                    var cpu = Run(0x3E, a, pair.Item2, b, 0x37, pair.Item1, 0x76);
                    return (cpu.regAF.A.Value, (F)cpu.Flags.Value);
                });
                TestSub((a,b) => // use SUB as SBC with 0 carry
                {
                    var cpu = Run(0x3E, a, pair.Item2, b, pair.Item1, 0x76);
                    return (cpu.regAF.A.Value, (F)cpu.Flags.Value);
                });
            }
        }

        static void Test0x97() // SUB A
        {
            byte[] testValues = { 0,1,0x7F,0x80,0xFE,0xFF};
            foreach (var test in testValues)
            {
                var cpu = Run(0x3E, test, 0x97, 0x76);
                Debug.Assert(cpu.regAF.A.Value == 0);
                Debug.Assert(cpu.Flags.Value == (byte)(F.Zero|F.AddSub));
            }
        }

        static void Test0x96() // SUB [HL]
        {
            TestSub((a,b) =>
            {
                // LD HL,0xABCD;LD A,a;LD [HL],b;SUB A,[HL];HALT
                var mem = new byte[0x10000];
                mem[0xABCD] = b;
                var code = new byte[] { 0x21, 0xCD, 0xAB, 0x3E, a, 0x96, 0x76 };
                Array.Copy(code, mem, code.Length);
                var cpu = Run(mem);
                return (cpu.regAF.A.Value, (F)cpu.Flags.Value);
            });
        }

        static void Test0x90_0x95() // SUB BCDEHL
        {
            var opcodes = new (byte,byte)[]
            {
                (0x90, 0x06),
                (0x91, 0x0E),
                (0x92, 0x16),
                (0x93, 0x1E),
                (0x94, 0x26),
                (0x95, 0x2E)
            };

            foreach (var pair in opcodes)
            {
                TestSub((a,b) =>
                {
                    var cpu = Run(0x3E, a, pair.Item2, b, pair.Item1, 0x76);
                    return (cpu.regAF.A.Value, (F)cpu.Flags.Value);
                });
            }
        }

        static void TestSbc(Func<byte,byte,(int,F)> Sbc)
        {
            Debug.Assert(Sbc(  0,  0) == (255, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(  0,  1) == (254, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(  0,127) == (128, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(  0,128) == (127, F.Carry | F.AddSub | F.HalfCarry));
            Debug.Assert(Sbc(  0,129) == (126, F.Carry | F.AddSub | F.HalfCarry));
            Debug.Assert(Sbc(  0,255) == (  0, F.Carry | F.AddSub | F.HalfCarry | F.Zero));
            Debug.Assert(Sbc(  1,  0) == (  0, F.Zero  | F.AddSub));
            Debug.Assert(Sbc(  1,  1) == (255, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(  1,127) == (129, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(  1,128) == (128, F.Carry | F.AddSub | F.ParityOverflow | F.Sign));
            Debug.Assert(Sbc(  1,129) == (127, F.Carry | F.AddSub | F.HalfCarry));
            Debug.Assert(Sbc(  1,255) == (  1, F.Carry | F.AddSub | F.HalfCarry));
            Debug.Assert(Sbc(127,  0) == (126, F.AddSub));
            Debug.Assert(Sbc(127,  1) == (125, F.AddSub));
            Debug.Assert(Sbc(127,127) == (255, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(127,128) == (254, F.Carry | F.AddSub | F.ParityOverflow | F.Sign));
            Debug.Assert(Sbc(127,129) == (253, F.Carry | F.AddSub | F.ParityOverflow | F.Sign));
            Debug.Assert(Sbc(127,255) == (127, F.Carry | F.AddSub | F.HalfCarry));
            Debug.Assert(Sbc(128,  0) == (127, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sbc(128,  1) == (126, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sbc(128,127) == (  0, F.AddSub | F.ParityOverflow | F.HalfCarry | F.Zero));
            Debug.Assert(Sbc(128,128) == (255, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(128,129) == (254, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(128,255) == (128, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(129,  0) == (128, F.AddSub | F.Sign));
            Debug.Assert(Sbc(129,  1) == (127, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sbc(129,127) == (  1, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sbc(129,128) == (  0, F.AddSub | F.Zero));
            Debug.Assert(Sbc(129,129) == (255, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(129,255) == (129, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
            Debug.Assert(Sbc(255,  0) == (254, F.AddSub | F.Sign));
            Debug.Assert(Sbc(255,  1) == (253, F.AddSub | F.Sign));
            Debug.Assert(Sbc(255,127) == (127, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sbc(255,128) == (126, F.AddSub));
            Debug.Assert(Sbc(255,129) == (125, F.AddSub));
            Debug.Assert(Sbc(255,255) == (255, F.Carry | F.AddSub | F.HalfCarry | F.Sign));
        }

        static void TestSub(Func<byte,byte,(int,F)> Sub)
        {
            Debug.Assert(Sub(  0,   0)==(  0, F.AddSub | F.Zero));
            Debug.Assert(Sub(  0,   1)==(255, F.AddSub | F.Carry | F.HalfCarry | F.Sign));
            Debug.Assert(Sub(  0, 127)==(129, F.AddSub | F.Carry | F.HalfCarry | F.Sign));
            Debug.Assert(Sub(  0, 128)==(128, F.AddSub | F.Carry | F.ParityOverflow | F.Sign));
            Debug.Assert(Sub(  0, 129)==(127, F.AddSub | F.Carry | F.HalfCarry));
            Debug.Assert(Sub(  0, 255)==(  1, F.AddSub | F.Carry | F.HalfCarry));
            Debug.Assert(Sub(  1,   0)==(  1, F.AddSub ));
            Debug.Assert(Sub(  1,   1)==(  0, F.AddSub | F.Zero ));
            Debug.Assert(Sub(  1, 127)==(130, F.AddSub | F.Carry | F.HalfCarry | F.Sign));
            Debug.Assert(Sub(  1, 128)==(129, F.AddSub | F.Carry | F.ParityOverflow | F.Sign));
            Debug.Assert(Sub(  1, 129)==(128, F.AddSub | F.Carry | F.ParityOverflow | F.Sign));
            Debug.Assert(Sub(  1, 255)==(  2, F.AddSub | F.Carry | F.HalfCarry));
            Debug.Assert(Sub(127,   0)==(127, F.AddSub ));
            Debug.Assert(Sub(127,   1)==(126, F.AddSub ));
            Debug.Assert(Sub(127, 127)==(  0, F.AddSub | F.Zero ));
            Debug.Assert(Sub(127, 128)==(255, F.AddSub | F.Carry | F.ParityOverflow | F.Sign));
            Debug.Assert(Sub(127, 129)==(254, F.AddSub | F.Carry | F.ParityOverflow | F.Sign));
            Debug.Assert(Sub(127, 255)==(128, F.AddSub | F.Carry | F.ParityOverflow | F.Sign));
            Debug.Assert(Sub(128,   0)==(128, F.AddSub | F.Sign));
            Debug.Assert(Sub(128,   1)==(127, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sub(128, 127)==(  1, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sub(128, 128)==(  0, F.AddSub | F.Zero ));
            Debug.Assert(Sub(128, 129)==(255, F.AddSub | F.Carry | F.Sign | F.HalfCarry));
            Debug.Assert(Sub(128, 255)==(129, F.AddSub | F.Carry | F.Sign | F.HalfCarry));
            Debug.Assert(Sub(129,   0)==(129, F.AddSub | F.Sign ));
            Debug.Assert(Sub(129,   1)==(128, F.AddSub | F.Sign ));
            Debug.Assert(Sub(129, 127)==(  2, F.AddSub | F.ParityOverflow | F.HalfCarry));
            Debug.Assert(Sub(129, 128)==(  1, F.AddSub ));
            Debug.Assert(Sub(129, 129)==(  0, F.AddSub | F.Zero ));
            Debug.Assert(Sub(129, 255)==(130, F.AddSub | F.Carry | F.Sign | F.HalfCarry));
            Debug.Assert(Sub(255,   0)==(255, F.AddSub | F.Sign ));
            Debug.Assert(Sub(255,   1)==(254, F.AddSub | F.Sign ));
            Debug.Assert(Sub(255, 127)==(128, F.AddSub | F.Sign ));
            Debug.Assert(Sub(255, 128)==(127, F.AddSub ));
            Debug.Assert(Sub(255, 129)==(126, F.AddSub ));
            Debug.Assert(Sub(255, 255)==(  0, F.AddSub | F.Zero ));
        }

        static void Test0x8F() // ADC A,A
        {
            var cpu = Adc(0,0);
            Debug.Assert(cpu.Flags.Value == (byte)F.Zero);
            Debug.Assert(cpu.regAF.A.Value == 0);
            cpu = Adc(0,1);
            Debug.Assert(cpu.Flags.Value == 0);
            Debug.Assert(cpu.regAF.A.Value == 1);

            cpu = Adc(0x4F,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Sign|F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x9E);
            cpu = Adc(0x4F,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Sign|F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x9F);

            cpu = Adc(0x1F,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x3E);
            cpu = Adc(0x1F,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x3F);

            cpu = Adc(0x80,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry|F.Zero));
            Debug.Assert(cpu.regAF.A.Value == 0);
            cpu = Adc(0x80,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 1);

            cpu = Adc(0x81,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 2);
            cpu = Adc(0x81,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 3);

            CPU Adc(byte v, byte c)
            {
                return c == 0 ? Run(0x3E, v, 0x8F,0x76) : Run(0x3E, v, 0x37, 0x8F, 0x76);
            }
        }

        static void Test0x8E() // ADC A,[HL]
        {
            var cpu = Adc(0,0,0);
            Debug.Assert(cpu.Flags.Value == (byte)F.Zero);
            Debug.Assert(cpu.regAF.A.Value == 0);
            
            cpu = Adc(0,0,1);
            Debug.Assert(cpu.Flags.Value == 0);
            Debug.Assert(cpu.regAF.A.Value == 1);

            cpu = Adc(0x7F,1,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.Sign|F.HalfCarry|F.ParityOverflow));
            Debug.Assert(cpu.regAF.A.Value == 0x80);
            cpu = Adc(0x7F,1,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.Sign|F.HalfCarry|F.ParityOverflow));
            Debug.Assert(cpu.regAF.A.Value == 0x81);

            cpu = Adc(0x0F,0x02,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x11);
            cpu = Adc(0x0F,0x02,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x12);

            cpu = Adc(0x80,0x80,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry|F.Zero));
            Debug.Assert(cpu.regAF.A.Value == 0);
            cpu = Adc(0x80,0x80,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 1);

            cpu = Adc(0x80,0x81,0);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 1);
            cpu = Adc(0x80,0x81,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 2);

            cpu = Adc(0xFF,0xFF,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry|F.Carry|F.Sign));
            Debug.Assert(cpu.regAF.A.Value == 0xFF);

            CPU Adc(byte b1, byte b2, byte carry)
            {
                // LD HL,0xABCD;LD A,b1;LD [HL],b2;<SCF>;ADD A,[HL];HALT
                var mem = new byte[0x10000];
                byte scf = carry == 1 ? (byte)0x37 : (byte)0;
                var code = new byte[] { 0x21, 0xCD, 0xAB, 0x3E, b1, 0x36, b2, scf, 0x8E, 0x76 };
                Array.Copy(code, mem, code.Length);
                return Run(mem);
           }
        }

        static void Test0x88()
        {
            Debug.Assert(Adc(   0 ,   0 , 0 )==(  0 ,0 ,0));//r,cy,ov
            Debug.Assert(Adc(   0 ,   1 , 0 )==(  1 ,0 ,0));
            Debug.Assert(Adc(   0 , 127 , 0 )==(127 ,0 ,0));
            Debug.Assert(Adc(   0 , 128 , 0 )==(128 ,0 ,0));
            Debug.Assert(Adc(   0 , 129 , 0 )==(129 ,0 ,0));
            Debug.Assert(Adc(   0 , 255 , 0 )==(255 ,0 ,0));
            Debug.Assert(Adc(   1 ,   0 , 0 )==(  1 ,0 ,0));
            Debug.Assert(Adc(   1 ,   1 , 0 )==(  2 ,0 ,0));
            Debug.Assert(Adc(   1 , 127 , 0 )==(128 ,0 ,1));
            Debug.Assert(Adc(   1 , 128 , 0 )==(129 ,0 ,0));
            Debug.Assert(Adc(   1 , 129 , 0 )==(130 ,0 ,0));
            Debug.Assert(Adc(   1 , 255 , 0 )==(  0 ,1 ,0));
            Debug.Assert(Adc( 127 ,   0 , 0 )==(127 ,0 ,0));
            Debug.Assert(Adc( 127 ,   1 , 0 )==(128 ,0 ,1));
            Debug.Assert(Adc( 127 , 127 , 0 )==(254 ,0 ,1));
            Debug.Assert(Adc( 127 , 128 , 0 )==(255 ,0 ,0));
            Debug.Assert(Adc( 127 , 129 , 0 )==(  0 ,1 ,0));
            Debug.Assert(Adc( 127 , 255 , 0 )==(126 ,1 ,0));
            Debug.Assert(Adc( 128 ,   0 , 0 )==(128 ,0 ,0));
            Debug.Assert(Adc( 128 ,   1 , 0 )==(129 ,0 ,0));
            Debug.Assert(Adc( 128 , 127 , 0 )==(255 ,0 ,0));
            Debug.Assert(Adc( 128 , 128 , 0 )==(  0 ,1 ,1));
            Debug.Assert(Adc( 128 , 129 , 0 )==(  1 ,1 ,1));
            Debug.Assert(Adc( 128 , 255 , 0 )==(127 ,1 ,1));
            Debug.Assert(Adc( 129 ,   0 , 0 )==(129 ,0 ,0));
            Debug.Assert(Adc( 129 ,   1 , 0 )==(130 ,0 ,0));
            Debug.Assert(Adc( 129 , 127 , 0 )==(  0 ,1 ,0));
            Debug.Assert(Adc( 129 , 128 , 0 )==(  1 ,1 ,1));
            Debug.Assert(Adc( 129 , 129 , 0 )==(  2 ,1 ,1));
            Debug.Assert(Adc( 129 , 255 , 0 )==(128 ,1 ,0));
            Debug.Assert(Adc( 255 ,   0 , 0 )==(255 ,0 ,0));
            Debug.Assert(Adc( 255 ,   1 , 0 )==(  0 ,1 ,0));
            Debug.Assert(Adc( 255 , 127 , 0 )==(126 ,1 ,0));
            Debug.Assert(Adc( 255 , 128 , 0 )==(127 ,1 ,1));
            Debug.Assert(Adc( 255 , 129 , 0 )==(128 ,1 ,0));
            Debug.Assert(Adc( 255 , 255 , 0 )==(254 ,1 ,0));
            Debug.Assert(Adc(   0 ,   0 , 1 )==(  1 ,0 ,0));
            Debug.Assert(Adc(   0 ,   1 , 1 )==(  2 ,0 ,0));
            Debug.Assert(Adc(   0 , 127 , 1 )==(128 ,0 ,1));
            Debug.Assert(Adc(   0 , 128 , 1 )==(129 ,0 ,0));
            Debug.Assert(Adc(   0 , 129 , 1 )==(130 ,0 ,0));
            Debug.Assert(Adc(   0 , 255 , 1 )==(  0 ,1 ,0));
            Debug.Assert(Adc(   1 ,   0 , 1 )==(  2 ,0 ,0));
            Debug.Assert(Adc(   1 ,   1 , 1 )==(  3 ,0 ,0));
            Debug.Assert(Adc(   1 , 127 , 1 )==(129 ,0 ,1));
            Debug.Assert(Adc(   1 , 128 , 1 )==(130 ,0 ,0));
            Debug.Assert(Adc(   1 , 129 , 1 )==(131 ,0 ,0));
            Debug.Assert(Adc(   1 , 255 , 1 )==(  1 ,1 ,0));
            Debug.Assert(Adc( 127 ,   0 , 1 )==(128 ,0 ,1));
            Debug.Assert(Adc( 127 ,   1 , 1 )==(129 ,0 ,1));
            Debug.Assert(Adc( 127 , 127 , 1 )==(255 ,0 ,1));
            Debug.Assert(Adc( 127 , 128 , 1 )==(  0 ,1 ,0));
            Debug.Assert(Adc( 127 , 129 , 1 )==(  1 ,1 ,0));
            Debug.Assert(Adc( 127 , 255 , 1 )==(127 ,1 ,0));
            Debug.Assert(Adc( 128 ,   0 , 1 )==(129 ,0 ,0));
            Debug.Assert(Adc( 128 ,   1 , 1 )==(130 ,0 ,0));
            Debug.Assert(Adc( 128 , 127 , 1 )==(  0 ,1 ,0));
            Debug.Assert(Adc( 128 , 128 , 1 )==(  1 ,1 ,1));
            Debug.Assert(Adc( 128 , 129 , 1 )==(  2 ,1 ,1));
            Debug.Assert(Adc( 128 , 255 , 1 )==(128 ,1 ,0));
            Debug.Assert(Adc( 129 ,   0 , 1 )==(130 ,0 ,0));
            Debug.Assert(Adc( 129 ,   1 , 1 )==(131 ,0 ,0));
            Debug.Assert(Adc( 129 , 127 , 1 )==(  1 ,1 ,0));
            Debug.Assert(Adc( 129 , 128 , 1 )==(  2 ,1 ,1));
            Debug.Assert(Adc( 129 , 129 , 1 )==(  3 ,1 ,1));
            Debug.Assert(Adc( 129 , 255 , 1 )==(129 ,1 ,0));
            Debug.Assert(Adc( 255 ,   0 , 1 )==(  0 ,1 ,0));
            Debug.Assert(Adc( 255 ,   1 , 1 )==(  1 ,1 ,0));
            Debug.Assert(Adc( 255 , 127 , 1 )==(127 ,1 ,0));
            Debug.Assert(Adc( 255 , 128 , 1 )==(128 ,1 ,0));
            Debug.Assert(Adc( 255 , 129 , 1 )==(129 ,1 ,0));
            Debug.Assert(Adc( 255 , 255 , 1 )==(255 ,1 ,0));

            (int,int,int) Adc(byte b1,byte b2,byte b3)
            {
                // LD A,b1;LD B,b2;ADC A,B;HALT
                byte scf = b3 == 1 ? (byte)0x37 : (byte)0;
                var cpu = Run(0x3E, b1, 0x06, b2, scf, 0x88, 0x76);
                return (cpu.regAF.A.Value, cpu.Flags.Carry ? 1 : 0, cpu.Flags.ParityOverflow ? 1 : 0);
            }
        }

        static void Test0x88_0x8D() // ADC A,(BCDEHL)
        {
            var codes = new (byte,byte)[] 
            {
                (0x88,0x06),
                (0x89,0x0E),
                (0x8A,0x16),
                (0x8B,0x1E),
                (0x8C,0x26),
                (0x8D,0x2E),
            };
            foreach (var op in codes)
            {
                var cpu = Adc(0,0,0);
                Debug.Assert(cpu.Flags.Value == (byte)F.Zero);
                Debug.Assert(cpu.regAF.A.Value == 0);

                cpu = Adc(0,0,1);
                Debug.Assert(cpu.Flags.Value == 0);
                Debug.Assert(cpu.regAF.A.Value == 1);

                cpu = Adc(0x7F,1,0);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Sign|F.HalfCarry));
                Debug.Assert(cpu.regAF.A.Value == 0x80);

                cpu = Adc(0x7F,1,1);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Sign|F.HalfCarry));
                Debug.Assert(cpu.regAF.A.Value == 0x81);

                cpu = Adc(0x0F,0x02,0);
                Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
                Debug.Assert(cpu.regAF.A.Value == 0x11);

                cpu = Adc(0x0F,0x02,1);
                Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
                Debug.Assert(cpu.regAF.A.Value == 0x12);

                cpu = Adc(0x80,0x80,0);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry|F.Zero));
                Debug.Assert(cpu.regAF.A.Value == 0);

                cpu = Adc(0x80,0x80,1);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
                Debug.Assert(cpu.regAF.A.Value == 1);

                cpu = Adc(0x80,0x81,0);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
                Debug.Assert(cpu.regAF.A.Value == 1);

                cpu = Adc(0x80,0x81,1);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
                Debug.Assert(cpu.regAF.A.Value == 2);

                cpu = Adc(0xFF,0xFF,1);
                Debug.Assert(cpu.Flags.Value == (byte)(F.Carry|F.HalfCarry|F.Sign));
                Debug.Assert(cpu.regAF.A.Value == 0xFF);

                CPU Adc(byte b1, byte b2, byte carry)
                {
                    // LD A,b1;LD B,b2;op;HALT
                    byte scf = carry == 1 ? (byte)0x37 : (byte)0;
                    return Run(0x3E, b1, op.Item2, b2, scf, op.Item1, 0x76);
                }
            }
        }

        static void Test0x87() // ADD A,A
        {
            var cpu = Add(0);
            Debug.Assert(cpu.Flags.Value == (byte)F.Zero);
            Debug.Assert(cpu.regAF.A.Value == 0);

            cpu = Add(0x4F);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Sign|F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x9E);

            cpu = Add(0x1F);
            Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x3E);

            cpu = Add(0x80);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry|F.Zero));
            Debug.Assert(cpu.regAF.A.Value == 0);

            cpu = Add(0x81);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 2);

            CPU Add(byte b1)
            {
                // LD A,b1;ADD A,A;HALT
                return Run(0x3E,b1,0x87,0x76);
           }
        }

        static void Test0x86() // ADD A,[HL]
        {
            var cpu = Add(0,0);
            Debug.Assert(cpu.Flags.Value == (byte)F.Zero);
            Debug.Assert(cpu.regAF.A.Value == 0);

            cpu = Add(0x7F,1);
            Debug.Assert(cpu.Flags.Value == (byte)(F.Sign|F.HalfCarry|F.ParityOverflow));
            Debug.Assert(cpu.regAF.A.Value == 0x80);

            cpu = Add(0x0F,0x02);
            Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
            Debug.Assert(cpu.regAF.A.Value == 0x11);

            cpu = Add(0x80,0x80);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry|F.Zero));
            Debug.Assert(cpu.regAF.A.Value == 0);

            cpu = Add(0x80,0x81);
            Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
            Debug.Assert(cpu.regAF.A.Value == 1);

            CPU Add(byte b1, byte b2)
            {
                // LD HL,0xABCD;LD A,b1;LD [HL],b2;ADD A,;HALT
                var mem = new byte[0x10000];
                var code = new byte[] { 0x21, 0xCD, 0xAB, 0x3E, b1, 0x36, b2, 0x86, 0x76 };
                Array.Copy(code, mem, code.Length);
                return Run(mem);
           }
        }

        static void Test0x80_0x85() // ADD A,(BCDEHL)
        {
            var codes = new (byte,byte)[] 
            {
                (0x80,0x06),
                (0x81,0x0E),
                (0x82,0x16),
                (0x83,0x1E),
                (0x84,0x26),
                (0x85,0x2E),
            };
            foreach (var op in codes)
            {
                var cpu = Add(0,0);
                Debug.Assert(cpu.Flags.Value == (byte)F.Zero);
                Debug.Assert(cpu.regAF.A.Value == 0);

                cpu = Add(0x7F,1);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Sign|F.HalfCarry));
                Debug.Assert(cpu.regAF.A.Value == 0x80);

                cpu = Add(0x0F,0x02);
                Debug.Assert(cpu.Flags.Value == (byte)(F.HalfCarry));
                Debug.Assert(cpu.regAF.A.Value == 0x11);

                cpu = Add(0x80,0x80);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry|F.Zero));
                Debug.Assert(cpu.regAF.A.Value == 0);

                cpu = Add(0x80,0x81);
                Debug.Assert(cpu.Flags.Value == (byte)(F.ParityOverflow|F.Carry));
                Debug.Assert(cpu.regAF.A.Value == 1);

                CPU Add(byte b1, byte b2)
                {
                    // LD A,b1;LD B,b2;op;HALT
                    return Run(0x3E, b1, op.Item2, b2, op.Item1, 0x76);
                }
            }
        }

        static void Test0x7E() // LD A,[HL]
        {
            var mem = new byte[0x10000];
            mem[0xABCD] = 0x7F;
            var code = new byte[] { 0x26,0xAB,0x2E,0xCD,0x7E,0x76 };
            Array.Copy(code, mem, code.Length);

            var cpu = Run(mem);
            Debug.Assert(cpu.regAF.A.Value == 0x7F);
        }

        static void Test0x7F() // LD A,A
        {
            var cpu = Run(0x2E,0x33,0x7D,0x7F,0x76);
            Debug.Assert(cpu.regAF.Value == 0x3300);
        }

        static void Test0x7D() // LD A,L
        {
            var cpu = Run(0x2E,0x7F,0x7D,0x76);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
            Debug.Assert(cpu.Registers.HL.Low.Value == 0x7F);
        }

        static void Test0x7C() // LD A,H
        {
            var cpu = Run(0x26,0x7F,0x7C,0x76);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
            Debug.Assert(cpu.Registers.HL.High.Value == 0x7F);
        }

        static void Test0x7B() // LD A,E
        {
            var cpu = Run(0x1E,0x7F,0x7B,0x76);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
            Debug.Assert(cpu.Registers.DE.Low.Value == 0x7F);
        }

        static void Test0x7A() // LD A,D
        {
            var cpu = Run(0x16,0x7F,0x7A,0x76);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
            Debug.Assert(cpu.Registers.DE.High.Value == 0x7F);
        }

        static void Test0x79() // LD A,C
        {
            var cpu = Run(0x0E,0x7F,0x79,0x76);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
            Debug.Assert(cpu.Registers.BC.Low.Value == 0x7F);
        }

        static void Test0x78() // LD A,B
        {
            var cpu = Run(0x06,0x7F,0x78,0x76);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
            Debug.Assert(cpu.Registers.BC.High.Value == 0x7F);
        }

        static void Test0x70_0x77() // LD [HL],(ABCDEHL)
        {
            for (byte x = 0x70; x <= 0x77; ++x)
            {
                if (x == 0x76) continue; // skip HALT
                var expected = x == 0x74 ? 0xAB : x == 0x75 ? 0xCD : 0;
                var mem = new byte[0x10000];
                mem[0xABCD] = 0xFF;
                var code = new byte[] { 0x21, 0xCD, 0xAB, x, 0x76 };
                Array.Copy(code, mem, code.Length);
                var cpu = Run(mem);
                Debug.Assert(mem[0xABCD] == expected);
                Debug.Assert(cpu.regAF.Value == 0);
                Debug.Assert(cpu.Registers.HL.Value == 0xABCD);
            }
        }

        static void Test0x68_0x6F() // LD L,(ABCDEHL); LD L,[HL]
        {
            var cpu = Run(0x01,0xCD,0xAB,0x68,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x00AB && cpu.Flags.Value == 0);
            cpu = Run(0x01,0xCD,0xAB,0x69,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x00CD && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x6A,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x00AB && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x6B,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x00CD && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x6C,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xABAB && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x6D,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xABCD && cpu.Flags.Value == 0);

            cpu = Run(0x6E,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x006E && cpu.Flags.Value == 0);

            var mem = new byte[0x10000];
            mem[0xABCD] = 0xFF;
            var code = new byte[] { 0x21,0xCD,0xAB,0x6E,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.HL.Value == 0xABFF && cpu.Flags.Value == 0);

            cpu = Run(0x3C,0x3C,0x6F,0x76);
            Debug.Assert(cpu.regAF.Value == 0x0200);
            Debug.Assert(cpu.Registers.HL.Value == 2);
        }

        static void Test0x60_0x67() // LD H,(ABCDEHL); LD H,[HL]
        {
            var cpu = Run(0x01,0xCD,0xAB,0x60,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xAB00 && cpu.Flags.Value == 0);
            cpu = Run(0x01,0xCD,0xAB,0x61,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xCD00 && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x62,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xAB00 && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x63,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xCD00 && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x64,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xABCD && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x65,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xCDCD && cpu.Flags.Value == 0);

            cpu = Run(0x66,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0x6600 && cpu.Flags.Value == 0);

            var mem = new byte[0x10000];
            mem[0xABCD] = 0xFF;
            var code = new byte[] { 0x21,0xCD,0xAB,0x66,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.HL.Value == 0xFFCD && cpu.Flags.Value == 0);

            cpu = Run(0x3C,0x3C,0x67,0x76);
            Debug.Assert(cpu.regAF.Value == 0x0200);
            Debug.Assert(cpu.Registers.HL.Value == 0x0200);
        }

        static void Test0x58_0x5F() // LD E,(ABCDEHL); LD E,[HL]
        {
            var cpu = Run(0x01,0xCD,0xAB,0x58,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x00AB && cpu.Flags.Value == 0);
            cpu = Run(0x01,0xCD,0xAB,0x59,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x00CD && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x5A,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xABAB && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x5B,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xABCD && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x5C,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x00AB && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x5D,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x00CD && cpu.Flags.Value == 0);

            cpu = Run(0x5E,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x005E && cpu.Flags.Value == 0);

            var mem = new byte[0x10000];
            mem[0xABCD] = 0xFF;
            var code = new byte[] { 0x21,0xCD,0xAB,0x5E,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.DE.Value == 0x00FF && cpu.Flags.Value == 0);

            cpu = Run(0x3C,0x3C,0x5F,0x76);
            Debug.Assert(cpu.regAF.Value == 0x0200);
            Debug.Assert(cpu.Registers.DE.Value == 2);
        }

        static void Test0x50_0x57() // LD D,(ABCDEHL); LD D,[HL]
        {
            var cpu = Run(0x01,0xCD,0xAB,0x50,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xAB00 && cpu.Flags.Value == 0);
            cpu = Run(0x01,0xCD,0xAB,0x51,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xCD00 && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x52,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xABCD && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x53,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xCDCD && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x54,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xAB00 && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x55,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0xCD00 && cpu.Flags.Value == 0);

            cpu = Run(0x56,0x76);
            Debug.Assert(cpu.Registers.DE.Value == 0x5600 && cpu.Flags.Value == 0);

            var mem = new byte[0x10000];
            mem[0xABCD] = 0xFF;
            var code = new byte[] { 0x21,0xCD,0xAB,0x56,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.DE.Value == 0xFF00 && cpu.Flags.Value == 0);

            cpu = Run(0x3C,0x3C,0x57,0x76);
            Debug.Assert(cpu.regAF.Value == 0x0200);
            Debug.Assert(cpu.Registers.DE.Value == 0x0200);
        }

        static void Test0x48_0x4F() // LD C,(ABCDEHL); LD C,[HL]
        {
            var cpu = Run(0x01,0xCD,0xAB,0x48,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xABAB && cpu.Flags.Value == 0);
            cpu = Run(0x01,0xCD,0xAB,0x49,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xABCD && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x4A,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x00AB && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x4B,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x00CD && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x4C,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x00AB && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x4D,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x00CD && cpu.Flags.Value == 0);

            cpu = Run(0x4E,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x004E && cpu.Flags.Value == 0);

            var mem = new byte[0x10000];
            mem[0xABCD] = 0xFF;
            var code = new byte[] { 0x21,0xCD,0xAB,0x4E,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.BC.Value == 0x00FF && cpu.Flags.Value == 0);

            cpu = Run(0x3C,0x3C,0x4F,0x76);
            Debug.Assert(cpu.regAF.Value == 0x0200);
            Debug.Assert(cpu.Registers.BC.Value == 2);
        }

        static void Test0x40_0x47() // LD B,(ABCDEHL); LD B,[HL]
        {
            var cpu = Run(0x01,0xCD,0xAB,0x40,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xABCD && cpu.Flags.Value == 0);
            cpu = Run(0x01,0xCD,0xAB,0x41,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xCDCD && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x42,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xAB00 && cpu.Flags.Value == 0);
            cpu = Run(0x11,0xCD,0xAB,0x43,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xCD00 && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x44,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xAB00 && cpu.Flags.Value == 0);
            cpu = Run(0x21,0xCD,0xAB,0x45,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0xCD00 && cpu.Flags.Value == 0);

            cpu = Run(0x46,0x76);
            Debug.Assert(cpu.Registers.BC.Value == 0x4600 && cpu.Flags.Value == 0);

            var mem = new byte[0x10000];
            mem[0xABCD] = 0xFF;
            var code = new byte[] { 0x21,0xCD,0xAB,0x46,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.BC.Value == 0xFF00 && cpu.Flags.Value == 0);

            cpu = Run(0x3C,0x3C,0x47,0x76);
            Debug.Assert(cpu.regAF.Value == 0x0200);
            Debug.Assert(cpu.Registers.BC.Value == 0x0200);
        }

        static void Test0x3F() // CCF
        {
            var cpu = new CPU();
            cpu.Flags.Value = 0xFF; // all set
            cpu.Run(new Memory(0x3F,0x76));
            Debug.Assert(cpu.Flags.Value == 252);

            cpu = Run(0x37,0x3F,0x76); // SCF;CCF;HALT
            Debug.Assert(cpu.Flags.Value == 16); // halfcarry = old carry, carry = 0
        }

        static void Test0x3E() // LD A,*
        {
            var cpu = Run(0x3E,0x00,0x3E,0x7F,0x76);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
        }

        static void Test0x3D() // DEC A
        {
            var cpu = Run(0x3D,0x3D,0x3D,0x76);
            Debug.Assert(cpu.regAF.Value == 0xFD82); //add,sign

            cpu = Run(0x3C,0x3D,0x76);
            Debug.Assert(cpu.regAF.Value == 66); // add,zero
        }

        static void Test0x3C() // INC A
        {
            var cpu = Run(0x3C,0x3C,0x3C,0x76);
            Debug.Assert(cpu.regAF.Value == 0x0300);

            cpu = Run(0x3D,0x3C,0x76);
            Debug.Assert(cpu.regAF.Value == 80); // zero
        }

        static void Test0x3B() // DEC SP
        {
            var cpu = Run(0x3B,0x3B,0x76);
            Debug.Assert(cpu.regSP.Value == 0xFFFE);
            Debug.Assert(cpu.Flags.Value == 0);

            // LD SP,0x8000;DEC SP;HALT
            cpu = Run(0x31,0x00,0x80,0x3B,0x76);
            Debug.Assert(cpu.regSP.Value == 0x7FFF);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x3A() // LD A,[**]
        {
            var cpu = Run(0x3A,0x00,0x00,0x76);
            Debug.Assert(cpu.regAF.Value == 0x3A00);

            var mem = new byte[0x10000];
            mem[0xABCD] = 0x7F;
            var code = new byte[] { 0x3A,0xCD,0xAB,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.regAF.Value == 0x7F00);
        }

        static void Test0x39() // ADD HL,SP
        {
            var cpu = Run(0x31,0x54,0x55,0x39,0x39,0x39,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0xFFFC);
            Debug.Assert(cpu.regSP.Value == 0x5554);
            Debug.Assert(cpu.Flags.Value == 16); // halfcarry

            cpu = Run(0x31,0x00,0x80,0x39,0x39,0x76);
            Debug.Assert(cpu.Registers.HL.Value == 0);
            Debug.Assert(cpu.regSP.Value == 0x8000);
            Debug.Assert(cpu.Flags.Value == 1); // carry
        }

        static void Test0x38() // JR C,*
        {
            // SCF;JR C,1;DEC [HL];HALT
            var cpu = Run(0x37,0x38,0x01,0x35,0x76);
            Debug.Assert(cpu.Flags.Value == 1); // carry
            Debug.Assert(cpu.regPC.Value == 4);
        }
        
        static void Test0x37() // SCF
        {
            // INC L;DEC L;SCF
            var cpu = Run(0x2C,0x2D,0x37,0x76);

            Debug.Assert(cpu.Registers.HL.Value == 0);
            Debug.Assert(cpu.Flags.Value == 65); //zero,carry
        }

        static void Test0x36() // LD [HL],*
        {
            var mem = new byte[0x10000];

            // DEC HL;LD [HL],0xFF; DEC HL; LD [HL],0x7F;HALT
            var code = new byte[] { 0x2B,0x36,0xFF,0x2B,0x36,0x7F,0x76 }; 
            Array.Copy(code, mem, code.Length);
            var cpu = Run(mem);
            Debug.Assert(mem[0xFFFF] == 0xFF);
            Debug.Assert(mem[0xFFFE] == 0x7F);
            Debug.Assert(cpu.Registers.HL.Value == 0xFFFE);
            Debug.Assert(cpu.Flags.Value == 0);
        }

        static void Test0x35() // DEC [HL]
        {
            var mem = new byte[0x10000];

            // LD HL,0x5678;DEC [HL]; HALT
            var code = new byte[] { 0x21,0x78,0x56,0x35,0x76 }; 
            Array.Copy(code, mem, code.Length);
            var cpu = Run(mem);

            Debug.Assert(cpu.Registers.HL.Value == 0x5678);
            Debug.Assert(cpu.Flags.Value == 146); //addsub,halfcarry,sign
            Debug.Assert(mem[0x5678] == 0xFF);

            // LD HL,0x5679;INC [HL];DEC [HL];HALT
            code = new byte[] { 0x21,0x79,0x56,0x34,0x35,0x76 }; 
            Array.Copy(code, mem, code.Length);
            cpu = Run(mem);
            Debug.Assert(cpu.Registers.HL.Value == 0x5679);
            Debug.Assert(cpu.Flags.Value == 66); // addsub,zero
            Debug.Assert(mem[0x5678] == 0xFF); // from prev.test
            Debug.Assert(mem[0x5679] == 0);
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