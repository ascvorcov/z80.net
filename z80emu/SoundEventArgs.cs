namespace z80emu
{
  using System;

  public delegate void NextSoundEventHandler(SoundEventArgs e);

  public class SoundEventArgs : EventArgs
  {
    public SoundEventArgs(byte[] soundFrame)
    {
      this.Frame = soundFrame;
    }

    public byte[] Frame {get;}
  }
}