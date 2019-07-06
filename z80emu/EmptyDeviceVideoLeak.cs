using System;

namespace z80emu
{
  class EmptyDeviceVideoLeak : IDevice
  {
    private byte portValue = 0xFF;

    public event EventHandler Interrupt = delegate{};
    
    public byte Read(byte highPart) => this.portValue;

    public void Write(byte highPart, byte value){}

    internal void SetVideoData(byte data )=> this.portValue = data;

    internal void Reset() => this.portValue = 0xFF;
  }
}
