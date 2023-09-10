using System;

namespace z80emu
{
    class Ext128KDevice : IDevice
    {
        private AYChip ay;
        private MemoryExtended memory;

        public Ext128KDevice(MemoryExtended memory, AYChip ay)
        {
            this.memory = memory;
            this.ay = ay;
        }

        public event EventHandler Interrupt;

        byte IDevice.Read(byte highPart)
        {
            if (highPart == 0xFF)
            {
                return this.ay.GetSelectedRegister();
            }
            else if (highPart == 0xBF)
            {
                return this.ay.ReadFromSelectedRegister();
            }

            return 0xFF;
        }

        void IDevice.Write(byte highPart, byte value)
        {
            if (highPart == 0xFF)
            {
                this.ay.SelectRegister(value);
            }
            else if (highPart == 0xBF)
            {
                this.ay.WriteToSelectedRegister(value);
            }
            else if (highPart == 0x7F)
            {
                var bank = value & 0b111;
                var shadow = (value & 0b1000) != 0;
                var oldrom = (value & 0b10000) != 0;
                var disable = (value & 0b100000) != 0;

                this.memory.SetBank((byte)bank);
                if (shadow)
                    this.memory.ShowShadowScreen();
                else
                    this.memory.ShowNormalScreen();

                if (oldrom)
                    this.memory.Select48KROM();
                else
                    this.memory.Select128KROM();

                if (disable)
                    this.memory.DisablePaging();
            }
        }

        public void Tick()
        {

        }
    }
}
