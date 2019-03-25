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
    private readonly Func<Memory, word> offset;
    public MemoryReference(Func<Memory, word> offset)
    {
      this.offset = offset;
    }
    
    public bool IsRegister => false;

    public word GetOffset(Memory m) => this.offset(m);

    IReference<word> IPointerReference<word>.Get() => new MemoryReference(ReadWord); // allocation

    IReference<byte> IPointerReference<byte>.Get() => new MemoryReference(ReadWord); // allocation

    byte IReference<byte>.Read(Memory m) => ReadByte(m);

    word IReference<word>.Read(Memory m) => ReadWord(m);

    void IReference<byte>.Write(Memory m, byte value) => m.WriteByte(this.offset(m), value);

    void IReference<word>.Write(Memory m, word value) => m.WriteWord(this.offset(m), value);

    private byte ReadByte(Memory m) => m.ReadByte(this.offset(m));
    private word ReadWord(Memory m) => m.ReadWord(this.offset(m));
  }
}