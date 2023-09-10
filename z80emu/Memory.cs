using System;
using Word16 = System.UInt16;

namespace z80emu
{
    class Memory : IMemory
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

        public byte ReadByte(Word16 offset)
        {
            return this.memory[offset];
        }

        public void WriteByte(Word16 offset, byte data)
        {
            var idx = offset;

            if (idx < 0x4000) 
              return;

            this.memory[idx] = data;
        }


        public ReadOnlySpan<byte> ReadScreen(Word16 offset, Word16 size)
        {
            return this.memory.AsSpan(0x4000 + offset, size);
        }
    }
}