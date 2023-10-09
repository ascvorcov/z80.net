using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using z80emu;

namespace z80view.Sound
{
    static class SoundDeviceFactory
    {
        public static ISoundDeviceSet Create(Emulator emulator)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var devices = Enumerable
                    .Repeat(emulator, emulator.SoundChannelsCount)
                    .Select(CreateWindowsSoundDevice)
                    .ToArray();
                return new SoundDeviceSet(devices);
            }
            
            return new SoundDeviceSet();
        }

        private static ISoundDevice CreateWindowsSoundDevice(Emulator emulator)
        {
            return new SoundDeviceProxy(() => new SoundDeviceWin32(
                emulator.SoundFrameSize,
                emulator.SoundSamplesPerSec));
        }
    }
}
