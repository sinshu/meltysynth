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
                channels[i] = new Channel(this, i == percussionChannel);
            }

            voices = new VoiceCollection(this, maxActiveVoiceCount);
        }

        internal Synthesizer(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public void ProcessMidiMessage(int channel, int command, int data1, int data2)
        {
            var channelInfo = channels[channel];

            switch (command)
            {
                case 0xB0: // Controller
                    switch (data1)
                    {
                        case 0x00: // Bank select coarse
                            channelInfo.SetBank(data2);
                            break;

                        case 0x07: // Channel volume coarse
                            channelInfo.SetVolumeCourse(data2);
                            break;

                        case 0x27: // Channel volume fine
                            channelInfo.SetVolumeFine(data2);
                            break;

                        case 0x0B: // Expression coarse
                            channelInfo.SetExpressionCourse(data2);
                            break;

                        case 0x2B: // Expression fine
                            channelInfo.SetExpressionFine(data2);
                            break;
                    }
                    break;

                case 0xC0: // Program change
                    channelInfo.SetPatch(data1);
                    break;
            }
        }

        public void NoteOn(int channel, int key, int velocity)
        {
            var preset = channels[channel].Preset;

            foreach (var presetRegion in preset.Regions)
            {
                if (presetRegion.Contains(key, velocity))
                {
                    foreach (var instrumentRegion in presetRegion.Instrument.Regions)
                    {
                        if (instrumentRegion.Contains(key, velocity))
                        {
                            var regionPair = new RegionPair(presetRegion, instrumentRegion);

                            var voice = voices.GetFreeVoice();
                            if (voice != null)
                            {
                                voice.Start(regionPair, channel, key, velocity);
                            }
                        }
                    }
                }
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
                var factor = voice.MixGain;

                for (var t = 0; t < source.Length; t++)
                {
                    destination[t] += factor * source[t];
                }
            }

            // TODO: Implement master volume.
            for (var t = 0; t < destination.Length; t++)
            {
                destination[t] *= 0.5F;
            }
        }

        internal Preset GetPreset(int bankNumber, int patchNumber)
        {
            foreach (var preset in soundFont.Presets)
            {
                if (preset.BankNumber == bankNumber && preset.PatchNumber == patchNumber)
                {
                    return preset;
                }
            }

            return null;
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
