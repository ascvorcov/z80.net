namespace z80emu
{
    using System;

    public delegate void NextSoundEventHandler(SoundEventArgs e);

  public class SoundEventArgs : EventArgs
  {
    private byte[] soundFrame;
    public SoundEventArgs(byte[] soundFrame)
    {
      this.soundFrame = soundFrame;
    }

    public byte[] GetFrame()
    {
      return this.soundFrame;
    }
  }
}