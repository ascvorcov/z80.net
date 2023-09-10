namespace z80emu
{
    using System;
    using System.Drawing;

    public class Emulator
    {
        private Spectrum128K speccy;

        public Emulator()
        {
            this.speccy = new Spectrum128K();
        }

        public void Run(Func<int> delay, System.Threading.CancellationToken token)
        {
            var nextSound = this.speccy.CPU.Clock.Ticks;
            while (!token.IsCancellationRequested)
            {
                bool continueExecution = this.speccy.CPU.Tick(this.speccy.Memory);
                if (!continueExecution)
                {
                    break;
                }

                var result = this.speccy.ULA.Tick(this.speccy.Memory);
                if (result.hasSound)
                {
                    var frame = this.speccy.ULA.GetSoundFrame();
                    this.NextSound.Invoke(new SoundEventArgs(frame));
                }

                if (result.hasVideo)
                {
                    var sleepMsec = delay();
                    if (sleepMsec != 0) // 0 removes sleep call
                    {
                        System.Threading.Thread.Sleep(sleepMsec);
                    }

                    var count = this.speccy.ULA.FrameCount;
                    var frame = this.speccy.ULA.GetVideoFrame();
                    var palette = this.speccy.ULA.Palette;
                    this.NextFrame.Invoke(new FrameEventArgs(frame, palette, count));
                }
            }
        }

        public Color[] Palette => this.speccy.ULA.Palette;

        public int SoundFrameSize => this.speccy.ULA.GetSoundFrame().Length;

        public int VideoFrameSize => this.speccy.ULA.GetVideoFrame().Length;

        public void KeyDown(Key key)=> this.speccy.ULA.KeyDown(key);

        public void KeyUp(Key key) => this.speccy.ULA.KeyUp(key);

        public void Dump() => this.speccy.CPU.Dump(this.speccy.Memory);

        public void Load(string file)
        {
           /*this.speccy = file == null
                ? new Spectrum48K() 
                : new Spectrum48K(file);*/
        }

        public event NextFrameEventHandler NextFrame = delegate {};
        public event NextSoundEventHandler NextSound = delegate {};
    }
}