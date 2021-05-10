using System;
using System.Collections.Generic;

namespace MeltySynth
{
    /// <summary>
    /// An instance of the SoundFount synthesizer.
    /// </summary>
    public sealed class Synthesizer
    {
        private static readonly int channelCount = 16;
        private static readonly int percussionChannel = 9;

        private readonly SoundFont soundFont;
        private readonly int sampleRate;
        private readonly int blockSize;
        private readonly int maximumPolyphony;
        private readonly bool enableReverbAndChorus;

        private readonly int minimumVoiceDuration;

        private readonly Dictionary<int, Preset> presetLookup;

        private readonly Channel[] channels;

        private readonly VoiceCollection voices;

        private readonly float[] blockLeft;
        private readonly float[] blockRight;

        private int blockRead;

        private long processedSampleCount;

        private float masterVolume;

        private Reverb reverb;
        private float[] reverbInputLeft;
        private float[] reverbInputRight;
        private float[] reverbOutputLeft;
        private float[] reverbOutputRight;

        /// <summary>
        /// Initializes a new instance of the synthesizer.
        /// </summary>
        /// <param name="soundFontPath">The SoundFont file name and path.</param>
        /// <param name="sampleRate">The sample rate for synthesis.</param>
        public Synthesizer(string soundFontPath, int sampleRate) : this(new SoundFont(soundFontPath), new SynthesizerSettings(sampleRate))
        {
        }

        /// <summary>
        /// Initializes a new instance of the synthesizer.
        /// </summary>
        /// <param name="soundFont">The SoundFont instance.</param>
        /// <param name="sampleRate">The sample rate for synthesis.</param>
        public Synthesizer(SoundFont soundFont, int sampleRate) : this(soundFont, new SynthesizerSettings(sampleRate))
        {
        }

        /// <summary>
        /// Initializes a new instance of the synthesizer.
        /// </summary>
        /// <param name="soundFontPath">The SoundFont file name and path.</param>
        /// <param name="settings">The settings of the synthesizer.</param>
        public Synthesizer(string soundFontPath, SynthesizerSettings settings) : this(new SoundFont(soundFontPath), settings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the synthesizer.
        /// </summary>
        /// <param name="soundFont">The SoundFont instance.</param>
        /// <param name="settings">The settings of the synthesizer.</param>
        public Synthesizer(SoundFont soundFont, SynthesizerSettings settings)
        {
            if (soundFont == null)
            {
                throw new ArgumentNullException(nameof(soundFont));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.soundFont = soundFont;
            this.sampleRate = settings.SampleRate;
            this.blockSize = settings.BlockSize;
            this.maximumPolyphony = settings.MaximumPolyphony;
            this.enableReverbAndChorus = settings.EnableReverbAndChorus;

            minimumVoiceDuration = sampleRate / 500;

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

            voices = new VoiceCollection(this, maximumPolyphony);

            blockLeft = new float[blockSize];
            blockRight = new float[blockSize];

            blockRead = blockSize;

            processedSampleCount = 0;

            masterVolume = 0.5F;

            if (enableReverbAndChorus)
            {
                reverb = new Reverb(sampleRate, blockSize);
                reverbInputLeft = new float[blockSize];
                reverbInputRight = new float[blockSize];
                reverbOutputLeft = new float[blockSize];
                reverbOutputRight = new float[blockSize];
            }
        }

        internal Synthesizer(int sampleRate)
        {
            this.sampleRate = sampleRate;
            this.blockSize = SynthesizerSettings.DefaultBlockSize;
            this.maximumPolyphony = SynthesizerSettings.DefaultMaximumPolyphony;
            this.enableReverbAndChorus = SynthesizerSettings.DefaultEnableReverbAndChorus;
        }

        /// <summary>
        /// Processes the MIDI message.
        /// </summary>
        /// <param name="channel">The channel to which the message should be sent.</param>
        /// <param name="command">The type of the message.</param>
        /// <param name="data1">The first data part of the message.</param>
        /// <param name="data2">The second data part of the message.</param>
        public void ProcessMidiMessage(int channel, int command, int data1, int data2)
        {
            if (!(0 <= channel && channel < channels.Length))
            {
                return;
            }

            var channelInfo = channels[channel];

            switch (command)
            {
                case 0x80: // Note Off
                    NoteOff(channel, data1);
                    break;

                case 0x90: // Note On
                    NoteOn(channel, data1, data2);
                    break;

                case 0xB0: // Controller
                    switch (data1)
                    {
                        case 0x00: // Bank Selection
                            channelInfo.SetBank(data2);
                            break;

                        case 0x01: // Modulation Coarse
                            channelInfo.SetModulationCoarse(data2);
                            break;

                        case 0x21: // Modulation Fine
                            channelInfo.SetModulationCoarse(data2);
                            break;

                        case 0x06: // Data Entry Coarse
                            channelInfo.DataEntryCoarse(data2);
                            break;

                        case 0x26: // Data Entry Fine
                            channelInfo.DataEntryFine(data2);
                            break;

                        case 0x07: // Channel Volume Coarse
                            channelInfo.SetVolumeCoarse(data2);
                            break;

                        case 0x27: // Channel Volume Fine
                            channelInfo.SetVolumeFine(data2);
                            break;

                        case 0x0A: // Pan Coarse
                            channelInfo.SetPanCoarse(data2);
                            break;

                        case 0x2A: // Pan Fine
                            channelInfo.SetPanFine(data2);
                            break;

                        case 0x0B: // Expression Coarse
                            channelInfo.SetExpressionCoarse(data2);
                            break;

                        case 0x2B: // Expression Fine
                            channelInfo.SetExpressionFine(data2);
                            break;

                        case 0x40: // Hold Pedal
                            channelInfo.SetHoldPedal(data2);
                            break;

                        case 0x5B: // Reverb Send
                            channelInfo.SetReverbSend(data2);
                            break;

                        case 0x5D: // Chorus Send
                            channelInfo.SetChorusSend(data2);
                            break;

                        case 0x65: // RPN Coarse
                            channelInfo.SetRpnCoarse(data2);
                            break;

                        case 0x64: // RPN Fine
                            channelInfo.SetRpnFine(data2);
                            break;

                        case 0x78: // All Sound Off
                            NoteOffAll(channel, true);
                            break;

                        case 0x79: // Reset All Controllers
                            ResetAllControllers(channel);
                            break;

                        case 0x7B: // All Note Off
                            NoteOffAll(channel, false);
                            break;
                    }
                    break;

                case 0xC0: // Program Change
                    channelInfo.SetPatch(data1);
                    break;

                case 0xE0: // Pitch Bend
                    channelInfo.SetPitchBend(data1, data2);
                    break;
            }
        }

        /// <summary>
        /// End a note.
        /// </summary>
        /// <param name="channel">The channel of the note.</param>
        /// <param name="key">The key of the note.</param>
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

        /// <summary>
        /// Start a note.
        /// </summary>
        /// <param name="channel">The channel of the note.</param>
        /// <param name="key">The key of the note.</param>
        /// <param name="velocity">The velocity of the note.</param>
        public void NoteOn(int channel, int key, int velocity)
        {
            if (velocity == 0)
            {
                NoteOff(channel, key);
                return;
            }

            if (!(0 <= channel && channel < channels.Length))
            {
                return;
            }

            var channelInfo = channels[channel];

            var presetId = (channelInfo.BankNumber << 16) | channelInfo.PatchNumber;

            Preset preset;
            if (!presetLookup.TryGetValue(presetId, out preset))
            {
                return;
            }

            foreach (var presetRegion in preset.Regions)
            {
                if (presetRegion.Contains(key, velocity))
                {
                    foreach (var instrumentRegion in presetRegion.Instrument.Regions)
                    {
                        if (instrumentRegion.Contains(key, velocity))
                        {
                            var regionPair = new RegionPair(presetRegion, instrumentRegion);

                            var voice = voices.RequestNew(instrumentRegion, channel);
                            if (voice != null)
                            {
                                voice.Start(regionPair, channel, key, velocity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// End all the notes.
        /// </summary>
        /// <param name="immediate">If <c>true</c>, notes stop without the release sound.</param>
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

        /// <summary>
        /// End all the notes in the specified channel.
        /// </summary>
        /// <param name="channel">The channel in which the notes should be stopped.</param>
        /// <param name="immediate">If <c>true</c>, notes stop without the release sound.</param>
        public void NoteOffAll(int channel, bool immediate)
        {
            if (immediate)
            {
                foreach (var voice in voices)
                {
                    if (voice.Channel == channel)
                    {
                        voice.Kill();
                    }
                }
            }
            else
            {
                foreach (var voice in voices)
                {
                    if (voice.Channel == channel)
                    {
                        voice.End();
                    }
                }
            }
        }

        /// <summary>
        /// Reset all the controllers.
        /// </summary>
        public void ResetAllControllers()
        {
            foreach (var channel in channels)
            {
                channel.ResetAllControllers();
            }
        }

        /// <summary>
        /// Reset all the controllers of the specified channel.
        /// </summary>
        /// <param name="channel">The channel to reset the controllers.</param>
        public void ResetAllControllers(int channel)
        {
            if (!(0 <= channel && channel < channels.Length))
            {
                return;
            }

            channels[channel].ResetAllControllers();
        }

        /// <summary>
        /// Reset the synthesizer.
        /// </summary>
        public void Reset()
        {
            voices.Clear();

            foreach (var channel in channels)
            {
                channel.Reset();
            }
        }

        /// <summary>
        /// Render the waveform.
        /// </summary>
        /// <param name="left">The buffer of the left channel to store the rendered waveform.</param>
        /// <param name="right">The buffer of the right channel to store the rendered waveform.</param>
        /// <remarks>
        /// The buffers must be the same length.
        /// </remarks>
        public void Render(Span<float> left, Span<float> right)
        {
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

                blockLeft.AsSpan(blockRead, rem).CopyTo(left.Slice(wrote, rem));
                blockRight.AsSpan(blockRead, rem).CopyTo(right.Slice(wrote, rem));

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
                    ArrayMath.MultiplyAdd(gainLeft, source, blockLeft);
                }

                var gainRight = masterVolume * voice.MixGainRight;
                if (gainRight > SoundFontMath.NonAudible)
                {
                    ArrayMath.MultiplyAdd(gainRight, source, blockRight);
                }
            }

            if (enableReverbAndChorus)
            {
                Array.Clear(reverbInputLeft, 0, reverbInputLeft.Length);
                Array.Clear(reverbInputRight, 0, reverbInputRight.Length);

                foreach (var voice in voices)
                {
                    var source = voice.Block;

                    var gainLeft = voice.MixGainLeft * voice.ReverbSend;
                    if (gainLeft > SoundFontMath.NonAudible)
                    {
                        ArrayMath.MultiplyAdd(gainLeft, source, reverbInputLeft);
                    }

                    var gainRight = voice.MixGainRight * voice.ReverbSend;
                    if (gainRight > SoundFontMath.NonAudible)
                    {
                        ArrayMath.MultiplyAdd(gainRight, source, reverbInputRight);
                    }
                }

                reverb.Process(reverbInputLeft, reverbInputRight, reverbOutputLeft, reverbOutputRight);
                ArrayMath.MultiplyAdd(masterVolume, reverbOutputLeft, blockLeft);
                ArrayMath.MultiplyAdd(masterVolume, reverbOutputRight, blockRight);
            }

            processedSampleCount += blockSize;
        }

        /// <summary>
        /// The block size of waveform rendering.
        /// </summary>
        public int BlockSize => blockSize;

        /// <summary>
        /// The number of maximum polyphony.
        /// </summary>
        public int MaximumPolyphony => maximumPolyphony;

        /// <summary>
        /// The number of channels.
        /// </summary>
        /// <remarks>
        /// This value is always 16.
        /// </remarks>
        public int ChannelCount => channelCount;

        /// <summary>
        /// The percussion channel.
        /// </summary>
        /// <remarks>
        /// This value is always 9.
        /// </remarks>
        public int PercussionChannel => percussionChannel;

        /// <summary>
        /// The SoundFont used as the audio source.
        /// </summary>
        public SoundFont SoundFont => soundFont;

        /// <summary>
        /// The sample rate for synthesis.
        /// </summary>
        public int SampleRate => sampleRate;

        /// <summary>
        /// The number of voices currently played.
        /// </summary>
        public int ActiveVoiceCount => voices.ActiveVoiceCount;

        /// <summary>
        /// The number of samples processed.
        /// </summary>
        public long ProcessedSampleCount => processedSampleCount;

        /// <summary>
        /// Gets or sets the master volume.
        /// </summary>
        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = value;
        }

        internal int MinimumVoiceDuration => minimumVoiceDuration;
        internal Channel[] Channels => channels;
    }
}
