namespace z80emu.Loader
{
    using System.IO;

    class Z80Rom
    {
        public static byte[] Load()
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