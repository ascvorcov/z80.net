namespace z80emu
{
  using System;
  using System.IO;

    class Loader
    {
        public (CPU,ULA,Memory) LoadRom()
        {
            var rom = LoadROM();
            Array.Resize(ref rom, 0x10000);
            var mem = new Memory(rom);
            var cpu = new CPU();
            var ula = new ULA();
            cpu.Bind(0xFE, ula);

            return (cpu,ula,mem);
        }

        public (CPU,ULA,Memory) LoadZ80(string z80romPath)
        {
            var (cpu,ula,memory) = LoadRom();
            var data = File.ReadAllBytes(z80romPath);

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

            ushort offset = 0x4000;
            for(int i = 30; i < data.Length; ++i)
            {
                if (data[i+0] == 0x00 && 
                    data[i+1] == 0xED && 
                    data[i+2] == 0xED && 
                    data[i+3] == 0x00 && 
                    bit.Compressed)
                    break;

                if (data[i] == 0xED && data[i+1] == 0xED && bit.Compressed)
                {
                    var repeat = data[i+2];
                    var value = data[i+3];
                    while(repeat-->0)
                    {
                        memory.WriteByte(offset++, value);
                    }

                    i = i+3;
                }
                else
                {
                    memory.WriteByte(offset++, data[i]);
                }
            }

            return (cpu,ula,memory);
        }

        private ushort Word(byte[] data, int offset)
        {
            return (ushort)(data[offset] | (data[offset+1]<<8));
        }

        private static byte[] LoadROM()
        {
            var assembly = typeof(Emulator).Assembly;
            var resourceStream = assembly.GetManifestResourceStream("z80emu.48.rom");
            using (var ms = new MemoryStream())
            {
                resourceStream.CopyTo(ms);
                return ms.ToArray();
            }
        }        
    }

    public struct BitInfo1
    {
        private byte data;
        public BitInfo1(byte data)
        {
          this.data = data == 255 ? (byte)1 : data;
        }

        // Bit 0  : Bit 7 of the R-register
        public byte Bit7 => (byte)(this.data << 7);

        // Bit 1-3: Border colour
        public byte BorderColor => (byte)((this.data >> 1)&7);

        // Bit 4  : 1=Basic SamRom switched in
        public bool SamRom => (this.data & 16) != 0;

        // Bit 5  : 1=Block of data is compressed
        public bool Compressed => (this.data & 32) != 0;

        // Bit 6-7: No meaning
    }

    public struct BitInfo2
    {
        private byte data;
        public BitInfo2(byte data)
        {
            this.data = data;
        }
        //Bit 0-1: Interrupt mode (0, 1 or 2)
        public int InterruptMode => this.data & 3;

        //Bit 2  : 1=Issue 2 emulation
        public bool Issue2 => (this.data & 4) != 0;

        //Bit 3  : 1=Double interrupt frequency
        public bool DoubleFreq => (this.data & 8) != 0;

        //Bit 4-5: 1=High video synchronisation
        //        3=Low video synchronisation
        //        0,2=Normal
        public int VideoSyncMode => (this.data >> 4) & 3;

        //Bit 6-7: 0=Cursor/Protek/AGF joystick
        //        1=Kempston joystick
        //        2=Sinclair 2 Left joystick (or user defined, for version 3 .z80 files)
        //        3=Sinclair 2 Right joystick
        public int Joystick => (this.data >> 6) & 3;
    }
}