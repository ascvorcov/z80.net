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
            (this.cpu, this.ula, this.mem) = Loader.Load.VanillaZ80Rom();
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
                    System.Threading.Thread.Sleep(10);
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
           (this.cpu, this.ula, this.mem) = file == null 
                ? Loader.Load.VanillaZ80Rom() 
                : Loader.Load.Z80FormatImage(file);
        }

        public event NextFrameEventHandler NextFrame;
    }
}