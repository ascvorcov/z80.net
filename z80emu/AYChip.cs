using System;
using System.Collections.Generic;
using System.Threading;

namespace z80emu
{
    // https://k1.spdns.de/Develop/Hardware/Infomix/ICs%20computer/IO%2C%20DMA%2C%20Timer/Sound/AY8910%2C%20AY8912%20PSG/psg.html
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
        public int NoiseFrequency => registers[6] & 0b11111; // 0...31
        public MixerState State {get;}

        public VolumeData ChannelAVolume {get;}
        public VolumeData ChannelBVolume {get;}
        public VolumeData ChannelCVolume {get;}
        public int EnvelopePeriod => registers[12] << 8 | registers[11];

        public Shape EnvelopeShapeCycle => (Shape)(registers[13] & 0b1111);
    }

    class Constants
    {
        public const int SAMPLING_FREQ = 40;
        public const int SOUND_FRAME_SIZE = 652;
    }

    class ChannelState
    {
        private bool tone;
        private bool noise;
        private bool useEnvelope;
        private int tonePeriod;
        private int noiseFrequency;
        private int envelopePeriod;
        private int volumeLevel;
        private AYStorage.Shape envelopeShape;
        private long nextToneCheckpoint;
        private long nextEnvelopeCheckpoint;
        private bool toneHigh;
        private int envelopeIndex;
        private byte[] currentSoundFrame = new byte[Constants.SOUND_FRAME_SIZE];
        private byte[] lastSoundFrame = new byte[Constants.SOUND_FRAME_SIZE];
        private bool frameHasAnySoundData = false;

        private static readonly List<byte[]> patterns = new List<byte[]>
        {
            new byte[32]{15,14,13,12,11,10,9,8,7,6,5,4,3,2,1,0,15,14,13,12,11,10,9,8,7,6,5,4,3,2,1,0},
            new byte[32]{15,14,13,12,11,10,9,8,7,6,5,4,3,2,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
            new byte[32]{15,14,13,12,11,10,9,8,7,6,5,4,3,2,1,0,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15},
            new byte[32]{15,14,13,12,11,10,9,8,7,6,5,4,3,2,1,0,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15},
            new byte[32]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15},
            new byte[32]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15,15},
            new byte[32]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,15,14,13,12,11,10,9,8,7,6,5,4,3,2,1,0},
            new byte[32]{0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
        };

        public bool Sample(int index)
        {
            bool ret = false;
            var tone = (byte)GetTone();
            this.currentSoundFrame[index] = tone;
            this.frameHasAnySoundData |= tone != 0;
            if (index == this.currentSoundFrame.Length - 1)
            {
                ret = this.frameHasAnySoundData;
                this.frameHasAnySoundData = false;
                this.currentSoundFrame = Interlocked.Exchange(ref this.lastSoundFrame, this.currentSoundFrame);
            }
            return ret;
        }
        public byte[] GetSoundFrame() => this.lastSoundFrame;

        public void SetTonePeriod(int tonePeriod)
        {
            // tone period = tp = 0..4095
            // tp=4095 freq is 1773450 / 16 / 4095 = 27 Hz (27 times per second)
            // tp=1 freq is 1773450 / 16 / 1 = 110,84 KHz (110840 times per second)
            this.tonePeriod = 16 * (tonePeriod == 0 ? 1 : tonePeriod);
            this.nextToneCheckpoint = 0;
        }
        public void SetEnvelopePeriod(int envelopePeriod)
        {
            // 0..65535  The larger the value sent the longer the cycle will be
            // ep=1 freq is 1773450 / 256 / 1 = 6927.54 Hz (envelope plays almost 7k times per second)
            // ep=65535 freq is 1773450 / 256 / 65535 = 0.1057 Hz (envelope lasts for 10 seconds)
            // which means one envelope takes 256..16776960 ticks
            this.envelopePeriod = 16 * (envelopePeriod == 0 ? 1 : envelopePeriod);
            this.nextEnvelopeCheckpoint = 0;
        }
        public void SetNoiseFrequency(int frequency)
        {
            // 0..31  Low values produce hissing, while large values produce grating noises.
            this.noiseFrequency = frequency;
        }
        public void SetEnvelopeShape(AYStorage.Shape shape)
        {
            this.envelopeShape = shape;
        }
        public void SetVolume(int level, bool useEnvelope)
        {
            //useEnvelope - if true, envelope volume is used
            //level - if mode is false, fixed volume level is used, 0..15
            this.volumeLevel = level;
            this.useEnvelope = useEnvelope;
        }
        public void EnableSound(bool tone, bool noise)
        {
            this.tone = tone;
            this.noise = noise;
        }

        public void Update(long clock)
        {
            if (tonePeriod > 0)
            {
                if (nextToneCheckpoint == 0)
                {
                    nextToneCheckpoint = clock + tonePeriod;
                    toneHigh = true;
                }
                else if (clock >= nextToneCheckpoint)
                {
                    toneHigh = !toneHigh;
                    nextToneCheckpoint += tonePeriod;
                }
            }

            if (envelopePeriod > 0)
            {
                if (nextEnvelopeCheckpoint == 0)
                {
                    nextEnvelopeCheckpoint = clock + envelopePeriod;
                    envelopeIndex = 0;
                }
                else if (clock >= nextEnvelopeCheckpoint)
                {
                    nextEnvelopeCheckpoint += envelopePeriod;
                    envelopeIndex++;
                }
            }
        }

        public int GetTone()
        {
            if (!tone && !noise)
                return 0;

            if (noise)
                return volumeLevel == 0 ? 0 : Random.Shared.Next(0, 256);

            if (!this.useEnvelope)
                return toneHigh ? volumeLevel * 17 : 0;

            if (!envelopeShape.HasFlag(AYStorage.Shape.Continue))
            {
                bool attack = envelopeShape.HasFlag(AYStorage.Shape.Attack);
                return 17 * patterns[attack ? 7 : 1][envelopeIndex > 16 ? 16 : envelopeIndex];
            }

            var pattern = patterns[(int)envelopeShape & 0b111];
            bool hold = envelopeShape.HasFlag(AYStorage.Shape.Hold);

            return 17 * pattern[hold ? (envelopeIndex > 16 ? 16 : envelopeIndex) : envelopeIndex % 32];
        }
    }

    [Flags]
    public enum ChannelFlags
    {
        NoSound = 0,
        ChannelA = 1,
        ChannelB = 2,
        ChannelC = 4
    }
    
    // AY-3-8912
    public class AYChip
    {
        private AYStorage storage = new AYStorage();

        private byte selected;
        private long nextCheckpoint = Constants.SAMPLING_FREQ;
        ChannelState channelA = new ChannelState();
        ChannelState channelB = new ChannelState();
        ChannelState channelC = new ChannelState();

        public AYChip()
        {
            //channelA.SetTonePeriod(50);
            //channelA.SetVolume(15, false);
            //channelA.EnableSound(true, false);
        }

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
            if (this.selected >= this.storage.registers.Length)
                return;

            this.storage.registers[this.selected] = value;
            switch (this.selected)
            {
                case 0:
                case 1:
                    channelA.SetTonePeriod(this.storage.ChannelATonePeriod);
                    break;
                case 2:
                case 3:
                    channelB.SetTonePeriod(this.storage.ChannelBTonePeriod);
                    break;
                case 4:
                case 5:
                    channelC.SetTonePeriod(this.storage.ChannelCTonePeriod);
                    break;
                case 6:
                    channelA.SetNoiseFrequency(this.storage.NoiseFrequency);
                    channelB.SetNoiseFrequency(this.storage.NoiseFrequency);
                    channelC.SetNoiseFrequency(this.storage.NoiseFrequency);
                    break;
                case 7:
                    channelA.EnableSound(this.storage.State.ToneEnabled.A, this.storage.State.NoiseEnabled.A);
                    channelB.EnableSound(this.storage.State.ToneEnabled.B, this.storage.State.NoiseEnabled.B);
                    channelC.EnableSound(this.storage.State.ToneEnabled.C, this.storage.State.NoiseEnabled.C);
                    break;
                case 8:
                    channelA.SetVolume(this.storage.ChannelAVolume.VolumeLevel, this.storage.ChannelAVolume.Mode);
                    break;
                case 9:
                    channelB.SetVolume(this.storage.ChannelBVolume.VolumeLevel, this.storage.ChannelBVolume.Mode);
                    break;
                case 10:
                    channelC.SetVolume(this.storage.ChannelCVolume.VolumeLevel, this.storage.ChannelCVolume.Mode);
                    break;
                case 11:
                case 12:
                    channelA.SetEnvelopePeriod(this.storage.EnvelopePeriod);
                    channelB.SetEnvelopePeriod(this.storage.EnvelopePeriod);
                    channelC.SetEnvelopePeriod(this.storage.EnvelopePeriod);
                    break;
                case 13:
                    channelA.SetEnvelopeShape(this.storage.EnvelopeShapeCycle);
                    channelB.SetEnvelopeShape(this.storage.EnvelopeShapeCycle);
                    channelC.SetEnvelopeShape(this.storage.EnvelopeShapeCycle);
                    break;
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

        // Spectrum128 frequency is 3546900 Hz
        // 70908 T states for one screen refresh, 50.01 Hz.
        // AY in original spectrum runs at 1 MHz
        // AY in spectrum clones run at half cpu frequency, 1773450 Hz
        public ChannelFlags Tick(long clock) // AY clock ticks (1.75 MHz) 
        {
            var ret = ChannelFlags.NoSound;
            if (clock >= this.nextCheckpoint)
            {
                var currentSoundSampleIndex = (int)((this.nextCheckpoint / Constants.SAMPLING_FREQ) % Constants.SOUND_FRAME_SIZE);
                this.nextCheckpoint += Constants.SAMPLING_FREQ;
                ret |= this.channelA.Sample(currentSoundSampleIndex) ? ChannelFlags.ChannelA : ChannelFlags.NoSound;
                ret |= this.channelB.Sample(currentSoundSampleIndex) ? ChannelFlags.ChannelB : ChannelFlags.NoSound;
                ret |= this.channelC.Sample(currentSoundSampleIndex) ? ChannelFlags.ChannelC : ChannelFlags.NoSound;
            }

            UpdateState(clock);
            return ret;
        }

        private void UpdateState(long clock)
        {
            channelA.Update(clock);
            channelB.Update(clock);
            channelC.Update(clock);
        }

        public byte[] GetSoundFrame(int channel)
        {
            switch (channel)
            {
                case 0: return this.channelA.GetSoundFrame();
                case 1: return this.channelB.GetSoundFrame();
                case 2: return this.channelC.GetSoundFrame();
                default: throw new ArgumentOutOfRangeException();
            }            
        }
    }
}
