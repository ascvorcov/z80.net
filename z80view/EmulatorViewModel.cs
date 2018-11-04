using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using z80emu;

namespace z80view
{
    public class EmulatorViewModel
    {
        private readonly Random _rnd = new Random();
        
        private readonly Action _invalidate;

        public EmulatorViewModel(Action invalidate)
        {
            _invalidate = invalidate;

            // Bgra8888 is device-native and much faster.
            Bitmap = new WritableBitmap(640, 480, PixelFormat.Bgra8888);
            Task.Run(() => MoveFlakes());
            Task.Run(() => Emulator.Run());
        }

        public WritableBitmap Bitmap { get; }

        private unsafe void MoveFlakes()
        {
            while (true)
            {
                var bmp = Bitmap;
                using (var buf = bmp.Lock())
                {
                    var y = _rnd.Next(0,480);
                    var x = _rnd.Next(0,640);
                    var ptr = (uint*) buf.Address;
                    ptr[y*640+x] = uint.MaxValue;
                }

                _invalidate();

                Thread.Sleep(10);
            }
            // ReSharper disable once FunctionNeverReturns
        }
    }
}
