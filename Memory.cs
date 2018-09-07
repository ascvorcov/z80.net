using System;

namespace z80emu
{
    class Memory
    {
      private readonly byte[] memory;

      public Memory()
      {
        this.memory = new byte[0x10000];
      }

      public Memory(params byte[] input)
      {
        this.memory = input;
      }

      public byte ReadByte(MemoryRef offset)
      {
        var idx = offset.Value;
        return this.memory[idx];
      }

      public void WriteByte(MemoryRef offset, byte data)
      {
        var idx = offset.Value;
        if (idx < 0x4000) throw new Exception("Attempt to write into ROM");

        this.memory[idx] = data;
      }

      public ushort ReadWord(MemoryRef offset)
      {
        ushort lo = ReadByte(offset);
        ushort hi = ReadByte(offset.Next());
        hi <<= 8;
        return hi.Or(lo);
      }

      public void WriteWord(MemoryRef offset, ushort word)
      {
        WriteByte(offset, (byte)word.And(0xFF));
        WriteByte(offset.Next(), (byte)(word >> 8));
      }
    }
}