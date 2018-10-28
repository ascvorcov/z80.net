namespace z80emu
{
  using System;

  using word = System.UInt16;

  class MemoryReference : 
    IReference<word>,
    IReference<byte>,
    IPointerReference<word>,
    IPointerReference<byte>
  {
    private readonly Func<word> offset;
    public MemoryReference(Func<word> offset)
    {
      this.offset = offset;
    }
    
    public bool IsRegister => false;

    IReference<word> IPointerReference<word>.Get(Memory m)
    {
      return new MemoryReference(() => m.ReadWord(this.offset()));
    }

    IReference<byte> IPointerReference<byte>.Get(Memory m)
    {
      return new MemoryReference(() => m.ReadWord(this.offset()));
    }

    byte IReference<byte>.Read(Memory m)
    {
      return m.ReadByte(this.offset());
    }

    word IReference<word>.Read(Memory m)
    {
      return m.ReadWord(this.offset());
    }

    void IReference<byte>.Write(Memory m, byte value)
    {
      m.WriteByte(this.offset(), value);
    }

    void IReference<word>.Write(Memory m, word value)
    {
      m.WriteWord(this.offset(), value);
    }
  }
}