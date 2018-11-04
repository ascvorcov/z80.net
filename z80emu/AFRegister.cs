namespace z80emu
{
    class AFRegister : WordRegister
    {
        public AFRegister()
        {
            this.High = ByteRegister.High(this);
            this.Low = new FlagsRegister(this);
        }

        public FlagsRegister F => (FlagsRegister)Low;
        public ByteRegister A => High;
    }
}