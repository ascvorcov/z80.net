using z80emu.Loader;

namespace z80emu
{
    class Spectrum128K
    {
        public Spectrum128K()
        {
            var clk = new Clock();

            this.Memory = new MemoryExtended(
                Load.Spectrum128KROM0(),
                Load.Spectrum128KROM1()
            );
            this.CPU = new CPU(clk);
            this.ULA = new ULA(clk);
            this.AY = new AYChip();
            this.EXT = new Ext128KDevice(this.Memory, this.AY);

            this.CPU.Bind(0xFF, this.ULA.LeakyPort);
            this.CPU.Bind(0xFE, this.ULA);
            this.CPU.Bind(0xFD, this.EXT);
        }
        public MemoryExtended Memory { get; }
        public CPU CPU { get; }
        public ULA ULA  { get; }
        public Ext128KDevice EXT { get; }
        public AYChip AY { get; }
    }
}
