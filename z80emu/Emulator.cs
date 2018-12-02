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

        public void Run(Func<int> delay, System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool continueExecution = this.cpu.Tick(this.mem);
                if (!continueExecution)
                {
                    break;
                }

                bool nextFrameAvailable = this.ula.Tick(this.mem, this.cpu.Clock);
                if (nextFrameAvailable)
                {
                    var sleepMsec = delay();
                    if (sleepMsec != 0) // 0 removes sleep call
                    {
                        System.Threading.Thread.Sleep(sleepMsec);
                    }

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