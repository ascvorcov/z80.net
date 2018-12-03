namespace z80emu.Loader
{
    using System.IO;

    class Resource
    {
        public static byte[] Load(string resource)
        {
            var assembly = typeof(Emulator).Assembly;
            var resourceStream = assembly.GetManifestResourceStream(resource);
            using (var ms = new MemoryStream())
            {
                resourceStream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}