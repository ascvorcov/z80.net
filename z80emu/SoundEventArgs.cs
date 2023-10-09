namespace z80emu
{
    using System;

    public delegate void NextSoundEventHandler(SoundEventArgs e);

  public class SoundEventArgs : EventArgs
  {
    private byte[] soundFrame;
    public SoundEventArgs(byte[] soundFrame, int channel)
    {
      this.soundFrame = soundFrame;
      this.Channel = channel;
    }

    public int Channel {get;}

    public byte[] GetFrame()
    {
      return this.soundFrame;
    }
  }
}