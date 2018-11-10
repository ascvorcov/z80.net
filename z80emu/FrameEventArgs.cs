namespace z80emu
{
  using System;
  using System.Drawing;

  public delegate void NextFrameEventHandler(FrameEventArgs e);

  public class FrameEventArgs : EventArgs
  {
    public FrameEventArgs(byte[] frame, Color[] palette)
    {
      this.Frame = frame;
      this.Palette = palette;
    }

    public byte[] Frame {get;}

    public Color[] Palette {get;}

  }
}