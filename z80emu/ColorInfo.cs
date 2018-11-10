namespace z80emu
{
  struct ColorInfo
  {
    private byte value;
    public ColorInfo(byte value)
    {
      this.value = value;
    }

    public bool Flash => (this.value & 0x80) != 0;
    public bool Bright => (this.value & 0x40) != 0;
    public int Paper => (this.value >> 3) & 7;
    public int Ink => this.value & 7;
  }
}
