namespace z80emu
{
  using System;
  using System.Drawing;

  public delegate void NextFrameEventHandler(FrameEventArgs e);

  public class FrameEventArgs : EventArgs
  {
    public FrameEventArgs(byte[] frame, Color[] palette, long frameCount)
    {
      this.Frame = frame;
      this.Palette = palette;
      this.FrameNumber = frameCount;
    }

    public byte[] Frame {get;}

    public Color[] Palette {get;}

    public long FrameNumber {get;}

  }
}