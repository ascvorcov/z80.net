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

      public ushort Value => (ushort)(this.parent.Value + this.offset);

      public MemoryRef Next(ushort delta = 1)
      {
        return new MemoryRef(this.parent, (ushort)(this.offset + delta));
      }
    }
}