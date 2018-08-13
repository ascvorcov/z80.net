using System;

namespace z80emu
{
    class MemoryRef
    {
      public ushort Value { get; }

      public MemoryRef(ushort value)
      {
        this.Value = value;
      }

      public void Increment()
      {
        this.Value++;
      }
    }
}