using System;
using System.Runtime.InteropServices;

namespace z80emu
{
    internal class AYStorage
    {
        public byte[] registers = new byte[16];
    }
    
    // AY-3-8912
    class AYChip
    {
        private AYStorage storage = new AYStorage();

        private byte selected;

        public void SelectRegister(byte reg)
        {
            this.selected = reg;
        }
        public byte GetSelectedRegister()
        {
            return this.selected;
        }

        public void WriteToSelectedRegister(byte value)
        {
            if (this.selected < 16)
            {
                this.storage.registers[this.selected] = value;
            }
        }

        public byte ReadFromSelectedRegister()
        {
            if (this.selected < 16)
            {
                return this.storage.registers[this.selected];
            }
            return 0xFF;
        }

        public bool Tick()
        {
            return false;
        }

        public byte[] GetSoundFrame()
        {
            return new byte[0];
        }
    }
}
