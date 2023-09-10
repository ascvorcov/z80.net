namespace z80emu
{
  using System;

  using Word16 = System.UInt16;

  class MemoryReference : 
    IReference<Word16>,
    IReference<byte>,
    IPointerReference<Word16>,
    IPointerReference<byte>
  {
    private readonly Func<Memory, Word16> offset;
    public MemoryReference(Func<Memory, Word16> offset)
    {
      this.offset = offset;
    }
    
    public bool IsRegister => false;

    public Word16 GetOffset(Memory m) => this.offset(m);

    IReference<Word16> IPointerReference<Word16>.Get() => new MemoryReference(ReadWord); // allocation

    IReference<byte> IPointerReference<byte>.Get() => new MemoryReference(ReadWord); // allocation

    byte IReference<byte>.Read(Memory m) => ReadByte(m);

    Word16 IReference<Word16>.Read(Memory m) => ReadWord(m);

    void IReference<byte>.Write(Memory m, byte value) => m.WriteByte(this.offset(m), value);

    void IReference<Word16>.Write(Memory m, Word16 value) => m.WriteWord(this.offset(m), value);

    private byte ReadByte(Memory m) => m.ReadByte(this.offset(m));
    private Word16 ReadWord(Memory m) => m.ReadWord(this.offset(m));
  }
}