namespace z80emu
{
  interface IPointerReference<T>
  {
    IReference<T> Get();
  }
}