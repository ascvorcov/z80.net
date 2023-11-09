namespace z80emu.Loader
{
    class ResourceLoader : ILoader
    {
        public byte[] Spectrum48KROM()
        {
            return Resource.Load("z80emu.48.rom");
        }
        public byte[] Spectrum128KROM0()
        {
            return Resource.Load("z80emu.128-0.rom");
        }
        public byte[] Spectrum128KROM1()
        {
            return Resource.Load("z80emu.128-1.rom");
        }

    }
}