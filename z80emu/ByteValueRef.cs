using System;

namespace z80emu
{
    class ByteValueRef
    {
      private readonly ByteRegister bytereg;

      public ByteValueRef(ByteRegister bytereg)
      {
        this.bytereg = bytereg;
      }

      public byte ByteValue => this.bytereg.Value;
    }
}