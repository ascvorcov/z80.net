using System;

namespace z80emu
{
    class Memory
    {
      private readonly byte[] memory = new byte[0x10000];

      public Memory()
      {}

      public Memory(byte[] input)
      {
        Array.Copy(input, memory, Math.Min(input.Length, memory.Length));
      }

      public byte ReadByte(MemoryRef offset)
      {
        return this.memory[offset.Value++];
      }

      public void WriteByte(MemoryRef offset, byte data)
      {
        if (offset.Value < 0x4000) throw new Exception("Attempt to write into ROM");

        this.memory[offset.Value++] = data;
      }

      public ushort ReadWord(MemoryRef offset)
      {
        ushort ret = ReadByte(offset);
        ret <<= 8;
        return ret.Or(ReadByte(offset));
      }

      public void WriteWord(MemoryRef offset, ushort word)
      {
        WriteByte(offset, (byte)word.And(0xFF));
        WriteByte(offset, (byte)(word >> 8));
      }
    }
}