using System;
using System.Collections.Generic;
using System.Linq;

namespace z80emu
{
    // https://worldofspectrum.org/faq/reference/128kreference.htm
    class MemoryExtended : IMemory
    {
        private ArraySegment<byte>[] banks = new ArraySegment<byte>[8];
        private ArraySegment<byte> rom128K;
        private ArraySegment<byte> rom48K;
        private byte[] raw_memory;
        private bool pagingDisabled = false;
        private bool showShadowScreen = false;
        private List<ArraySegment<byte>> memory_layout = new List<ArraySegment<byte>>(4);

        public MemoryExtended(byte[] rom128K, byte[] rom48K)
        {
            const int BANK_SIZE = 16384;
            const int BANK_COUNT = 8;

            this.rom128K = rom128K; // ROM0
            this.rom48K = rom48K; // ROM1

            this.raw_memory = new byte[BANK_SIZE * 8];
            for (int i = 0; i < BANK_COUNT; ++i)
            {
                banks[i] = new ArraySegment<byte>(this.raw_memory, i * BANK_SIZE, BANK_SIZE);
            }

            memory_layout[0] = rom128K;
            memory_layout[1] = banks[5];
            memory_layout[2] = banks[2];
            memory_layout[3] = banks[0];
        }

        public void SetBank(byte bank)
        {
            if (this.pagingDisabled)
            {
                return;
            }

            if (bank < this.banks.Length)
            {
                this.memory_layout[3] = this.banks[bank];
            }
        }
        public void Select128KROM()
        {
            if (this.pagingDisabled)
            {
                return;
            }

            this.memory_layout[0] = this.rom128K;
        }
        public void Select48KROM()
        {
            if (this.pagingDisabled)
            {
                return;
            }

            this.memory_layout[0] = this.rom48K;
        }
        public void DisablePaging()
        {
            this.pagingDisabled = true;
        }
        public void ShowNormalScreen()
        {
            if (this.pagingDisabled)
            {
                return;
            }

            this.showShadowScreen = false;
        }
        public void ShowShadowScreen()
        {
            if (this.pagingDisabled)
            {
                return;
            }

            this.showShadowScreen = true;
        }

        void IMemory.Dump()
        {
            System.IO.File.WriteAllBytes("mem.dump", this.raw_memory);
        }

        byte IMemory.ReadByte(ushort offset)
        {
            var bank = this.memory_layout[offset >> 14];
            return bank[offset & 0x3FFF];
        }

        void IMemory.WriteByte(ushort offset, byte data)
        {
            var bank = this.memory_layout[offset >> 14];
            bank[offset & 0x3FFF] = data;
        }

        ReadOnlySpan<byte> IMemory.ReadScreen(ushort offset, ushort count)
        {
            Span<byte> screen = showShadowScreen ? this.banks[7] : this.banks[5];
            return screen.Slice(offset, count);
        }
    }
}