 namespace z80emu.Loader
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;

    class TapePlayer
    {
        // https://worldofspectrum.org/faq/reference/48kreference.htm
        // https://sinclair.wiki.zxnet.co.uk/wiki/Spectrum_tape_interface
        // https://k1.spdns.de/Develop/Projects/zxsp-osx/Info/tapeloader%20in%20rom.htm
        // https://speccy.xyz/rom/asm/0556
        public class Cycle
        {
            private long start = 0;
            private int index = 0;
            private int[] durations;
            public enum State
            {
                NoChanges,
                PulseEnded,
                CycleEnded
            }

            public static Cycle Header() => new Cycle { durations = MakePilotone(true) };
            public static Cycle Data() => new Cycle { durations = MakePilotone(false) };
            public static Cycle Zero() => new Cycle { durations = new int[] { 855, 855 } };
            public static Cycle One() => new Cycle { durations = new int[] { 1710, 1710 } };
            public State Tick(long clock)
            {
                if (start == 0)
                {
                    start = clock;
                    index = 0;
                }
                else if (start + durations[index] <= clock)
                {
                    start += durations[index++];
                    if (index == durations.Length)
                    {
                        start = 0;
                        return State.CycleEnded;
                    }                    
                    return State.PulseEnded;
                }
                return State.NoChanges;
            }

            private static int[] MakePilotone(bool header)
            {
                var durations = new int[1 + (header ? 8063 : 3223) + 2];
                Array.Fill(durations, 2168);
                durations[0] = 200000; // pause
                durations[^2] = 667; // sync 1
                durations[^1] = 735; // sync 2
                return durations;
            }
        }

        private bool pulse_state = false;
        private int current_block = -1;
        private volatile IEnumerator<Cycle> current_block_data = null;
        private TAPFormat current_tape;
        private Clock clock;

        public TapePlayer(Clock clock)
        {
            this.clock = clock;            
        }

        public void Load(string tapFile)
        {
            using var stream = File.OpenRead(tapFile);
            this.current_tape = new TAPFormat(stream);
            this.TakeNextBlock();
        }

        public bool Tick()
        {
            if (this.current_block_data == null)
            {
                // not playing anything
                return this.pulse_state;
            }

            var cycle = this.current_block_data.Current;
            switch (cycle.Tick(this.clock.Ticks))
            {
                case Cycle.State.PulseEnded:
                    this.pulse_state = !this.pulse_state;
                    break;
                case Cycle.State.CycleEnded:
                    this.pulse_state = !this.pulse_state;
                    if (!this.current_block_data.MoveNext())
                    {
                        this.current_block_data.Dispose();
                        this.TakeNextBlock();
                    }
                    break;
            }

            return this.pulse_state;
        }

        private void TakeNextBlock()
        {
            var next_block = this.GetNextBlock();
            var block_data = next_block.GetEnumerator();
            if (!block_data.MoveNext())
            {
                this.current_block_data = null;
                return; // tape ended
            }
            var block = block_data.Current;
            if (block == null)
            {
                throw new Exception("unexpected cycle value");
            }

            // there is a parallel access to this enumerator, must be assigned last
            this.current_block_data = block_data;
        }

        private IEnumerable<Cycle> GetNextBlock()
        {
            var block = this.current_tape.GetBlock(++this.current_block);
            if (block == null)
            {
                yield break;
            }

            yield return block.Header ? Cycle.Header() : Cycle.Data();

            for (int b = 0; b < block.SizeBits; ++b)
            {
                yield return block.GetBit(b) ? Cycle.One() : Cycle.Zero();
            }
        }
    }
}