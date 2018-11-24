using System;
using System.Diagnostics;
using word = System.UInt16;

namespace z80emu
{
    interface IClock
    {
        void Tick(int t);
    }

    class CPU : IClock
    {
        public WordRegister regPC = new WordRegister();
        public WordRegister regSP = new WordRegister();
        public WordRegister regIX = new WordRegister();
        public WordRegister regIY = new WordRegister();

        public AFRegister regAF = new AFRegister();
        public AFRegister regAFx = new AFRegister();

        public GeneralRegisters Registers = new GeneralRegisters();
        public GeneralRegisters RegistersCopy = new GeneralRegisters();

        public Port Port = new Port();
        public long InterruptRaisedUntil;
        
        public WordRegister regIR = new WordRegister();
        public ByteRegister regI => regIR.High;
        public ByteRegister regR => regIR.Low;
        public bool IFF1;
        public bool IFF2;
        public int InterruptMode = 0;
        public long Clock = 0;
        public bool Halted = false;

        public IInstruction handler;

        public CPU()
        {
            handler = new InstructionTable(this).BuildTable();
        }

        public FlagsRegister Flags => this.regAF.F;

        public void Run(Memory memory)
        {
            while (Tick(memory));
        }

        public bool Tick(Memory memory)
        {
            Dump(memory);
            bool allowCheckInterrupt = true;
            if (!this.Halted)
            {
                var instruction = memory.ReadByte(regPC.Value);
                if (instruction == 0x76 && IFF1 == false) 
                {
                    // halt breaks execution if interrupts are disabled
                    return false; 
                }

                if (instruction == 0xFB)
                {
                    // When an EI instruction is executed, any pending interrupt request 
                    // is not accepted until after the instruction following EI is executed
                    allowCheckInterrupt = false;
                }

                handler.Execute(memory);
            }
            else
            {
                IClock clock = this;
                clock.Tick(4); // in halted state nops are executed
            }

            if (allowCheckInterrupt)
            {
                this.CheckInterrupt(memory);
            }

            return true;
        }

        public void Bind(byte port, IDevice device)
        {
            Port.Bind(port, device);
            // On the 48K Spectrum, the ULA holds the /INT pin low for precisely 32 T-states
            device.Interrupt += (s, a) => InterruptRaisedUntil = Clock + 32;
        }

        public bool traceEnabled = false;
        public void Dump(Memory mem)
        {
            Labels lab = new Labels();
            var l = lab.GetLabel(regPC.Value);
            if (l != null)
            {
                if (l == "THE 'POSITION STORE' SUBROUTINE")
                    Console.WriteLine("Hello");
                Console.WriteLine(l);
            }
            if (!traceEnabled) return;

            mem.Dump();
            var instruction = mem.ReadByte(regPC.Value);

            Console.Write($"OP={instruction:X} ");
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

        void IClock.Tick(int t)
        {
            this.Clock += t;
        }

        private void CheckInterrupt(Memory m)
        {
            if (InterruptRaisedUntil == 0) 
                return; // no interrupt is raised, nothing to handle

            if (Clock >= InterruptRaisedUntil)
            {
                // interrupt was held for 32 ticks, but handling was disabled. ignore.
                InterruptRaisedUntil = 0;
                return;
            }

            if (!IFF1)
                return; // interrupts disabled

            Halted = false;
            IFF1 = IFF2 = false;
            switch(InterruptMode)
            {
                case 0:
                case 1:
                    regSP.Value -= 2;
                    m.WriteWord(regSP.Value, regPC.Value);
                    regPC.Value = 0x38;
                    break;
                case 2:
                    var offset = m.ReadWord((word)(regI.Value << 8));
                    regSP.Value -= 2;
                    m.WriteWord(regSP.Value, regPC.Value);
                    regPC.Value = offset;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
