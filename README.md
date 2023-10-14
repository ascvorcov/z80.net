# ZX Spectrum 48K-128K emulator

Simple .net core implementation of Z80 CPU emulator, ULA/AY chip and .Z80/.TAP file format reader.
UI is implemented using cross-platform Avalonia library.

![](images/win7.png)

## Features:

* Runs both under windows and linux
* Can load and play z80 v1 and v2 format apps
* Can play TAP files
* Processor and ULA emulation not coupled with UI
* Clock cycles are emulated, not affected by real time - supports single-frame stepping
* Instruction timing according to specs
* Passes zexdoc and zexall tests
* Capable of running at 1K FPS
* Contains reader of zx spectrum floating point format

## Bugs and limitations:

* Ubuntu version doesn't react on keydown events
* Sound output supported only on win32
* No joystick support
* AF register undocumented bits 3 and 5 behavior is not completely implemented
* Non-maskable interrupts are not supported
* Output tv signal is done line-by-line instead of pixel-by-pixel, so some game effects look invalid
* Contended memory emulation is not implemented
* R register increment is simplified and based only on instruction size

