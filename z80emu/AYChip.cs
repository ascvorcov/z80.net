using System;
using System.Runtime.InteropServices;

namespace z80emu
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct AYStorage
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        [FieldOffset(0)]
        public byte[] registers;
    }
    
    // AY-3-8912
    class AYChip
    {
        private AYStorage storage;
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
    }
}
