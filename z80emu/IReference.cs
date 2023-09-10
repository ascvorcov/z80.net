namespace z80emu
{
  internal interface IReference<T>
  {
    bool IsRegister {get;}
    T Read(IMemory m);
    void Write(IMemory m, T value);
  }
}