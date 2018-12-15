namespace z80view.Sound
{
    class NullSoundDevice : ISoundDevice
    {
        public bool Play(byte[] data)
        {
            return true;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
        }
    }
}
