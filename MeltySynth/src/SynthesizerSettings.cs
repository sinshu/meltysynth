using System;

namespace MeltySynth
{
    public sealed class SynthesizerSettings
    {
        private int sampleRate;
        private int blockSize;
        private int maxActiveVoiceCount;

        public SynthesizerSettings()
        {
            this.sampleRate = 44100;
            this.blockSize = 64;
            this.maxActiveVoiceCount = 32;
        }

        public SynthesizerSettings(int sampleRate)
        {
            if (!(16000 <= sampleRate && sampleRate <= 192000))
            {
                throw new ArgumentOutOfRangeException("The sample rate must be between 8000 and 192000.");
            }

            this.sampleRate = sampleRate;
            this.blockSize = 64;
            this.maxActiveVoiceCount = 32;
        }

        public int SampleRate
        {
            get => sampleRate;

            set
            {
                if (!(16000 <= value && value <= 192000))
                {
                    throw new ArgumentOutOfRangeException("The sample rate must be between 8000 and 192000.");
                }

                sampleRate = value;
            }
        }

        public int BlockSize
        {
            get => blockSize;

            set
            {
                if (!(8 <= value && value <= 1024))
                {
                    throw new ArgumentOutOfRangeException("The block size must be between 8 and 1024.");
                }

                blockSize = value;
            }
        }

        public int MaxActiveVoiceCount
        {
            get => maxActiveVoiceCount;

            set
            {
                if (!(8 <= value && value <= 256))
                {
                    throw new ArgumentOutOfRangeException("The max number of active voices must be between 8 and 256.");
                }

                maxActiveVoiceCount = value;
            }
        }
    }
}
