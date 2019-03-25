using System;
using System.Runtime.CompilerServices;

namespace z80emu
{
  [Flags]
  enum F : byte
  {
    Sign = 1 << 7,
    Zero = 1 << 6,
    Flag5 = 1 << 5,
    HalfCarry = 1 << 4,
    Flag3 = 1 << 3,
    ParityOverflow = 1 << 2,
    AddSub = 1 << 1,
    Carry = 1 << 0
  }

  class FlagsRegister : ByteRegister
  {
    public FlagsRegister(IByteStorage storage) : base(storage) {}

    public bool Sign // flag S
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(7);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(7, value);
    }

    public bool Zero // flag Z 
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(6);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(6, value);
    }

    public bool Flag5
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(5);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(5, value);
    }

    public bool HalfCarry // flag H
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(4);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(4, value);
    }
    
    public bool Flag3
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(3);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(3, value);
    }

    public bool ParityOverflow // flag P/V
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(2);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(2, value);
    }

    public bool Parity // flag P/V
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(2);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(2, value);
    }

    public bool AddSub // flag N
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(1);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(1, value);
    }

    public bool Carry // flag C
    {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => Get(0);
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      set => Set(0, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUndocumentedFlags(byte value)
    {
      this.Flag3 = (value & 0b001000) != 0;
      this.Flag5 = (value & 0b100000) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlags(bool sign, bool zero, bool halfCarry, bool overflow, bool addsub, byte undocumented)
    {
      // no carry in this version, common scenario
      int result = undocumented & 0b101000; // keep only bits 3 and 5

      result |= sign ? 128 : 0; //7
      result |= zero ? 64 : 0; //6
      result |= halfCarry ? 16 : 0; //4
      result |= overflow ? 4 : 0; //2
      result |= addsub ? 2 : 0; //1
      result |= this.Value & 1;//0, keep carry

      this.Value = (byte)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlags(bool sign, bool zero, bool halfCarry, bool overflow, bool addsub, bool carry, byte undocumented)
    {
      // all flags set
      int result = undocumented & 0b101000; // keep only bits 3 and 5

      result |= sign ? 128 : 0; //7
      result |= zero ? 64 : 0; //6
      result |= halfCarry ? 16 : 0; //4
      result |= overflow ? 4 : 0; //2
      result |= addsub ? 2 : 0; //1
      result |= carry ? 1 : 0;//0

      this.Value = (byte)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlags(bool halfCarry, bool addsub, bool carry, byte undocumented)
    {
      int result = (this.Value & 0b11000100) | (undocumented & 0b101000);
      result |= halfCarry ? 16 : 0; //4
      result |= addsub ? 2 : 0; //1
      result |= carry ? 1 : 0;//0

      this.Value = (byte)result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool Get(byte bit)
    {
      byte f = 1; f <<= bit;
      var v = this.Value;
      v &= f;
      return v != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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