namespace z80emu.Loader
{
    using System;

    class Load
    {
        public static byte[] Spectrum48KROM()
        {
            return Resource.Load("z80emu.48.rom");
        }
        public static byte[] Spectrum128KROM0()
        {
            return Resource.Load("z80emu.128-0.rom");
        }
        public static byte[] Spectrum128KROM1()
        {
            return Resource.Load("z80emu.128-1.rom");
        }

    }
}