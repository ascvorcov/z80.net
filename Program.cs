using System;

namespace z80emu
{
    class App
    {
        static void Main()
        {
            var cpu = new CPU();
            var mem = new Memory(new byte[] { 1,2,3,4,5,6,7,8,9,0 });

            cpu.Run(mem);
        }
    }
}