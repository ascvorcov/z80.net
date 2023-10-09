namespace z80view.Sound
{
    public class SoundDeviceSet : ISoundDeviceSet
    {
        private ISoundDevice[] devices;
        public SoundDeviceSet(params ISoundDevice[] devices)
        {
            this.devices = devices;
        }

        public void Dispose()
        {
            foreach (var device in this.devices)
            {
                device.Dispose();
            }
        }

        public bool Play(byte[] buffer, int channel)
        {
            return this.devices[channel].Play(buffer);
        }

        public void Reset()
        {
            foreach (var device in this.devices)
            {
                device.Reset();
            }
        }
    }
}
