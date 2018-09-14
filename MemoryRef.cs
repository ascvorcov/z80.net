using System;

namespace z80emu
{
    // register-based reference to memory.
    class MemoryRef
    {
      private readonly WordRegister parent;
      private readonly ushort offset;

      public MemoryRef(WordRegister parent, ushort offset)
      {
        this.parent = parent;
        this.offset = offset;
      }

      public static MemoryRef Absolute(ushort address)
      {
        return new MemoryRef(null, address);
      }

      public ushort Value => (ushort)((this.parent?.Value ?? 0) + this.offset);

      public MemoryRef Next(ushort delta = 1)
      {
        return new MemoryRef(this.parent, (ushort)(this.offset + delta));
      }
    }
}