namespace z80emu.Loader
{
    public struct BitInfo2
    {
        private byte data;
        public BitInfo2(byte data)
        {
            this.data = data;
        }
        //Bit 0-1: Interrupt mode (0, 1 or 2)
        public int InterruptMode => this.data & 3;

        //Bit 2  : 1=Issue 2 emulation
        public bool Issue2 => (this.data & 4) != 0;

        //Bit 3  : 1=Double interrupt frequency
        public bool DoubleFreq => (this.data & 8) != 0;

        //Bit 4-5: 1=High video synchronisation
        //        3=Low video synchronisation
        //        0,2=Normal
        public int VideoSyncMode => (this.data >> 4) & 3;

        //Bit 6-7: 0=Cursor/Protek/AGF joystick
        //        1=Kempston joystick
        //        2=Sinclair 2 Left joystick (or user defined, for version 3 .z80 files)
        //        3=Sinclair 2 Right joystick
        public int Joystick => (this.data >> 6) & 3;
    }
}