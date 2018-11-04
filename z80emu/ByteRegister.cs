using System;

namespace z80emu
{
    class ByteRegister : IReference<byte>
    {
      private readonly bool high;
      private readonly WordRegister parent;
      protected ByteRegister(WordRegister parent, bool high)
      {
        this.parent = parent;
        this.high = high;
      }

      public static ByteRegister High(WordRegister reg) => new ByteRegister(reg, true);
      public static ByteRegister Low(WordRegister reg) => new ByteRegister(reg, false);

      public bool IsRegister => true;
      
      public byte Value 
      {
        get 
        {
          return this.high ? (byte)(this.parent.Value >> 8) : ((byte)this.parent.Value.And(0x00FF));
        }
        set
        {
          ushort uvalue = value;
          if (this.high)
          {
            ushort t = this.parent.Value.And(0x00FF);
            this.parent.Value = t.Or((ushort)(uvalue << 8));
          }
          else
          {
            ushort t = this.parent.Value.And(0xFF00);
            this.parent.Value = t.Or(uvalue);
          }
        }
      }

      public byte Increment()
      {
        var old = this.Value;
        this.Value++;
        return old;
      }

      public byte Decrement()
      {
        var old = this.Value;
        this.Value--;
        return old;
      }

      public byte Read(Memory m)
      {
        return this.Value;
      }

      public void Write(Memory m, byte value)
      {
        this.Value = value;
      }
  }
}