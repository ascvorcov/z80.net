using System;
using System.Runtime.CompilerServices;
using Word16 = System.UInt16;

namespace z80emu
{
    internal class WordRegister : IReference<Word16>
    {
        private WordStorage storage;

        public ref Word16 Value => ref storage.value;

        public WordRegister()
        {
            this.High = new ByteRegister(new HighStorage(this));
            this.Low = new ByteRegister(new LowStorage(this));
        }

        public bool IsRegister => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Word16 Read(Memory m)
        {
            return this.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Memory m, Word16 value)
        {
            this.Value = value;
        }

        public ByteRegister High {get; protected set;}

        public ByteRegister Low {get; protected set;}

        public IPointerReference<byte> AsBytePtr(Word16 offset = 0)
        {
            return new MemoryReference(m => (Word16)(this.Value + offset));
        }

        public IPointerReference<Word16> AsWordPtr(Word16 offset = 0)
        {
            return new MemoryReference(m => (Word16)(this.Value + offset));
        }

        public IReference<byte> ByteRef(Word16 offset = 0)
        {
            return new MemoryReference(m => (Word16)(this.Value + offset));
        }

        public IReference<Word16> WordRef(Word16 offset = 0)
        {
            return new MemoryReference(m => (Word16)(this.Value + offset));
        }

        public IReference<byte> ByteRef(IReference<byte> offset)
        {
            return new MemoryReference(m => 
            {
                Word16 ret = this.Value;
                sbyte off = (sbyte)offset.Read(m); // offset is signed
                return (Word16)(ret + off);
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Increment() => this.Value++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Decrement() => this.Value--;

        public void Dump(string name)
        {
            Console.Write($"{name}={this.Value:X} ");
        }

        internal class HighStorage : IByteStorage
        {
            private readonly WordRegister parent;
            public HighStorage(WordRegister parent) => this.parent = parent;
            public ref byte value => ref this.parent.storage.high;
        }

        internal class LowStorage : IByteStorage
        {
            private readonly WordRegister parent;
            public LowStorage(WordRegister parent) => this.parent = parent;
            public ref byte value => ref this.parent.storage.low;
        }        
  }
}