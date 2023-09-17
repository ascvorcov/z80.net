namespace z80emu.Loader
{
    using System;
    class Z80Format
    {
        private readonly byte[] data;
        private readonly IComputer computer;

        public Z80Format(byte[] data)
        {
            this.data = data;
            this.computer = CreateComputer(data);
        }

        public IComputer LoadZ80()
        {
            var cpu = this.computer.CPU;
            var ula = this.computer.ULA;
            cpu.regAF.A.Value = data[0];
            cpu.regAF.F.Value = data[1];
            cpu.Registers.BC.Value = Word(data, 2);
            cpu.Registers.HL.Value = Word(data, 4);
            cpu.regPC.Value = Word(data, 6);
            cpu.regSP.Value = Word(data, 8);
            cpu.regI.Value = data[10];
            
            var bit = new BitInfo1(data[12]);
            cpu.regR.Value = (byte)((data[11] & 127) | bit.Bit7);
            ula.BorderColor = bit.BorderColor;
            cpu.Registers.DE.Value = Word(data, 13);
            cpu.RegistersCopy.BC.Value = Word(data, 15);
            cpu.RegistersCopy.DE.Value = Word(data, 17);
            cpu.RegistersCopy.HL.Value = Word(data, 19);
            cpu.regAFx.A.Value = data[21];
            cpu.regAFx.F.Value = data[22];
            cpu.regIY.Value = Word(data, 23);
            cpu.regIX.Value = Word(data, 25);
            cpu.IFF1 = data[27] != 0;
            cpu.IFF2 = data[28] != 0;
            var bit2 = new BitInfo2(data[29]);
            cpu.InterruptMode = bit2.InterruptMode;

            if (cpu.regPC.Value == 0)
            {
                // v2 format
                ReadV2Format(data);
            }
            else
            {
                UnpackMem(0x4000, data, 30, data.Length, bit.Compressed);
            }

            return this.computer;
        }

        private static IComputer CreateComputer(byte[] data)
        {
            if (Word(data, 6) == 0)
            {
                if (data[34] >= 3)
                    return new Spectrum128K();
            }
            return new Spectrum48K();
        }

        private void ReadV2Format(byte[] data)
        {
            var len = Word(data, 30);
            this.computer.CPU.regPC.Value = Word(data, 32);
            int i = 32 + len;
            var hwMode = data[34];
            bool use128k = hwMode >= 3;

            while (i != data.Length)
            {
                var datalen = Word(data, i);
                var page = use128k ? SetBank(data[i+2]) : GetPage(data[i + 2]);

                i = i + 3; // skip block header

                bool compressed = true;
                if (datalen == 0xFFFF)
                {
                    datalen = 16384;
                    compressed = false;
                }

                UnpackMem(page, data, i, i + datalen, compressed);
                
                i += datalen;
            }
            if (use128k)
                SetBank(3);
        }

        private ushort SetBank(byte bank)
        {
            if (bank < 3 || bank > 11)
                throw new NotSupportedException();
            var ext = this.computer.Memory as MemoryExtended;
            ext.SetBank((byte)(bank - 3));

            return 0xc000;
        }

        private ushort GetPage(byte page)
        {
            switch(page)
            {
                case 0: return 0; // rom
                case 4: return 0x8000;
                case 5: return 0xc000;
                case 8: return 0x4000;
                default: throw new Exception($"page type {page} not supported on 48k");
            }
        }

        private ushort UnpackMem(ushort offset, byte[] data, int start, int end, bool compressed)
        {
            var memory = this.computer.Memory;
            for(int i = start; i < end; ++i)
            {
                if (compressed && 
                    data[i+0] == 0x00 && 
                    data[i+1] == 0xED && 
                    data[i+2] == 0xED && 
                    data[i+3] == 0x00)
                    break;

                if (data[i] == 0xED && data[i+1] == 0xED && compressed)
                {
                    var repeat = data[i+2];
                    var value = data[i+3];
                    while(repeat-->0)
                    {
                        memory.WriteByte(offset++, value);
                    }

                    i = i + 3;
                }
                else
                {
                    memory.WriteByte(offset++, data[i]);
                }
            }
            return offset;
        }

        private static ushort Word(byte[] data, int offset)
        {
            return (ushort)(data[offset] | (data[offset+1]<<8));
        }
    }
}