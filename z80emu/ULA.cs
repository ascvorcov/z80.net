using System;
using System.Drawing;
using System.Threading;
using z80emu.Loader;

namespace z80emu
{
  class ULA : IDevice
  {
    public class Settings
    {
      public int TicksPerFrame = 69888;
      public int UpperScanlines = 64;
      public int TicksPerScanline = 224;
      public int SoundFrameSize = 875; // can also be 850,1250 - divisors of SoundSamplesPerSec
      public int SoundSamplesPerSec = 43750;

      public int VideoFrameSize => (UpperScanlines + 192 + 56) * 352;
    }

    private long frameCount = 0;
    private long interruptStartedAt = 0;
    private long nextLineAt = 0;
    private long nextSoundSampleAt = 0;

    private byte[] keyboard = { 0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF };

    private byte[] currentVideoFrame;
    private byte[] lastVideoFrame;

    private int currentSoundSampleBit = 0;
    private byte[] currentSoundFrame;
    private byte[] lastSoundFrame;

    private bool earIsOn = false; // also controls if mic is on
    private bool micSignal = false;
    private bool hasAnySoundDataInFrame = false;

    private Clock clock;
    private Settings settings;
    private PregeneratedLines lookup = PregeneratedLines.Generate();
    private EmptyDeviceVideoLeak leakyPort = new EmptyDeviceVideoLeak();

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

    // hack for original ZX spectrum - ULA is leaking video info through reads to non-existing ports
    public IDevice LeakyPort => this.leakyPort;

    public ULA(Clock clock, Settings settings = null)
    {
      this.clock = clock;
      this.settings = settings ?? new Settings();
      this.currentSoundFrame = new byte[this.settings.SoundFrameSize];
      this.lastSoundFrame = new byte[this.settings.SoundFrameSize];

      this.currentVideoFrame = new byte[this.settings.VideoFrameSize];
      this.lastVideoFrame = new byte[this.settings.VideoFrameSize];
    }

    public Settings GetSettings()
    {
      return this.settings;
    }

    public byte[] GetVideoFrame()
    {
      return this.lastVideoFrame;
    }

    public byte[] GetSoundFrame()
    {
      return this.lastSoundFrame;
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

      if (this.earIsOn)
      {
        if (!this.micSignal)
          ret |= (byte)0b01000000;
        else
          ret &= (byte)0b10111111;
      }

      return ret;
    }

    void IDevice.Write(byte highPart, byte value)
    {
      this.BorderColor = (byte)(value & 0b111);
      this.earIsOn = (value & 0b11000) != 0;
    }

    public void SetMic(bool high)
    {
      this.micSignal = high;
    }

    public void KeyDown(Key key)
    {
      this.keyboard[(int)key >> 8] &= (byte)~key;
    }

    public void KeyUp(Key key)
    {
      this.keyboard[(int)key >> 8] |= (byte)key;
    }

    public (bool hasSound, bool hasVideo) Tick(IMemory mem)
    {
      bool hasNewVideoFrame = false;
      bool hasNewSoundFrame = false;
      this.leakyPort.Reset();
      if (this.clock.Ticks >= this.interruptStartedAt + this.settings.TicksPerFrame)
      {
        hasNewVideoFrame = true;
        this.frameCount++;
        this.currentVideoFrame = Interlocked.Exchange(ref this.lastVideoFrame, this.currentVideoFrame);
        this.Interrupt.Invoke(this, null);

        // '50 Hz' interrupt is actually a 
        // 3.5MHz/69888=50.08 Hz interrupt on 48k, and
        // 3.54690MHz/70908=50.01 Hz interrupt on 128k
        this.interruptStartedAt = this.clock.Ticks; 
        this.nextLineAt = this.clock.Ticks;
      }

      if (this.clock.Ticks >= nextSoundSampleAt)
      {
        // sample sound every 80 ticks - with 3.5Mhz frequency,
        // 3500000 / 80 = 43750 samples per second on 48k, closest integer to 44.1Khz
        // 3546900 / 80 = 44336 on 128k
        bool hasSound = this.earIsOn;
        this.nextSoundSampleAt += 80;
        this.currentSoundFrame[this.currentSoundSampleBit++] = hasSound ? (byte)0xFF : (byte)0;
        this.hasAnySoundDataInFrame = hasSound ? true : this.hasAnySoundDataInFrame;

        if (this.currentSoundSampleBit == this.currentSoundFrame.Length)
        {
          // sound frame is 875 samples - 50 samples per second, 30 msec each
          // if sound frame is all 0 - ignore this sound frame
          hasNewSoundFrame = this.hasAnySoundDataInFrame;
          this.hasAnySoundDataInFrame = false;
          this.currentSoundSampleBit = 0;
          this.currentSoundFrame = Interlocked.Exchange(ref this.lastSoundFrame, this.currentSoundFrame);
        }
      }

      // use while in case we were late and missed several cycles
      while (this.clock.Ticks >= this.nextLineAt)
      {
        // on 48k,
        // a frame is (64+192+56)*224=69888 T states long 
        // (224 tstates per line = 64/56 upper/lower border + 192 screen).

        // on 128k
        // a frame is (63+192+56)*228=70908 T states long
        // 228 tstates per line = 63/56 upper/lower border + 192 screen)

        // for simplicity, we copy 1 line every 224 ticks.
        // actual resolution is 256x192 main screen, l/r border is 48 pixels wide,
        // upper/lower border is 64/56 pixels high, giving total of 352x312

        var currentLine = (this.nextLineAt - this.interruptStartedAt) / this.settings.TicksPerScanline;
        CopyScreenLine(mem, (int)currentLine);
        this.nextLineAt += this.settings.TicksPerScanline;
      }

      return (hasNewSoundFrame, hasNewVideoFrame); // returns true if next frame is available
    }

    private void CopyScreenLine(IMemory mem, int y)
    {
      const int lineSize = 352;
      const int borderLR = 48;

      var offset = y * lineSize;
      
      // although top border is 48 pixels high, we add extra 16 'lines' to simulate
      // vertical ray retrace timing
      if (y < this.settings.UpperScanlines || y >= this.settings.UpperScanlines + 192)
      {
        // upper/lower border part
        Array.Fill(this.currentVideoFrame, BorderColor, offset, lineSize);
        return;
      }

      Array.Fill(this.currentVideoFrame, BorderColor, offset, borderLR);
      Array.Fill(this.currentVideoFrame, BorderColor, offset + lineSize - borderLR, borderLR);

      offset += borderLR; // reposition from border to screen
      
      // screen Y is different from absolute bitmap Y, and does not include border
      var y0 = y - this.settings.UpperScanlines; 

      // compute vertical offset, encoded as [Y7 Y6] [Y2 Y1 Y0] [Y5 Y4 Y3] [X4 X3 X2 X1 X0]
      var newY = (y0 & 0b11_000_000) | (y0 << 3 & 0b00_111_000) | (y0 >> 3 & 0b00_000_111);
      var bitmapOffset = newY << 5;
      var colorInfoOffset = 0x1800 + y0 / 8 * 32;
      bool flash = (frameCount & 16) != 0; // bit 4 is toggled every 16 frames

      var bitmap = mem.ReadScreen((ushort)bitmapOffset, 32);
      var attrib = mem.ReadScreen((ushort)colorInfoOffset, 32);
      this.leakyPort.SetVideoData(bitmap[0]);

      for (var chx = 0; chx < 32; chx++)
      {
        var bits = bitmap[chx];
        var color = attrib[chx];
        var src = lookup.GetPixels(bits, color, flash);
        Array.Copy(src, 0, this.currentVideoFrame, offset + chx*8, src.Length);
      }
    }
  }
}
