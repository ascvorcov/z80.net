namespace z80emu
{
  using System;

  public delegate void NextBeepEventHandler(BeepEventArgs e);

  public class BeepEventArgs : EventArgs
  {
    public BeepEventArgs(int frequency, int duration)
    {
      this.Frequency = frequency;
      this.Duration = duration;
    }

    public int Frequency {get;}

    public int Duration {get;}
  }
}