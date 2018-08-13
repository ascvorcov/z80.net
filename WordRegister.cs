using System;

namespace z80emu
{
    class AFRegister : WordRegister
    {
        public AFRegister()
        {
            this.High = ByteRegister.High(this);
            this.Low = new FlagsRegister(this);
        }

        public FlagsRegister F => (FlagsRegister)Low;
        public ByteRegister A => High;
    }

    class WordRegister
    {
        public ushort Value;

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

        public MemoryRef MemRef() => new MemoryRef(ref this.Value);

        public ByteRegister High {get; protected set;}

        public ByteRegister Low {get; protected set;}

        public ushort Increment()
        {
            ushort old = Value;
            this.Value++;
            return old;
        }

        public ushort Decrement()
        {
            ushort old = Value;
            this.Value--;
            return old;
        }

        public void Dump(string name)
        {
            Console.Write($"{name}={this.Value:X} ");
        }
    }
}