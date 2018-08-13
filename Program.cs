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