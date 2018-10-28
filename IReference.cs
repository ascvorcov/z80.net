namespace z80emu
{
  interface IReference<T>
  {
    bool IsRegister {get;}
    T Read(Memory m);
    void Write(Memory m, T value);
  }
}