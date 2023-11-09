namespace z80emu
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading;
    using z80emu.Loader;

    public class Emulator
    {
        private IComputer speccy;
        private TapePlayer player;
        private object sync = new object();

        public Emulator(bool use128K = false, ILoader loader = null)
        {
            this.speccy = use128K ? new Spectrum128K(loader) : new Spectrum48K(loader);
            this.player = new TapePlayer(this.speccy.CPU.Clock);
        }

        public FrameEventArgs RunToNextFrame(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                lock (this.sync)
                {
                    bool continueExecution = this.speccy.CPU.Tick(this.speccy.Memory);
                    if (!continueExecution)
                    {
                        break;
                    }

                    this.speccy.ULA.SetMic(this.player.Tick());
                    var result = this.speccy.ULA.Tick(this.speccy.Memory);
                    
                    if (result.hasVideo)
                    {
                        var count = this.speccy.ULA.FrameCount;
                        var frame = this.speccy.ULA.GetVideoFrame();
                        var palette = this.speccy.ULA.Palette;
                        return new FrameEventArgs(frame, palette, count);
                    }
                }
            }
            return null;
        }

        public void Run(Func<int> delay, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                lock (this.sync)
                {
                    bool continueExecution = this.speccy.CPU.Tick(this.speccy.Memory);
                    if (!continueExecution)
                    {
                        break;
                    }

                    this.speccy.ULA.SetMic(this.player.Tick());
                    var result = this.speccy.ULA.Tick(this.speccy.Memory);
                    
                    if (result.hasSound)
                    {
                        var frame = this.speccy.ULA.GetSoundFrame();
                        this.NextSound.Invoke(new SoundEventArgs(frame, 0));
                    }

                    if (this.speccy is Spectrum128K s)
                    {
                        var flags = s.EXT.Tick();
                        if (flags.HasFlag(ChannelFlags.ChannelA))
                            this.NextSound.Invoke(new SoundEventArgs(s.EXT.GetSoundFrame(0), 1));
                        if (flags.HasFlag(ChannelFlags.ChannelB))
                            this.NextSound.Invoke(new SoundEventArgs(s.EXT.GetSoundFrame(1), 2));
                        if (flags.HasFlag(ChannelFlags.ChannelC))
                            this.NextSound.Invoke(new SoundEventArgs(s.EXT.GetSoundFrame(2), 3));
                    }

                    if (result.hasVideo)
                    {
                        var sleepMsec = delay();
                        if (sleepMsec != 0) // 0 removes sleep call
                        {
                            Thread.Sleep(sleepMsec);
                        }

                        var count = this.speccy.ULA.FrameCount;
                        var frame = this.speccy.ULA.GetVideoFrame();
                        var palette = this.speccy.ULA.Palette;
                        this.NextFrame.Invoke(new FrameEventArgs(frame, palette, count));
                    }
                }
            }
        }

        public Color[] Palette => this.speccy.ULA.Palette;
        public int SoundFrameSize => this.speccy.ULA.GetSettings().SoundFrameSize;
        public int SoundSamplesPerSec => this.speccy.ULA.GetSettings().SoundSamplesPerSec;
        public int SoundChannelsCount => this.speccy.ULA.GetSettings().SoundChannelsCount;
        public int VideoFrameSize => this.speccy.ULA.GetSettings().VideoFrameSize;

        public void KeyDown(Key key)=> this.speccy.ULA.KeyDown(key);

        public void KeyUp(Key key) => this.speccy.ULA.KeyUp(key);

        public void Dump() => this.speccy.CPU.Dump(this.speccy.Memory);

        public void Load(string file)
        {
            if (file.ToUpper().EndsWith(".TAP"))
            {
                this.player.Load(file);
                return;
            }

            LoadZ80(File.ReadAllBytes(file));
        }

        public void LoadZ80(byte[] data)
        {
            lock(this.sync)
            {
                var fmt = new Z80Format(data);
                this.speccy = fmt.LoadZ80();
            }
        }

        public event NextFrameEventHandler NextFrame = delegate {};
        public event NextSoundEventHandler NextSound = delegate {};
    }
}