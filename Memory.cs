using System;

namespace z80emu
{
    using word = System.UInt16;
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

      public void Dump()
      {
        System.IO.File.WriteAllBytes("mem.dump", this.memory);
      }

      public byte ReadByte(word offset)
      {
        return this.memory[offset];
      }

      public void WriteByte(word offset, byte data)
      {
        var idx = offset;
        if (idx < 0x4000) throw new Exception("Attempt to write into ROM");

        this.memory[idx] = data;
      }

      public word ReadWord(word offset)
      {
        word lo = ReadByte(offset++);
        word hi = ReadByte(offset);
        hi <<= 8;
        return hi.Or(lo);
      }

      public void WriteWord(word offset, word word)
      {
        WriteByte(offset++, (byte)word.And(0xFF));
        WriteByte(offset, (byte)(word >> 8));
      }
    }
}