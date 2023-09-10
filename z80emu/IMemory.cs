using System;
using Word16 = System.UInt16;

namespace z80emu
{
    public interface IMemory
    {
        void Dump();
        byte ReadByte(Word16 offset);
        void WriteByte(Word16 offset, byte data);
        ReadOnlySpan<byte> ReadScreen(Word16 offset, Word16 size);
    }

    public static class MemoryExtensions
    {
        public static Word16 ReadWord(this IMemory memory, Word16 offset)
        {
            Word16 lo = memory.ReadByte(offset++);
            Word16 hi = memory.ReadByte(offset);
            hi <<= 8;
            return hi.Or(lo);
        }

        public static void WriteWord(this IMemory memory, Word16 offset, Word16 word)
        {
            memory.WriteByte(offset++, (byte)word.And(0xFF));
            memory.WriteByte(offset, (byte)(word >> 8));
        }        
    }
}