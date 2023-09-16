 namespace z80emu.Loader
{
    using System;
    using System.IO;

    class TapePlayer
    {
        // https://sinclair.wiki.zxnet.co.uk/wiki/Spectrum_tape_interface
        // https://k1.spdns.de/Develop/Projects/zxsp-osx/Info/tapeloader%20in%20rom.htm
        public class Cycle
        {
            public int pulse1;
            public int pulse2;
            public int repeat;

            public int OneCycleSize => this.pulse1 + this.pulse2;

            public bool IsHeaderOrData => this.pulse1 == 2168;
            public bool IsSync => this.pulse1 == 667;

            public static Cycle Header() => new Cycle { pulse1 = 2168, pulse2 = 2168, repeat = 8063/2 };
            public static Cycle Data()=> new Cycle { pulse1 = 2168, pulse2 = 2168, repeat = 3223/2 };
             public static Cycle Sync() => new Cycle { pulse1 = 667, pulse2 = 735, repeat = 1 };
             public static Cycle One() => new Cycle { pulse1 = 855, pulse2 = 855, repeat = 1 };
             public static Cycle Zero() => new Cycle { pulse1 = 1710, pulse2 = 1710, repeat = 1 };
        }

        private long cycle_start = -1;
        private long next_checkpoint = 0;
        private bool pulse_state = false;
        private int current_block;
        private int current_bit;
        private TAPFormat current_tape;
        private Cycle current_cycle; // cycle is two pulses
        private Clock clock;

        public TapePlayer(Clock clock)
        {
            this.clock = clock;            
        }

        public void Load(string tapFile)
        {
            using var stream = File.OpenRead(tapFile);
            this.current_tape = new TAPFormat(stream);
            this.current_cycle = Cycle.Header();
            this.current_block = 0;
            this.current_bit = 0;
            this.next_checkpoint = this.clock.Ticks + this.current_cycle.pulse1;
            this.cycle_start = this.clock.Ticks;
        }

        public bool Tick()
        {
            if (this.cycle_start == -1)
            {
                // not playing anything
                return this.pulse_state;
            }

            long ticks = this.clock.Ticks;
            if (ticks >= this.cycle_start + this.current_cycle.OneCycleSize)
            {
                // full cycle complete
                this.current_cycle.repeat--;
                this.cycle_start = this.cycle_start + this.current_cycle.OneCycleSize;
                this.next_checkpoint = this.cycle_start + this.current_cycle.pulse1;
                this.pulse_state = !this.pulse_state;
            }
            else if (ticks >= this.next_checkpoint)
            {
                this.next_checkpoint += this.current_cycle.pulse2;
                this.pulse_state = !this.pulse_state;
                // first half of cycle complete
            }

            if (this.current_cycle.repeat == 0)
            {
                // select next;
                this.cycle_start = this.next_checkpoint;
                if (this.current_cycle.IsHeaderOrData)
                {
                    this.current_cycle = Cycle.Sync();
                    this.next_checkpoint += this.current_cycle.pulse1;
                }
                else // sync, one or zero ended
                {
                    var block = this.current_tape.GetBlock(this.current_block);
                    if (block == null)
                    {
                        // no more blocks, reset tape
                        this.cycle_start = -1;
                    }
                    else if (this.current_bit >= block.SizeBits)
                    {
                        this.current_block++;
                        this.current_bit = 0;
                        var nextBlock = this.current_tape.GetBlock(this.current_block);
                        this.current_cycle = (nextBlock?.Header ?? false) ? Cycle.Header() : Cycle.Data();
                        this.cycle_start = nextBlock == null ? -1 : this.cycle_start;
                        this.next_checkpoint += this.current_cycle.pulse1;
                    }
                    else
                    {
                        bool bit = block.GetBit(this.current_bit++);
                        this.current_cycle = bit ? Cycle.One() : Cycle.Zero();
                        this.next_checkpoint += this.current_cycle.pulse1;
                    }
                }
            }

            return this.pulse_state;
        }
    }
}