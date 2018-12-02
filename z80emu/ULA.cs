using System;
using System.Drawing;
using System.Threading;
using System.Linq;

namespace z80emu
{
  class ULA : IDevice
  {
    private long frameCount = 0;
    private long interruptStartedAt = 0;
    private long nextLineAt = 0;

    private byte[] keyboard = { 0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF };

    private byte[] currentFrame = new byte[352*312];
    private byte[] lastFrame = new byte[352*312];

    private PregeneratedLines lookup = PregeneratedLines.Generate();

    public Color[] Palette {get;} = new[]
    {
      Color.FromArgb(0x000000),
      Color.FromArgb(0x0000D7),
      Color.FromArgb(0xD70000),
      Color.FromArgb(0xD700D7),
      Color.FromArgb(0x00D700),
      Color.FromArgb(0x00D7D7),
      Color.FromArgb(0xD7D700),
      Color.FromArgb(0xD7D7D7),

      Color.FromArgb(0x000000),
      Color.FromArgb(0x0000FF),
      Color.FromArgb(0xFF0000),
      Color.FromArgb(0xFF00FF),
      Color.FromArgb(0x00FF00),
      Color.FromArgb(0x00FFFF),
      Color.FromArgb(0xFFFF00),
      Color.FromArgb(0xFFFFFF)
    };

    public byte BorderColor {get;set;}

    public long FrameCount => this.frameCount;

    public byte[] GetFrame()
    {
      return lastFrame;
    }

    public event EventHandler Interrupt = delegate {};

    byte IDevice.Read(byte highPart)
    {
      byte ret = 0xFF;
      if ((highPart & 1) == 0) ret &= keyboard[0];
      if ((highPart & 2) == 0) ret &= keyboard[1];
      if ((highPart & 4) == 0) ret &= keyboard[2];
      if ((highPart & 8) == 0) ret &= keyboard[3];
      if ((highPart & 16) == 0) ret &= keyboard[4];
      if ((highPart & 32) == 0) ret &= keyboard[5];
      if ((highPart & 64) == 0) ret &= keyboard[6];
      if ((highPart & 128) == 0) ret &= keyboard[7];
      return ret;
    }

    void IDevice.Write(byte highPart, byte value)
    {
      this.BorderColor = (byte)(value & 7);
      // todo: MIC and EAR bits;
    }

    public void KeyDown(Key key)
    {
      this.keyboard[(int)key >> 8] &= (byte)~key;
    }

    public void KeyUp(Key key)
    {
      this.keyboard[(int)key >> 8] |= (byte)key;
    }

    public bool Tick(Memory mem, long clock)
    {
      bool ret = false;
      if (clock >= interruptStartedAt + 69888)
      {
        ret = true;
        frameCount++;
        currentFrame = Interlocked.Exchange(ref lastFrame, currentFrame);

        Interrupt.Invoke(this, null);
        // '50 Hz' interrupt is actually a 3.5MHz/69888=50.08 Hz interrupt.
        interruptStartedAt = clock; 
        nextLineAt = clock;
      }

      // use while in case we were late and missed several cycles
      while (clock >= nextLineAt)
      {
        // a frame is (64+192+56)*224=69888 T states long 
        // (224 tstates per line = 64/56 upper/lower border + 192 screen).
        // for simplicity, we copy 1 line every 224 ticks.
        // actual resolution is 256x192 main screen, l/r border is 48 pixels wide,
        // upper/lower border is 64/56 pixels high, giving total of 352x312

        var currentLine = (nextLineAt - interruptStartedAt) / 224;
        CopyScreenLine(mem, (int)currentLine);
        nextLineAt += 224;
      }

      return ret; // returns true if next frame is available
    }

    private void CopyScreenLine(Memory mem, int y)
    {
      const int lineSize = 352;
      const int borderLR = 48;

      var offset = y * lineSize;
      if (y < 48 || y >= 240)
      {
        // upper/lower border part
        Array.Fill(currentFrame, BorderColor, offset, lineSize);
        return;
      }

      Array.Fill(currentFrame, BorderColor, offset, borderLR);
      Array.Fill(currentFrame, BorderColor, offset + lineSize - borderLR, borderLR);

      offset += borderLR; // reposition from border to screen
      
      // screen Y is different from absolute bitmap Y, and does not include border
      var y0 = y - 48; 

      // compute vertical offset, encoded as [Y7 Y6] [Y2 Y1 Y0] [Y5 Y4 Y3] [X4 X3 X2 X1 X0]
      var newY = (y0 & 0b11_000_000) | (y0 << 3 & 0b00_111_000) | (y0 >> 3 & 0b00_000_111);
      var bitmapOffset = 0x4000 + (newY << 5);

      var colorInfoOffset = 0x5800 + y0 / 8 * 32;
      bool flash = (frameCount & 16) != 0; // bit 4 is toggled every 16 frames
      for (var chx = 0; chx < 32; chx++)
      {
        var bits = mem.ReadByte((ushort)(chx + bitmapOffset));
        var color = mem.ReadByte((ushort)(chx + colorInfoOffset));
        var src = lookup.GetPixels(bits, color, flash);
        Array.Copy(src, 0, currentFrame, offset + chx*8, src.Length);
      }
    }
  }
}
