namespace z80emu
{
    interface IDevice
    {
      byte Read();
      void Write(byte value);
    }

    class Port
    {
      private IDevice[] map = new IDevice[256];
      private Port() {}

      public void Bind(byte port, IDevice device)
      {
        this.map[port] = device;
      }

      public IDevice Get(byte port)
      {
        return this.map[port];
      }

      public static Port Create()
      {
        var ret = new Port();
        ret.Bind(0xFE, new ULA());
        return ret;
      }
    }
}
