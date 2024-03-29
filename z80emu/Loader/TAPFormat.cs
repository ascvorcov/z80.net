using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace z80emu.Loader
{
    public class TAPFormat
    {
        public class Block
        {
            ushort size;
            byte header;
            byte[] data;
            byte checksum;
            public Block(BinaryReader reader)
            {
                this.size = reader.ReadUInt16();
                this.header = reader.ReadByte();
                this.data = reader.ReadBytes(size - 2);
                this.checksum = reader.ReadByte();
                if ((this.checksum ^ this.header) != this.data.Aggregate(0, (a,b) => a^b))
                    throw new System.Exception("invalid checksum");
            }

            public bool Header => this.header == 0;

            public string Name => this.Header ? System.Text.Encoding.ASCII.GetString(this.data[1..11]) : "<binary>";

            public int SizeBits => (this.size + 2) * 8;

            public bool GetBit(int n)
            {
                if (n < 8)
                    return header == 0 ? false : true;

                if (n >= 8 && n < this.data.Length * 8 + 8)
                    return GetBit(this.data[(n - 8) / 8], n % 8);

                return GetBit(this.checksum, n % 8);
            }

            private static bool GetBit(byte b, int n)
            {
                // byte - 10100101 = 0xA5
                //        ^-n=0  ^-n=7
                return (b & (1 << (7-n))) != 0;
            }
           
        }
        private List<Block> blocks = new List<Block>();
        public TAPFormat(Stream data)
        {
            using var reader = new BinaryReader(data);
            while (reader.BaseStream.Position != reader.BaseStream.Length)
                this.blocks.Add(new Block(reader));
        }

        public Block GetBlock(int i)
        {
            if (i >= this.blocks.Count || i < 0)
                return null;
            return this.blocks[i];
        }
    }
}