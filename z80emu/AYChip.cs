using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace z80emu
{
    // https://zxpress.ru/book_articles.php?id=1031
    // https://zxpress.ru/article.php?id=5455&ysclid=lmogzkef16579517562
    // http://datasheet.elcodis.com/pdf2/76/12/761232/ay-3-8912.pdf
    // https://habr.com/ru/articles/739110/
    internal class AYStorage
    {
        [Flags]
        public enum Shape
        {
            Hold = 1,
            Alternate = 2,
            Attack = 4,
            Continue = 8
        }

        public class VolumeData
        {
            private byte[] data;
            private int index;
            public int VolumeLevel => data[index] & 0b1111; //0..15
            public bool Mode => (data[index] & 0b10000) != 0;
            public VolumeData(byte[] data, int i)
            {
                this.data = data;
                this.index = i;
            }
        }

        public class MixerState
        {
            public class Channel
            {
                private byte[] data;
                private int shift;
                public bool A => ((data[7] >> shift) & 1) == 0;
                public bool B => ((data[7] >> shift) & 2) == 0;
                public bool C => ((data[7] >> shift) & 4) == 0;
                public Channel(byte[] data, int shift)
                {
                    this.data = data;
                    this.shift = shift;
                }
            }
            public Channel ToneEnabled;
            public Channel NoiseEnabled;
            public Channel InputEnabled;

            public MixerState(byte[] data)
            {
                this.ToneEnabled = new Channel(data, 0);
                this.NoiseEnabled = new Channel(data, 3);
                this.InputEnabled = new Channel(data, 6);
            }
        }

        public AYStorage()
        {
            ChannelAVolume = new VolumeData(registers, 8);
            ChannelBVolume = new VolumeData(registers, 9);
            ChannelCVolume = new VolumeData(registers, 10);
            State = new MixerState(registers);
        }

        public byte[] registers {get;} = new byte[16];

        public int ChannelATonePeriod => (registers[1] << 8 | registers[0]) & 0b111111111111; // 0..4095
        public int ChannelBTonePeriod => (registers[3] << 8 | registers[2]) & 0b111111111111;
        public int ChannelCTonePeriod => (registers[5] << 8 | registers[4]) & 0b111111111111;
        public int NoisePeriod => registers[6] & 0b11111; // 0...31
        public MixerState State {get;}

        public VolumeData ChannelAVolume {get;}
        public VolumeData ChannelBVolume {get;}
        public VolumeData ChannelCVolume {get;}
        public int EnvelopePeriod => registers[12] << 8 | registers[11];

        public Shape EnvelopeShapeCycle => (Shape)(registers[13] & 0b1111);
    }
    
    // AY-3-8912
    class AYChip
    {
        private AYStorage storage = new AYStorage();

        private byte selected;

        public void SelectRegister(byte reg)
        {
            this.selected = reg;
        }
        public byte GetSelectedRegister()
        {
            return this.selected;
        }

        public void WriteToSelectedRegister(byte value)
        {
            if (this.selected < 16)
            {
                this.storage.registers[this.selected] = value;
            }
        }

        public byte ReadFromSelectedRegister()
        {
            if (this.selected < 16)
            {
                return this.storage.registers[this.selected];
            }
            return 0xFF;
        }

        const int FREQ = 3546900 / 2;
        // Spectrum128 frequency is 3546900 Hz
        // 70908 T states for one screen refresh, 50.01 Hz.
        // AY in original spectrum runs at 1 MHz
        // AY in spectrum clones run at 1.75 MHz
        // 
        public bool Tick(long clock)
        {
            ConfigureChannel(
                0,
                this.storage.ChannelATonePeriod,
                this.storage.NoisePeriod,
                this.storage.EnvelopePeriod,
                this.storage.State.ToneEnabled.A,
                this.storage.State.NoiseEnabled.A,
                this.storage.State.InputEnabled.A,
                this.storage.ChannelAVolume,
                this.storage.EnvelopeShapeCycle);
            ConfigureChannel(
                1,
                this.storage.ChannelBTonePeriod,
                this.storage.NoisePeriod,
                this.storage.EnvelopePeriod,
                this.storage.State.ToneEnabled.B,
                this.storage.State.NoiseEnabled.B,
                this.storage.State.InputEnabled.B,
                this.storage.ChannelBVolume,
                this.storage.EnvelopeShapeCycle);
            ConfigureChannel(
                2,
                this.storage.ChannelCTonePeriod,
                this.storage.NoisePeriod,
                this.storage.EnvelopePeriod,
                this.storage.State.ToneEnabled.C,
                this.storage.State.NoiseEnabled.C,
                this.storage.State.InputEnabled.C,
                this.storage.ChannelCVolume,
                this.storage.EnvelopeShapeCycle);

            return false;
        }

        void ConfigureChannel(
            int channel,
            int tonePeriod, int noisePeriod, int envelopePeriod, 
            bool tone, bool noise, bool input, 
            AYStorage.VolumeData volume, AYStorage.Shape envelope)
        {
            //var noise = FREQ / 256 / (noisePeriod == 0 ? 1 : noisePeriod);
            //var tone = FREQ / 16 / (tonePeriod == 0 ? 1 : tonePeriod);
            //var env = FREQ / 256 / (envelopePeriod == 0 ? 1 : envelopePeriod);

        }

        public byte[] GetSoundFrame()
        {
            return new byte[0];
        }
    }
}
