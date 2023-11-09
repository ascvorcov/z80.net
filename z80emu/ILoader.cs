namespace z80emu
{
    public interface ILoader
    {
        byte[] Spectrum48KROM();
        byte[] Spectrum128KROM0();
        byte[] Spectrum128KROM1();
    }
}