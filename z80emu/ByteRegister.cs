using System;
using System.Runtime.CompilerServices;

namespace z80emu
{
    class ByteRegister : IReference<byte>
    {
      private readonly IByteStorage storage;
      public ByteRegister(IByteStorage storage) => this.storage = storage;

      public bool IsRegister => true;
      
      public byte Value 
      {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.storage.value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public byte Read(IMemory m)
      {
        return this.storage.value;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public void Write(IMemory m, byte value)
      {
        this.storage.value = value;
      }
  }
}