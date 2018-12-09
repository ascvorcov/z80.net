namespace z80emu
{
    class Clock
    {
        public long Ticks = 0;

        public void Tick(int t) => Ticks += t;
    }
}
