using System.Drawing;

namespace z80emu
{
  // for every combination of ink/paper/flash and bit pattern,
  // contains 8 bytes sequence for screen display, total of 256*256*2 = 131072 records,
  // 8 bytes each, 1Mb total.
  class PregeneratedLines
  {
    class Line
    {
      public byte[] Data;
    }

    private readonly Line[] lookup;
    
    private PregeneratedLines() 
    {
      this.lookup = new Line[256*256*2];
    }

    public static PregeneratedLines Generate()
    {
      var ret = new PregeneratedLines();

      for (int flash = 0; flash <= 1; ++flash)
      for (int bits = 0; bits <= 255; ++bits)
      for (int color = 0; color <= 255; ++color)
      {
        bool f = flash == 1;
        byte b = (byte)bits;
        byte c = (byte)color;

        ret.lookup[GetKey(b,c,f)] = Draw(b,c,f);
      }

      return ret;
    }

    private static Line Draw(byte bits, byte color, bool flash)
    {
      var c = new ColorInfo(color);
      var data = new byte[8];
      for (int bit = 7; bit >= 0; bit--)
      {
        bool set = (bits & (1 << bit)) != 0;

        var selected = set ? c.Ink : c.Paper;

        selected = c.Bright ? selected | 0b1000 : selected;

        data[7-bit] = (byte)(set ? 0 : 7);//(byte)selected;
      }

      return new Line { Data = data };
    }

    public byte[] GetPixels(byte bits, byte color, bool flash)
    {
      return this.lookup[GetKey(bits, color, flash)].Data;
    }

    private static int GetKey(byte bits, byte color, bool flash)
    {
      return (bits << 8 | color) | (flash ? 0x10000 : 0);
    }
  }
}
