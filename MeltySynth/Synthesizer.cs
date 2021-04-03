using System;
using System.Linq;

namespace MeltySynth
{
    public sealed class Synthesizer
    {
        private static readonly int blockSize = 64;
        private static readonly int maxActiveVoiceCount = 64;

        private SoundFont soundFont;
        private int sampleRate;

        private Voice test;

        public Synthesizer(SoundFont soundFont, int sampleRate)
        {
            this.soundFont = soundFont;
            this.sampleRate = sampleRate;

            test = new Voice(this);
        }

        internal Synthesizer(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public void NoteOn(int channel, int key, int velocity)
        {
            var inst = soundFont.Instruments.First(x => x.Name == "Piano 1");
            var region = inst.Regions.First(x => x.Contains(key, velocity));
            test.Start(new RegionPair(PresetRegion.Default, region), channel, key, velocity);
        }

        public void NoteOff(int channel, int key)
        {
            test.End();
        }

        public void Render(float[] data)
        {
        }

        public int BlockSize => blockSize;
        public int MaxActiveVoiceCount => maxActiveVoiceCount;

        public SoundFont SoundFont => soundFont;
        public int SampleRate => sampleRate;
    }
}
