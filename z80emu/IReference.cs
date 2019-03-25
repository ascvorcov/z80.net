namespace z80emu
{
  internal interface IReference<T>
  {
    bool IsRegister {get;}
    T Read(Memory m);
    void Write(Memory m, T value);
  }
}