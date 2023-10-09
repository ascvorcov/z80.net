using z80emu.Loader;

namespace z80emu
{
    class Spectrum128K : IComputer
    {
        public Spectrum128K()
        {
            var clk = new Clock();
            var settings = new ULA.Settings
            {
                TicksPerFrame = 70908,
                TicksPerScanline = 228,
                UpperScanlines = 63,
                SoundFrameSize = 652,
                SoundSamplesPerSec = 44336,
                SoundChannelsCount = 4 // 3 from AY and 1 beeper
            };

            this.MemoryExt = new MemoryExtended(
                Load.Spectrum128KROM0(),
                Load.Spectrum128KROM1()
            );
            this.CPU = new CPU(clk);
            this.ULA = new ULA(clk, settings);
            this.EXT = new Ext128KDevice(this.MemoryExt, new AYChip(), clk);

            this.CPU.Bind(0xFF, this.ULA.LeakyPort);
            this.CPU.Bind(0xFE, this.ULA);
            this.CPU.Bind(0xFD, this.EXT);
        }
        public MemoryExtended MemoryExt { get; }
        IMemory IComputer.Memory => MemoryExt;
        public CPU CPU { get; }
        public ULA ULA  { get; }
        public Ext128KDevice EXT { get; }
    }
}
