namespace z80emu
{
    using System;
    using System.IO;
    using System.Reflection;

    public static class Emulator
    {
        public static void Run()
        {
            var rom = LoadROM();
            Array.Resize(ref rom, 0x10000);
            var mem = new Memory(rom);
            var cpu = new CPU();
            cpu.Run(mem);
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
}