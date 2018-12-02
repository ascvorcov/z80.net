namespace z80emu
{
    using System;

    class Z80Float
    {
        private readonly byte exp;
        private readonly byte m1;
        private readonly byte m2;
        private readonly byte m3;
        private readonly byte m4;
        public Z80Float(byte exp, byte m1, byte m2, byte m3, byte m4)
        {
            this.exp = exp;
            this.m1 = m1;
            this.m2 = m2;
            this.m3 = m3;
            this.m4 = m4;
        }

        public float ToFloat()
        {
            if (exp == 0 && m1 == 0 && m2 == 0 && m3 == 0 && m4 == 0)
                return 0;

            if (exp == 0 && (m1 == 0 || m1 == 0xFF) && m4 == 0)
            {
                //int format
                return (m2 | m3 << 8) * (m1 == 0 ? 1.0f : -1.0f);
            }

            float ret = 0;
            int pow = 1;
            float sign = ((m1 & 0x80) == 1) ? -1 : 1;
            var m1Adjusted = m1 | 0x80;
            foreach (var m in new[] { m1Adjusted, m2, m3, m4 })
            {
                pow <<= 1;
                ret += (m & 0b10000000) != 0 ? 1.0f/pow : 0;
                pow <<= 1;
                ret += (m & 0b01000000) != 0 ? 1.0f/pow : 0;
                pow <<= 1;
                ret += (m & 0b00100000) != 0 ? 1.0f/pow : 0;
                pow <<= 1;
                ret += (m & 0b00010000) != 0 ? 1.0f/pow : 0;
                pow <<= 1;
                ret += (m & 0b00001000) != 0 ? 1.0f/pow : 0;
                pow <<= 1;
                ret += (m & 0b00000100) != 0 ? 1.0f/pow : 0;
                pow <<= 1;
                ret += (m & 0b00000010) != 0 ? 1.0f/pow : 0;
                pow <<= 1;
                ret += (m & 0b00000001) != 0 ? 1.0f/pow : 0;
            }
            return sign * ret * (MathF.Pow(2, exp - 128));
        }
    }
}
