namespace z80view.Sound
{
    using System;

    public interface ISoundDevice : IDisposable
    {
        void Reset();
        bool Play(byte[] buffer);
    }

    public interface ISoundDeviceSet : IDisposable
    {
        void Reset();
        bool Play(byte[] buffer, int channel);
    }
}
