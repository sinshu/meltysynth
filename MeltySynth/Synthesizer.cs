using System;

namespace MeltySynth
{
    public sealed class Synthesizer
    {
        private static readonly int blockSize = 64;

        private SoundFont soundFont;
        private int sampleRate;

        public Synthesizer(SoundFont soundFont, int sampleRate)
        {
            this.soundFont = soundFont;
            this.sampleRate = sampleRate;
        }

        internal Synthesizer(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public int BlockSize => blockSize;

        public SoundFont SoundFont => soundFont;
        public int SampleRate => sampleRate;
    }
}
