namespace z80emu.Loader
{
    public struct BitInfo1
    {
        private byte data;
        public BitInfo1(byte data)
        {
          this.data = data == 255 ? (byte)1 : data;
        }

        // Bit 0  : Bit 7 of the R-register
        public byte Bit7 => (byte)(this.data << 7);

        // Bit 1-3: Border colour
        public byte BorderColor => (byte)((this.data >> 1)&7);

        // Bit 4  : 1=Basic SamRom switched in
        public bool SamRom => (this.data & 16) != 0;

        // Bit 5  : 1=Block of data is compressed
        public bool Compressed => (this.data & 32) != 0;

        // Bit 6-7: No meaning
    }
}