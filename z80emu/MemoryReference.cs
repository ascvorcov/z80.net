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
    private readonly Func<Memory,word> offset;
    public MemoryReference(Func<Memory, word> offset)
    {
      this.offset = offset;
    }
    
    public bool IsRegister => false;

    public word GetOffset(Memory m) => this.offset(m);

    IReference<word> IPointerReference<word>.Get()
    {
      return new MemoryReference(m => m.ReadWord(this.offset(m)));
    }

    IReference<byte> IPointerReference<byte>.Get()
    {
      return new MemoryReference(m => m.ReadWord(this.offset(m)));
    }

    byte IReference<byte>.Read(Memory m)
    {
      return m.ReadByte(this.offset(m));
    }

    word IReference<word>.Read(Memory m)
    {
      return m.ReadWord(this.offset(m));
    }

    void IReference<byte>.Write(Memory m, byte value)
    {
      m.WriteByte(this.offset(m), value);
    }

    void IReference<word>.Write(Memory m, word value)
    {
      m.WriteWord(this.offset(m), value);
    }
  }
}