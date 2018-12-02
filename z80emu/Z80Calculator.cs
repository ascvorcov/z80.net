namespace z80emu
{
  using System;
  using System.Collections.Generic;

    // Can read and dump floating point numbers 
    // in calculator stack in spectrum basic calculator
    class Z80Calculator
    {
        private IEnumerable<Z80Float> DumpStack(Memory memory)
        {
            var stkend = memory.ReadWord(0x5C65); // stack first free
            var stkbot = memory.ReadWord(0x5C63); // stack bottom
            stkend-=5;
            for (;stkend != stkbot; stkend-=5)
            {
                yield return DumpFloat(stkend, memory);
            }
        }

        private Z80Float DumpFloat(ushort o, Memory memory)
        {
            var b1 = memory.ReadByte(o++);
            var b2 = memory.ReadByte(o++);
            var b3 = memory.ReadByte(o++);
            var b4 = memory.ReadByte(o++);
            var b5 = memory.ReadByte(o++);
            return new Z80Float(b1,b2,b3,b4,b5);
        }

    }
}
