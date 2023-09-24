using System;
using System.Threading;
using z80emu;

namespace z80view.Sound
{
    public class SoundDeviceProxy : ISoundDevice
    {
        private readonly Func<ISoundDevice> factory;
        private readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private ISoundDevice activeDevice;
        public SoundDeviceProxy(Func<ISoundDevice> factory)
        {
            this.factory = factory;
            this.Reset();
        }

        public void Dispose()
        {
            this.rwlock.EnterWriteLock();
            this.activeDevice?.Dispose();
            this.rwlock.ExitWriteLock();
            this.rwlock.Dispose();
        }

        public bool Play(byte[] buffer)
        {
            try
            {
                this.rwlock.EnterReadLock();
                return this.activeDevice.Play(buffer);
            }
            finally
            {
                this.rwlock.ExitReadLock();
            }
        }

        public void Reset()
        {
            try
            {
                this.rwlock.EnterWriteLock();
                this.activeDevice?.Dispose();
                this.activeDevice = factory();
            }
            finally
            {
                this.rwlock.ExitWriteLock();
            }
        }
    }
}
