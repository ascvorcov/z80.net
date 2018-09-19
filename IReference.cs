namespace z80emu
{
  interface IReference<T>
  {
    T Read(Memory m);
    void Write(Memory m, T value);
  }
}