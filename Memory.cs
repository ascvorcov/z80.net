using System;

namespace z80emu
{
    class Memory
    {
      private readonly byte[] memory = new byte[0x10000];

      public Memory()
      {}

      public Memory(params byte[] input)
      {
        this.memory = input;
      }

      public byte ReadByte(MemoryRef offset)
      {
        return this.memory[offset.Increment()];
      }

      public void WriteByte(MemoryRef offset, byte data)
      {
        if (offset.Value < 0x4000) throw new Exception("Attempt to write into ROM");

        this.memory[offset.Increment()] = data;
      }

      public ushort ReadWord(MemoryRef offset)
      {
        ushort lo = ReadByte(offset);
        ushort hi = ReadByte(offset);
        hi <<= 8;
        return hi.Or(lo);
      }

      public void WriteWord(MemoryRef offset, ushort word)
      {
        WriteByte(offset, (byte)word.And(0xFF));
        WriteByte(offset, (byte)(word >> 8));
      }
    }
}