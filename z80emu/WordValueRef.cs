using System;

namespace z80emu
{
    class WordValueRef
    {
      private readonly WordRegister wordreg;

      public WordValueRef(WordRegister wordreg)
      {
        this.wordreg = wordreg;
      }

      public ushort WordValue => this.wordreg.Value;
    }
}