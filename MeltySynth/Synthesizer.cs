using System;
using System.Collections.Generic;

namespace MeltySynth
{
    public sealed class Synthesizer
    {
        private static readonly int blockSize = 64;
        private static readonly int maxActiveVoiceCount = 32;

        private static readonly int channelCount = 16;
        private static readonly int percussionChannel = 9;

        private SoundFont soundFont;
        private int sampleRate;

        private int minimumVoiceLength;

        private Dictionary<int, Preset> presetLookup;

        private Channel[] channels;

        private VoiceCollection voices;

        private float[] blockLeft;
        private float[] blockRight;
        private int blockRead;

        private float masterVolume;

        public Synthesizer(SoundFont soundFont, int sampleRate)
        {
            if (soundFont == null)
            {
                throw new ArgumentNullException(nameof(soundFont));
            }

            if (!(8000 <= sampleRate && sampleRate <= 192000))
            {
                throw new ArgumentOutOfRangeException("The sample rate must be between 8000 and 192000.");
            }

            this.soundFont = soundFont;
            this.sampleRate = sampleRate;

            minimumVoiceLength = sampleRate / 500;

            presetLookup = new Dictionary<int, Preset>();
            foreach (var preset in soundFont.Presets)
            {
                var presetId = (preset.BankNumber << 16) | preset.PatchNumber;
                presetLookup.TryAdd(presetId, preset);
            }

            channels = new Channel[channelCount];
            for (var i = 0; i < channels.Length; i++)
            {
                channels[i] = new Channel(this, i == percussionChannel);
            }

            voices = new VoiceCollection(this, maxActiveVoiceCount);

            blockLeft = new float[blockSize];
            blockRight = new float[blockSize];
            blockRead = blockSize;

            masterVolume = 0.5F;
        }

        internal Synthesizer(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public void ProcessMidiMessage(int channel, int command, int data1, int data2)
        {
            if (!(0 <= channel && channel < channels.Length))
            {
                return;
            }

            var channelInfo = channels[channel];

            switch (command)
            {
                case 0x80: // Note off
                    NoteOff(channel, data1);
                    break;

                case 0x90:
                    NoteOn(channel, data1, data2);
                    break;

                case 0xB0: // Controller
                    switch (data1)
                    {
                        case 0x00: // Bank selection
                            channelInfo.SetBank(data2);
                            break;

                        case 0x01: // Modulation coarse
                            channelInfo.SetModulationCoarse(data2);
                            break;

                        case 0x21: // Modulation fine
                            channelInfo.SetModulationCoarse(data2);
                            break;

                        case 0x07: // Channel volume coarse
                            channelInfo.SetVolumeCoarse(data2);
                            break;

                        case 0x27: // Channel volume fine
                            channelInfo.SetVolumeFine(data2);
                            break;

                        case 0x0A: // Pan coarse
                            channelInfo.SetPanCoarse(data2);
                            break;

                        case 0x2A: // Pan fine
                            channelInfo.SetPanFine(data2);
                            break;

                        case 0x0B: // Expression coarse
                            channelInfo.SetExpressionCoarse(data2);
                            break;

                        case 0x2B: // Expression fine
                            channelInfo.SetExpressionFine(data2);
                            break;

                        case 0x40: // Hold pedal
                            channelInfo.SetHoldPedal(data2);
                            break;

                        case 0x65: // RPN coarse
                            channelInfo.SetRpnCoarse(data2);
                            break;

                        case 0x64: // RPN Fine
                            channelInfo.SetRpnFine(data2);
                            break;

                        case 0x7B: // Note Off All
                            NoteOffAll(false);
                            break;

                        case 0x06: // Data entry Coarse
                            channelInfo.DataEntryCoarse(data2);
                            break;

                        case 0x26: // Data entry Fine
                            channelInfo.DataEntryFine(data2);
                            break;

                        case 0x79: // Reset All
                            Reset();
                            break;
                    }
                    break;

                case 0xC0: // Program change
                    channelInfo.SetPatch(data1);
                    break;

                case 0xE0: // Pitch Bend
                    channelInfo.SetPitchBend(data1, data2);
                    break;
            }
        }

        public void NoteOff(int channel, int key)
        {
            if (!(0 <= channel && channel < channels.Length))
            {
                return;
            }

            foreach (var voice in voices)
            {
                if (voice.Channel == channel && voice.Key == key)
                {
                    voice.End();
                }
            }
        }

        public void NoteOn(int channel, int key, int velocity)
        {
            if (!(0 <= channel && channel < channels.Length))
            {
                return;
            }

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

                            var voice = voices.RequestNew(instrumentRegion, channel, key);
                            if (voice != null)
                            {
                                voice.Start(regionPair, channel, key, velocity);
                            }
                        }
                    }
                }
            }
        }

        public void NoteOffAll(bool immediate)
        {
            if (immediate)
            {
                voices.Clear();
            }
            else
            {
                foreach (var voice in voices)
                {
                    voice.End();
                }
            }
        }

        public void Reset()
        {
            voices.Clear();

            foreach (var channel in channels)
            {
                channel.Reset();
            }
        }

        public void RenderStereo(float[] left, float[] right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Length != right.Length)
            {
                throw new ArgumentException("The output buffers must be the same length.");
            }

            var wrote = 0;
            while (wrote < left.Length)
            {
                if (blockRead == blockSize)
                {
                    RenderBlock();
                    blockRead = 0;
                }

                var srcRem = blockSize - blockRead;
                var dstRem = left.Length - wrote;
                var rem = Math.Min(srcRem, dstRem);

                Array.Copy(blockLeft, blockRead, left, wrote, rem);
                Array.Copy(blockRight, blockRead, right, wrote, rem);

                blockRead += rem;
                wrote += rem;
            }
        }

        public void RenderMono(float[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var wrote = 0;
            while (wrote < destination.Length)
            {
                if (blockRead == blockSize)
                {
                    RenderBlock();
                    blockRead = 0;
                }

                var srcRem = blockSize - blockRead;
                var dstRem = destination.Length - wrote;
                var rem = Math.Min(srcRem, dstRem);

                var blockLeftSpan = blockLeft.AsSpan(blockRead, rem);
                var blockRightSpan = blockRight.AsSpan(blockRead, rem);
                var destinationSpan = destination.AsSpan(wrote, rem);
                SpanMath.Mean(blockLeftSpan, blockRightSpan, destinationSpan);

                blockRead += rem;
                wrote += rem;
            }
        }

        private void RenderBlock()
        {
            voices.Process();

            Array.Clear(blockLeft, 0, blockLeft.Length);
            Array.Clear(blockRight, 0, blockRight.Length);

            foreach (var voice in voices)
            {
                var source = voice.Block;

                var gainLeft = masterVolume * voice.MixGainLeft;
                if (gainLeft > SoundFontMath.NonAudible)
                {
                    SpanMath.MultiplyAdd(gainLeft, source, blockLeft);
                }

                var gainRight = masterVolume * voice.MixGainRight;
                if (gainRight > SoundFontMath.NonAudible)
                {
                    SpanMath.MultiplyAdd(gainRight, source, blockRight);
                }
            }
        }

        internal Preset GetPreset(int bankNumber, int patchNumber)
        {
            var presetId = (bankNumber << 16) | patchNumber;

            if (presetLookup.TryGetValue(presetId, out Preset found))
            {
                return found;
            }
            else
            {
                return Preset.Default;
            }
        }

        public int BlockSize => blockSize;
        public int MaxActiveVoiceCount => maxActiveVoiceCount;

        public int ChannelCount => channelCount;
        public int PercussionChannel => percussionChannel;

        public SoundFont SoundFont => soundFont;
        public int SampleRate => sampleRate;

        public int ActiveVoiceCount => voices.ActiveVoiceCount;

        internal int MinimumVoiceLength => minimumVoiceLength;
        internal Channel[] Channels => channels;
    }
}
