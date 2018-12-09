namespace z80emu.Loader
{
    using System;
    using System.IO;

    class Load
    {
        public static (CPU,ULA,Memory) VanillaZ80Rom()
        {
            var rom = Resource.Load("z80emu.48.rom");
            Array.Resize(ref rom, 0x10000);
            var clk = new Clock();
            var mem = new Memory(rom);
            var cpu = new CPU(clk);
            var ula = new ULA(clk);
            cpu.Bind(0xFE, ula);

            return (cpu,ula,mem);
        }

        public static (CPU,ULA,Memory) Z80FormatImage(string imagePath)
        {
            var rom = Resource.Load("z80emu.48.rom");
            Array.Resize(ref rom, 0x10000);
            var clk = new Clock();
            var mem = new Memory(rom);
            var cpu = new CPU(clk);
            var ula = new ULA(clk);
            cpu.Bind(0xFE, ula);

            var format = new Z80Format(cpu, ula, mem);
            format.LoadZ80(File.ReadAllBytes(imagePath));

            return (cpu,ula,mem);
        }
    }
}