using System;

namespace z80emu
{
    class MemoryRef
    {
      private readonly WordRegister parent;

      public MemoryRef(WordRegister parent)
      {
        this.parent = parent;
      }

      public ushort Value => this.parent.Value;

      public ushort Increment()
      {
        return this.parent.Increment();
      }
    }
}