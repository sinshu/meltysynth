using System;

namespace MeltySynth
{
    public sealed class SynthesizerSettings
    {
        internal static int DefaultBlockSize = 64;
        internal static int DefaultMaximumPolyphony = 64;

        private int sampleRate;
        private int blockSize;
        private int maximumPolyphony;

        public SynthesizerSettings(int sampleRate)
        {
            CheckSampleRate(sampleRate);

            this.sampleRate = sampleRate;
            this.blockSize = DefaultBlockSize;
            this.maximumPolyphony = DefaultMaximumPolyphony;
        }

        private static void CheckSampleRate(int value)
        {
            if (!(16000 <= value && value <= 192000))
            {
                throw new ArgumentOutOfRangeException("The sample rate must be between 16000 and 192000.");
            }
        }

        private static void CheckBlockSize(int value)
        {
            if (!(8 <= value && value <= 1024))
            {
                throw new ArgumentOutOfRangeException("The block size must be between 8 and 1024.");
            }
        }

        private static void CheckMaximumPolyphony(int value)
        {
            if (!(8 <= value && value <= 256))
            {
                throw new ArgumentOutOfRangeException("The maximum number of polyphony must be between 8 and 256.");
            }
        }

        public int SampleRate
        {
            get => sampleRate;

            set
            {
                CheckSampleRate(value);
                sampleRate = value;
            }
        }

        public int BlockSize
        {
            get => blockSize;

            set
            {
                CheckBlockSize(value);
                blockSize = value;
            }
        }

        public int MaximumPolyphony
        {
            get => maximumPolyphony;

            set
            {
                CheckMaximumPolyphony(value);
                maximumPolyphony = value;
            }
        }
    }
}
