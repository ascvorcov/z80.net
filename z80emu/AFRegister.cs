namespace z80emu
{
    class AFRegister : WordRegister
    {
        public AFRegister()
        {
            this.High = new ByteRegister(new WordRegister.HighStorage(this));
            this.Low = new FlagsRegister(new WordRegister.LowStorage(this));
        }

        public FlagsRegister F => (FlagsRegister)Low;
        public ByteRegister A => High;
    }
}