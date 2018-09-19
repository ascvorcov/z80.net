using System;

using word = System.UInt16;

namespace z80emu
{
    class WordRegister : IReference<word>
    {
        public word Value;

        protected WordRegister(ByteRegister high, ByteRegister low)
        {
          this.High = high;
          this.Low = low;
        }

        public WordRegister()
        {
            this.High = ByteRegister.High(this);
            this.Low = ByteRegister.Low(this);
        }

        public word Read(Memory m)
        {
            return this.Value;
        }

        public void Write(Memory m, word value)
        {
            this.Value = value;
        }

        public ByteRegister High {get; protected set;}

        public ByteRegister Low {get; protected set;}

        public IPointerReference<byte> AsBytePtr(word offset = 0)
        {
            return new MemoryReference(() => (word)(this.Value + offset));
        }

        public IPointerReference<word> AsWordPtr(word offset = 0)
        {
            return new MemoryReference(() => (word)(this.Value + offset));
        }

        public word Increment()
        {
            word old = Value;
            this.Value++;
            return old;
        }

        public word Decrement()
        {
            word old = Value;
            this.Value--;
            return old;
        }

        public void Dump(string name)
        {
            Console.Write($"{name}={this.Value:X} ");
        }
  }
}