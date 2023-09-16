using z80emu.Loader;

namespace z80emu
{
    class Spectrum128K : IComputer
    {
        public Spectrum128K()
        {
            var clk = new Clock();

            this.MemoryExt = new MemoryExtended(
                Load.Spectrum128KROM0(),
                Load.Spectrum128KROM1()
            );
            this.CPU = new CPU(clk);
            this.ULA = new ULA(clk, new ULA.Settings { TicksPerFrame = 70908 , TicksPerScanline = 228, UpperScanlines = 63 });
            this.AY = new AYChip();
            this.EXT = new Ext128KDevice(this.MemoryExt, this.AY);

            this.CPU.Bind(0xFF, this.ULA.LeakyPort);
            this.CPU.Bind(0xFE, this.ULA);
            this.CPU.Bind(0xFD, this.EXT);
        }
        public MemoryExtended MemoryExt { get; }
        IMemory IComputer.Memory => MemoryExt;
        public CPU CPU { get; }
        public ULA ULA  { get; }
        public Ext128KDevice EXT { get; }
        public AYChip AY { get; }
    }
}
