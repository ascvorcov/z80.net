using System.Runtime.InteropServices;

namespace z80emu
{
    internal interface IByteStorage
    {
        ref byte value {get;}
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    internal struct WordStorage
    {
        [FieldOffset(0)]
        public System.UInt16 value;
        [FieldOffset(0)]
        public byte low;
        [FieldOffset(1)]
        public byte high;
    }
}