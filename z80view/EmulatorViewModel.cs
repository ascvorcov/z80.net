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
        private readonly Action invalidate;

        private readonly AutoResetEvent nextFrame = new AutoResetEvent(false);

        private readonly Thread drawingThread;

        private readonly Thread emulatorThread;

        private readonly Emulator emulator;

        private FrameEventArgs frame;

        public EmulatorViewModel(Action invalidate)
        {
            this.invalidate = invalidate;

            this.emulator = new Emulator();
            this.Bitmap = new WritableBitmap(352, 312, PixelFormat.Rgba8888);
            this.ResetCommand = new ActionCommand(Reset);

            this.emulatorThread = new Thread(RunEmulator);
            this.emulatorThread.Start();

            this.drawingThread = new Thread(DrawScreen);
            this.drawingThread.Start();
        }

        public ICommand ResetCommand { get; }

        public WritableBitmap Bitmap { get; }

        private void Reset()
        {
            this.emulator.Dump();
        }

        private void RunEmulator()
        {
            this.emulator.NextFrame += args =>
            {
                this.frame = args;
                this.nextFrame.Set();
            };

            this.emulator.Run();
        }

        private unsafe void DrawScreen()
        {
            while (true)
            {
                nextFrame.WaitOne(1000);
                if (frame == null)
                {
                    continue;
                }
                
                var bmp = Bitmap;
                using (var buf = bmp.Lock())
                {
                    var pal = frame.Palette;
                    var src = frame.Frame;
                    var dst = (uint*) buf.Address;
                    for (int i = 0; i < src.Length; ++i)
                    {
                        var c = pal[src[i]];
                        var rgba = (uint)(c.R << 16 | c.G << 8 | c.B) | 0xFF000000;
                        dst[i] = rgba;
                    }
                }

                invalidate();
            }
        }
    }
}
