using System;

namespace z80emu
{
    class EmptyDevice : IDevice
    {
        public event EventHandler Interrupt = delegate{};

        public byte Read(byte highPart)
        {
          return 0xFF;
        }

        public void Write(byte highPart, byte value)
        {
        }
    }
}
