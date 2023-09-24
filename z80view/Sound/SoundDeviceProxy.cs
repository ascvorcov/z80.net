using System;
using System.Threading;
using z80emu;

namespace z80view.Sound
{
    public class SoundDeviceProxy : ISoundDevice
    {
        private readonly Func<ISoundDevice> factory;
        private ISoundDevice activeDevice;
        public SoundDeviceProxy(Func<ISoundDevice> factory)
        {
            this.factory = factory;
            this.Reset();
        }

        public void Dispose()
        {
            this.activeDevice.Dispose();
        }

        public bool Play(byte[] buffer)
        {
            return this.activeDevice.Play(buffer);
        }

        public void Reset()
        {
            var newDevice = factory();
            var oldDevice = Interlocked.Exchange(ref this.activeDevice, newDevice);
            if (oldDevice != null)
            {
                oldDevice.Dispose();
            }
        }
    }
}
