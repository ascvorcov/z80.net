namespace z80emu
{
    using System;

    public delegate void NextSoundEventHandler(SoundEventArgs e);

  public class SoundEventArgs : EventArgs
  {
    private byte[] channel0;
    private byte[] channel1;
    private byte[] channel2;
    public SoundEventArgs(byte[] soundFrame)
    {
      this.channel0 = soundFrame;
      this.ChannelCount = 1;
    }
    public SoundEventArgs(byte[] channel0, byte[] channel1, byte[] channel2)
    {
      this.channel0 = channel0;
      this.channel1 = channel1;
      this.channel2 = channel2;
      this.ChannelCount = 3;
    }

    public byte[] GetFrame(int channel)
    {
      if (channel >= ChannelCount || channel < 0)
        throw new ArgumentOutOfRangeException();

      switch(channel)
      {
        case 0: return this.channel0;
        case 1: return this.channel1;
        case 2: return this.channel2;
        default: throw new InvalidOperationException();
      }
    }

    public int ChannelCount {get;}
  }
}