using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace z80view.Sound
{
    public delegate void WaveCallback(IntPtr device, int uMsg, IntPtr dwUser, IntPtr dwParam1, IntPtr dwParam2);

    //https://learn.microsoft.com/en-us/windows-hardware/drivers/audio/extensible-wave-format-descriptors
    public class Win32API
    {
        [DllImport("winmm.dll")]
        public static extern int waveOutOpen(
            out IntPtr hWaveOut,
            int uDeviceID,
            WAVEFORMATEX lpFormat,
            IntPtr dwCallback,
            IntPtr dwInstance,
            int dwFlags);

        [DllImport("winmm.dll")]
        public static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        public static extern int waveOutRestart(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll")]
        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll")]
        public static extern int waveOutWrite(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, uint uSize);
        
        [DllImport("winmm.dll")]
        public static extern int waveOutSetPlaybackRate(IntPtr hWaveOut, uint dwRate);

        [DllImport("winmm.dll")]
        public static extern int waveOutClose(IntPtr hWaveOut);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEHDR
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr reserved1;
        public int reserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    };

    public class SoundDeviceWin32 : ISoundDevice
    {
        private readonly WaveCallback callback;
        private readonly IntPtr waveHandle;
        private readonly Buffer[] buffers;
        private int currentBuffer = 0;
        private int countBusyBuffers = 0;
        const int BUFFERS = 10;

        public SoundDeviceWin32(uint soundFrameSize)
        {
            var fmt = new WAVEFORMATEX();
            fmt.cbSize = 0;
            fmt.wBitsPerSample = 8;
            fmt.nChannels = 1;
            fmt.nBlockAlign = (ushort)((fmt.nChannels * fmt.wBitsPerSample) / 8);
            fmt.nSamplesPerSec = 43750;
            fmt.nAvgBytesPerSec = fmt.nSamplesPerSec * fmt.nBlockAlign;
            fmt.wFormatTag = 1; // PCM

            const int FlagCallbackFunction = 0x00030000;

            this.callback = this.CallbackProc;
            var ptr = Marshal.GetFunctionPointerForDelegate(this.callback);

            CheckError(Win32API.waveOutOpen(
                out this.waveHandle, -1, fmt, ptr, IntPtr.Zero, FlagCallbackFunction));

            this.buffers = new Buffer[BUFFERS];
            for (uint i = 0; i < this.buffers.Length; ++i)
                this.buffers[i] = new Buffer(this.waveHandle, soundFrameSize, i);
        }

        public void Dispose()
        {
            Reset();
            CheckError(Win32API.waveOutClose(this.waveHandle));
        }

        public void Reset()
        {
            CheckError(Win32API.waveOutReset(this.waveHandle));
        }

        public bool Play(byte[] data)
        {
            bool played = false;
            this.currentBuffer++;
            if (this.currentBuffer == this.buffers.Length)
                this.currentBuffer = 0;

            var selectedBuffer = this.buffers[this.currentBuffer];
            if (selectedBuffer.IsAvailable())
            {
                Interlocked.Increment(ref this.countBusyBuffers);
                selectedBuffer.Play(data);
                played = true;
            }

            return played;
        }

        private void CallbackProc(IntPtr device, int uMsg, IntPtr dwUser, IntPtr dwParam1, IntPtr dwParam2)
        {
            const int WOM_DONE = 0x3BD;
            if (uMsg == WOM_DONE)
            {
                var hdr = Marshal.PtrToStructure<WAVEHDR>(dwParam1);
                var index = (int)hdr.dwUser;
                this.buffers[index].MarkAsAvailable();
                Interlocked.Decrement(ref this.countBusyBuffers);
            }
        }

        private static void CheckError(int err, [CallerMemberName] string caller = null)
        {
            if (err != 0 && err != 33)
                throw new Exception($"Error calling {caller} : {err}");
        }

        private class Buffer : IDisposable
        {
            static uint WAVEHDRsize = (uint)Marshal.SizeOf<WAVEHDR>();

            private readonly IntPtr handle;
            private WAVEHDR hdr;
            private bool available = true;

            public Buffer(IntPtr handle, uint size, uint userData)
            {
                this.handle = handle;
                this.hdr = new WAVEHDR();
                this.hdr.dwBufferLength = size;
                this.hdr.dwUser = (IntPtr)userData;
                this.hdr.lpData = Marshal.AllocHGlobal((int)size);
                CheckError(Win32API.waveOutPrepareHeader(handle, ref this.hdr, WAVEHDRsize));
            }

            public void MarkAsAvailable()
            {
                this.available = true;
            }

            public bool IsAvailable()
            {
                return this.available; 
            }

            public void Play(byte[] data)
            {
                Marshal.Copy(data, 0, this.hdr.lpData, data.Length);
                CheckError(Win32API.waveOutWrite(this.handle, ref this.hdr, WAVEHDRsize));
                this.available = false;
            }

            public void Dispose()
            {
                CheckError(Win32API.waveOutUnprepareHeader(handle, ref this.hdr, WAVEHDRsize));
                Marshal.FreeHGlobal(this.hdr.lpData);
            }
        }
    }

}
