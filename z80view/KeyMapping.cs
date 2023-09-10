using System.Collections.Generic;
using z80emu;

namespace z80view
{
    public class KeyMapping
    {
        Dictionary<Avalonia.Input.Key, z80emu.Key> keys = new Dictionary<Avalonia.Input.Key, Key>
        {
            [Avalonia.Input.Key.Enter] = Key.Enter,
            [Avalonia.Input.Key.Space] = Key.Space,
            [Avalonia.Input.Key.LeftShift] = Key.Shift,
            [Avalonia.Input.Key.LeftCtrl] = Key.Sym,

            [Avalonia.Input.Key.D0] = Key.D0,
            [Avalonia.Input.Key.D1] = Key.D1,
            [Avalonia.Input.Key.D2] = Key.D2,
            [Avalonia.Input.Key.D3] = Key.D3,
            [Avalonia.Input.Key.D4] = Key.D4,
            [Avalonia.Input.Key.D5] = Key.D5,
            [Avalonia.Input.Key.D6] = Key.D6,
            [Avalonia.Input.Key.D7] = Key.D7,
            [Avalonia.Input.Key.D8] = Key.D8,
            [Avalonia.Input.Key.D9] = Key.D9,

            [Avalonia.Input.Key.A] = Key.A,
            [Avalonia.Input.Key.B] = Key.B,
            [Avalonia.Input.Key.C] = Key.C,
            [Avalonia.Input.Key.D] = Key.D,
            [Avalonia.Input.Key.E] = Key.E,
            [Avalonia.Input.Key.F] = Key.F,
            [Avalonia.Input.Key.G] = Key.G,
            [Avalonia.Input.Key.H] = Key.H,
            [Avalonia.Input.Key.I] = Key.I,
            [Avalonia.Input.Key.J] = Key.J,
            [Avalonia.Input.Key.K] = Key.K,
            [Avalonia.Input.Key.L] = Key.L,
            [Avalonia.Input.Key.M] = Key.M,
            [Avalonia.Input.Key.N] = Key.N,
            [Avalonia.Input.Key.O] = Key.O,
            [Avalonia.Input.Key.P] = Key.P,
            [Avalonia.Input.Key.Q] = Key.Q,
            [Avalonia.Input.Key.R] = Key.R,
            [Avalonia.Input.Key.S] = Key.S,
            [Avalonia.Input.Key.T] = Key.T,
            [Avalonia.Input.Key.U] = Key.U,
            [Avalonia.Input.Key.V] = Key.V,
            [Avalonia.Input.Key.W] = Key.W,
            [Avalonia.Input.Key.X] = Key.X,
            [Avalonia.Input.Key.Y] = Key.Y,
            [Avalonia.Input.Key.Z] = Key.Z,

        };

        public Key[] Map(Avalonia.Input.KeyEventArgs args)
        {
            if (args.Key == Avalonia.Input.Key.Back)
            {
                return new[] { Key.Shift, Key.D0 };
            }
            if (args.Key == Avalonia.Input.Key.Up)
            {
                return new[] { Key.Shift, Key.D7 };
            }
            if (args.Key == Avalonia.Input.Key.Down)
            {
                return new[] { Key.Shift, Key.D6 };
            }
            if (args.Key == Avalonia.Input.Key.Left)
            {
                return new[] { Key.Shift, Key.D5 };
            }
            if (args.Key == Avalonia.Input.Key.Right)
            {
                return new[] { Key.Shift, Key.D8 };
            }
            return new[] { this.keys.TryGetValue(args.Key, out var k) ? k : Key.None };
        }
    }
}
