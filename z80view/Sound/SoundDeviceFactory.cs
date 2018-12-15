using System.Runtime.InteropServices;

namespace z80view.Sound
{
    static class SoundDeviceFactory
    {
        public static ISoundDevice Create(uint soundFrameSize)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateWindowsSoundDevice(soundFrameSize);
            }
            
            return new NullSoundDevice();
        }

        private static ISoundDevice CreateWindowsSoundDevice(uint soundFrameSize)
        {
            return new SoundDeviceWin32(soundFrameSize);
        }
    }
}
