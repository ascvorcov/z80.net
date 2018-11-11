namespace z80emu
{
  using System;
  using System.IO;
  using System.Reflection;

    public class Emulator
    {
        private readonly Memory mem;
        private readonly CPU cpu;
        private readonly ULA ula;

        public Emulator()
        {
            var rom = LoadROM();
            Array.Resize(ref rom, 0x10000);
            this.mem = new Memory(rom);
            this.cpu = new CPU();
            this.ula = new ULA();
            this.cpu.Bind(0xFE, this.ula);
        }

        public void Run(System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!this.cpu.Tick(this.mem))
                {
                    break;
                }

                if (this.ula.Tick(this.mem, this.cpu.Clock))
                {
                    var count = this.ula.FrameCount;
                    var frame = this.ula.GetFrame();
                    var palette = this.ula.Palette;
                    this.NextFrame.Invoke(new FrameEventArgs(frame, palette, count));
                }
            }
        }

        public void KeyDown(Key key)=> this.ula.KeyDown(key);

        public void KeyUp(Key key) => this.ula.KeyUp(key);

        public void Dump() => this.cpu.Dump(this.mem);

        public event NextFrameEventHandler NextFrame;

        private static byte[] LoadROM()
        {
            var assembly = typeof(Emulator).Assembly;
            var resourceStream = assembly.GetManifestResourceStream("z80emu.48.rom");
            using (var ms = new MemoryStream())
            {
                resourceStream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}