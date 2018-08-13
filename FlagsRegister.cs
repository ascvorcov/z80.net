using System;

namespace z80emu
{
  class FlagsRegister : ByteRegister
  {
    public FlagsRegister(WordRegister parent) : base(parent, false) {}

    public bool Sign // flag S
    {
      get => Get(7); set => Set(7, value);
    }

    public bool Zero // flag Z 
    {
      get => Get(6); set => Set(6, value);
    }

    public bool Flag5
    {
      get => Get(5); set => Set(5, value);
    }

    public bool HalfCarry // flag H
    {
      get => Get(4); set => Set(4, value);
    }
    
    public bool Flag3
    {
      get => Get(3); set => Set(3, value);
    }

    public bool ParityOverflow // flag P/V
    {
      get => Get(2); set => Set(2, value);
    }

    public bool AddSub // flag N
    {
      get => Get(1); set => Set(1, value);
    }

    public bool Carry // flag C
    {
      get => Get(0); set => Set(0, value);
    }

    bool Get(byte bit)
    {
      byte f = 1; f <<= bit;
      var v = this.Value;
      v &= f;
      return v != 0;
    }

    void Set(byte bit, bool v)
    {
      if (v)
      {
        byte f = 1; f <<= bit;
        this.Value |= f;
      }
      else
      {
        byte f = 1; f <<= bit;
        this.Value &= (byte)(~f);
      }
    }
  }
}