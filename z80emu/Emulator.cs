namespace z80emu
{
    using System;
    using System.Drawing;
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

        public byte[] NextVideoFrame()
        {
            while (true)
            {
                if (!this.cpu.Tick(this.mem))
                {
                    return null; // halted
                }

                var result = this.ula.Tick(this.mem);
                if (result.hasVideo)
                {
                    return this.ula.GetVideoFrame();
                }
            }
        }

        public void Run(Func<int> delay, System.Threading.CancellationToken token)
        {
            var nextSound = this.cpu.Clock.Ticks;
            while (!token.IsCancellationRequested)
            {
                bool continueExecution = this.cpu.Tick(this.mem);
                if (!continueExecution)
                {
                    break;
                }

                var result = this.ula.Tick(this.mem);
                if (result.hasSound)
                {
                    var frame = this.ula.GetSoundFrame();
                    this.NextSound.Invoke(new SoundEventArgs(frame));
                }

                if (result.hasVideo)
                {
                    var sleepMsec = delay();
                    if (sleepMsec != 0) // 0 removes sleep call
                    {
                        System.Threading.Thread.Sleep(sleepMsec);
                    }

                    var count = this.ula.FrameCount;
                    var frame = this.ula.GetVideoFrame();
                    var palette = this.ula.Palette;
                    this.NextFrame.Invoke(new FrameEventArgs(frame, palette, count));
                }
            }
        }

        public Color[] Palette => this.ula.Palette;

        public int SoundFrameSize => this.ula.GetSoundFrame().Length;

        public int VideoFrameSize => this.ula.GetVideoFrame().Length;

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
        public event NextSoundEventHandler NextSound;
    }
}