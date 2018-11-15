namespace z80emu
{
  using System;
  using System.IO;
  using System.Reflection;

    public class Emulator
    {
        private Memory mem;
        private CPU cpu;
        private ULA ula;

        public Emulator()
        {
            var loader = new Loader();
            var x = loader.LoadRom();
            this.cpu = x.Item1;
            this.ula = x.Item2;
            this.mem = x.Item3;
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

        public void Load(string file)
        {
            var loader = new Loader();
            var x = file == null ? loader.LoadRom() : loader.LoadZ80(file);
            this.cpu = x.Item1;
            this.ula = x.Item2;
            this.mem = x.Item3;
        }

        public event NextFrameEventHandler NextFrame;
    }
}