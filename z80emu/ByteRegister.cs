using System;

namespace z80emu
{
    class ByteRegister : IReference<byte>
    {
      private readonly IByteStorage storage;
      public ByteRegister(IByteStorage storage) => this.storage = storage;

      public bool IsRegister => true;
      
      public byte Value 
      {
        get => this.storage.value;
        set => this.storage.value = value;
      }

      public byte Increment()
      {
        var old = this.storage.value;
        this.storage.value = (byte)(old + 1);
        return old;
      }

      public byte Decrement()
      {
        var old = this.storage.value;
        this.storage.value = (byte)(old - 1);
        return old;
      }

      public byte Read(Memory m)
      {
        return this.storage.value;
      }

      public void Write(Memory m, byte value)
      {
        this.storage.value = value;
      }
  }
}