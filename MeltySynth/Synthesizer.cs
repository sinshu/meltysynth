using System;
using System.Linq;

namespace MeltySynth
{
    public sealed class Synthesizer
    {
        private static readonly int blockSize = 64;
        private static readonly int maxActiveVoiceCount = 64;

        private static readonly int channelCount = 16;
        private static readonly int percussionChannel = 9;

        private SoundFont soundFont;
        private int sampleRate;

        private Channel[] channels;

        private VoiceCollection voices;

        public Synthesizer(SoundFont soundFont, int sampleRate)
        {
            this.soundFont = soundFont;
            this.sampleRate = sampleRate;

            channels = new Channel[channelCount];
            for (var i = 0; i < channels.Length; i++)
            {
                channels[i] = new Channel(this);
            }

            voices = new VoiceCollection(this, maxActiveVoiceCount);
        }

        internal Synthesizer(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public void NoteOn(int channel, int key, int velocity)
        {
            var inst = soundFont.Instruments.First(x => x.Name == "Piano 1");
            var region = inst.Regions.First(x => x.Contains(key, velocity));

            var voice = voices.GetFreeVoice();
            if (voice != null)
            {
                voice.Start(new RegionPair(PresetRegion.Default, region), channel, key, velocity);
            }
        }

        public void NoteOff(int channel, int key)
        {
            foreach (var voice in voices)
            {
                if (voice.Channel == channel && voice.Key == key)
                {
                    voice.End();
                }
            }
        }

        public void RenderBlock(float[] destination)
        {
            Array.Clear(destination, 0, destination.Length);

            voices.Process();

            foreach (var voice in voices)
            {
                var source = voice.Block;

                for (var t = 0; t < source.Length; t++)
                {
                    destination[t] += 0.2F * source[t];
                }
            }
        }

        public int BlockSize => blockSize;
        public int MaxActiveVoiceCount => maxActiveVoiceCount;

        public int ChannelCount => channelCount;
        public int PercussionChannel => percussionChannel;

        public SoundFont SoundFont => soundFont;
        public int SampleRate => sampleRate;

        public int ActiveVoiceCount => voices.ActiveVoiceCount;

        internal Channel[] Channels => channels;
    }
}
