using System.Runtime.InteropServices;
using z80emu;

namespace z80view.Sound
{
    static class SoundDeviceFactory
    {
        public static ISoundDevice Create(Emulator emulator)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateWindowsSoundDevice(emulator);
            }
            
            return new NullSoundDevice();
        }

        private static ISoundDevice CreateWindowsSoundDevice(Emulator emulator)
        {
            return new SoundDeviceProxy(() => new SoundDeviceWin32(emulator.SoundFrameSize, emulator.SoundSamplesPerSec));
        }
    }
}
