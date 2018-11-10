using System;

namespace z80emu
{
  [Flags]
  public enum Key
  {
    None = 0,

    // row 0
    Shift = 0x001,
    Z     = 0x002,
    X     = 0x004,
    C     = 0x008,
    V     = 0x010,

    // row 1
    A     = 0x101,
    S     = 0x102,
    D     = 0x104,
    F     = 0x108,
    G     = 0x110,

    // row 2
    Q     = 0x201,
    W     = 0x202,
    E     = 0x204,
    R     = 0x208,
    T     = 0x210,

    // row 3
    D1    = 0x301,
    D2    = 0x302,
    D3    = 0x304,
    D4    = 0x308,
    D5    = 0x310,

    // row 4
    D0    = 0x401,
    D9    = 0x402,
    D8    = 0x404,
    D7    = 0x408,
    D6    = 0x410,

    // row 5
    P     = 0x501,
    O     = 0x502,
    I     = 0x504,
    U     = 0x508,
    Y     = 0x510,

    // row 6
    Enter = 0x601,
    L     = 0x602,
    K     = 0x604,
    J     = 0x608,
    H     = 0x610,

    // row 7
    Space = 0x701,
    Sym   = 0x702,
    M     = 0x704,
    N     = 0x708,
    B     = 0x710
  }
}
