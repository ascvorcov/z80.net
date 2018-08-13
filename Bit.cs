using System;

namespace z80emu
{
    static class Bit
    {
        public static ushort And(this ushort target, ushort mask)
        {
            uint t = target;
            uint m = mask;
            return (ushort)(t & m);
        }

        public static ushort Or(this ushort target, ushort mask)
        {
            uint t = target;
            uint m = mask;
            return (ushort)(t | m);
        }
    }
}
