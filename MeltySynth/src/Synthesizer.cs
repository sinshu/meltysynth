using System;
using System.Collections.Generic;

namespace MeltySynth
{
    /// <summary>
    /// An instance of the SoundFont synthesizer.
    /// </summary>
    /// <remarks>
    /// Note that this class does not provide thread safety.
    /// If you want to send notes and render the waveform in separate threads,
    /// you must ensure that the methods will not be called simultaneously.
    /// </remarks>
    public sealed class Synthesizer : IAudioRenderer
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
        private readonly Preset defaultPreset;

        private readonly Channel[] channels;

        private readonly VoiceCollection voices;

        private readonly float[] blockLeft;
        private readonly float[] blockRight;

        private readonly float inverseBlockSize;

        private int blockRead;

        private float masterVolume;

        private Reverb reverb;
        private float[] reverbInput;
        private float[] reverbOutputLeft;
        private float[] reverbOutputRight;

        private Chorus chorus;
        private float[] chorusInputLeft;
        private float[] chorusInputRight;
        private float[] chorusOutputLeft;
        private float[] chorusOutputRight;

        /// <summary>
        /// Initializes a new synthesizer using a specified SoundFont and sample rate.
        /// </summary>
        /// <param name="soundFontPath">The SoundFont file name and path.</param>
        /// <param name="sampleRate">The sample rate for synthesis.</param>
        public Synthesizer(string soundFontPath, int sampleRate) : this(new SoundFont(soundFontPath), new SynthesizerSettings(sampleRate))
        {
        }

        /// <summary>
        /// Initializes a new synthesizer using a specified SoundFont and sample rate.
        /// </summary>
        /// <param name="soundFont">The SoundFont instance.</param>
        /// <param name="sampleRate">The sample rate for synthesis.</param>
        public Synthesizer(SoundFont soundFont, int sampleRate) : this(soundFont, new SynthesizerSettings(sampleRate))
        {
        }

        /// <summary>
        /// Initializes a new synthesizer using a specified SoundFont and settings.
        /// </summary>
        /// <param name="soundFontPath">The SoundFont file name and path.</param>
        /// <param name="settings">The settings for synthesis.</param>
        public Synthesizer(string soundFontPath, SynthesizerSettings settings) : this(new SoundFont(soundFontPath), settings)
        {
        }

        /// <summary>
        /// Initializes a new synthesizer using a specified SoundFont and settings.
        /// </summary>
        /// <param name="soundFont">The SoundFont instance.</param>
        /// <param name="settings">The settings for synthesis.</param>
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

            var minPresetId = int.MaxValue;
            foreach (var preset in soundFont.PresetArray)
            {
                // The preset ID is Int32, where the upper 16 bits represent the bank number
                // and the lower 16 bits represent the patch number.
                // This ID is used to search for presets by the combination of bank number
                // and patch number.
                var presetId = (preset.BankNumber << 16) | preset.PatchNumber;
                presetLookup.TryAdd(presetId, preset);

                // The preset with the minimum ID number will be default.
                // If the SoundFont is GM compatible, the piano will be chosen.
                if (presetId < minPresetId)
                {
                    defaultPreset = preset;
                    minPresetId = presetId;
                }
            }

            channels = new Channel[channelCount];
            for (var i = 0; i < channels.Length; i++)
            {
                channels[i] = new Channel(this, i == percussionChannel);
            }

            voices = new VoiceCollection(this, maximumPolyphony);

            blockLeft = new float[blockSize];
            blockRight = new float[blockSize];

            inverseBlockSize = 1F / blockSize;

            blockRead = blockSize;

            masterVolume = 0.5F;

            if (enableReverbAndChorus)
            {
                reverb = new Reverb(sampleRate);
                reverbInput = new float[blockSize];
                reverbOutputLeft = new float[blockSize];
                reverbOutputRight = new float[blockSize];

                chorus = new Chorus(sampleRate, 0.002, 0.0019, 0.4);
                chorusInputLeft = new float[blockSize];
                chorusInputRight = new float[blockSize];
                chorusOutputLeft = new float[blockSize];
                chorusOutputRight = new float[blockSize];
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
        /// Processes a MIDI message.
        /// </summary>
        /// <param name="channel">The channel to which the message will be sent.</param>
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
                case MidiCommand.NOTE_OFF:
                    NoteOff(channel, data1);
                    break;

                case MidiCommand.NOTE_ON:
                    NoteOn(channel, data1, data2);
                    break;

                case MidiCommand.CONTROLLER:
                    switch (data1)
                    {
                        case MidiCommand.BANK_SELECTION:
                            channelInfo.SetBank(data2);
                            break;

                        case MidiCommand.MODULATION_COARSE:
                            channelInfo.SetModulationCoarse(data2);
                            break;

                        case MidiCommand.MODULATION_FINE:
                            channelInfo.SetModulationFine(data2);
                            break;

                        case MidiCommand.DATA_ENTRY_COARSE:
                            channelInfo.DataEntryCoarse(data2);
                            break;

                        case MidiCommand.DATA_ENTRY_FINE:
                            channelInfo.DataEntryFine(data2);
                            break;

                        case MidiCommand.CHANNEL_VOLUME_COARSE:
                            channelInfo.SetVolumeCoarse(data2);
                            break;

                        case MidiCommand.CHANNEL_VOLUME_FINE:
                            channelInfo.SetVolumeFine(data2);
                            break;

                        case MidiCommand.PAN_COARSE:
                            channelInfo.SetPanCoarse(data2);
                            break;

                        case MidiCommand.PAN_FINE:
                            channelInfo.SetPanFine(data2);
                            break;

                        case MidiCommand.EXPRESSION_COARSE:
                            channelInfo.SetExpressionCoarse(data2);
                            break;

                        case MidiCommand.EXPRESSION_FINE:
                            channelInfo.SetExpressionFine(data2);
                            break;

                        case MidiCommand.HOLD_PEDAL:
                            channelInfo.SetHoldPedal(data2);
                            break;

                        case MidiCommand.REVERB_SEND:
                            channelInfo.SetReverbSend(data2);
                            break;

                        case MidiCommand.CHORUS_SEND:
                            channelInfo.SetChorusSend(data2);
                            break;

                        case MidiCommand.RPN_COARSE:
                            channelInfo.SetRpnCoarse(data2);
                            break;

                        case MidiCommand.RPN_FINE:
                            channelInfo.SetRpnFine(data2);
                            break;

                        case MidiCommand.ALL_SOUND_OFF:
                            NoteOffAll(channel, true);
                            break;

                        case MidiCommand.RESET_ALL_CONTROLLERS:
                            ResetAllControllers(channel);
                            break;

                        case MidiCommand.ALL_NOTE_OFF:
                            NoteOffAll(channel, false);
                            break;
                    }
                    break;

                case MidiCommand.PROGRAM_CHANGE:
                    channelInfo.SetPatch(data1);
                    break;

                case MidiCommand.PITCH_BEND:
                    channelInfo.SetPitchBend(data1, data2);
                    break;
            }
        }

        /// <summary>
        /// Stops a note.
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
        /// Starts a note.
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
                // Try fallback to the GM sound set.
                // Normally, the given patch number + the bank number 0 will work.
                // For drums (bank number >= 128), it seems to be better to select the standard set (128:0).
                var gmPresetId = channelInfo.BankNumber < 128 ? channelInfo.PatchNumber : (128 << 16);

                if (!presetLookup.TryGetValue(gmPresetId, out preset))
                {
                    // No corresponding preset was found. Use the default one...
                    preset = defaultPreset;
                }
            }

            foreach (var presetRegion in preset.RegionArray)
            {
                if (presetRegion.Contains(key, velocity))
                {
                    foreach (var instrumentRegion in presetRegion.Instrument.RegionArray)
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
        /// Stops all the notes.
        /// </summary>
        /// <param name="immediate">If <c>true</c>, notes will stop immediately without the release sound.</param>
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
        /// Stops all the notes in the specified channel.
        /// </summary>
        /// <param name="channel">The channel in which the notes will be stopped.</param>
        /// <param name="immediate">If <c>true</c>, notes will stop immediately without the release sound.</param>
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
        /// Resets all the controllers.
        /// </summary>
        public void ResetAllControllers()
        {
            foreach (var channel in channels)
            {
                channel.ResetAllControllers();
            }
        }

        /// <summary>
        /// Resets all the controllers of the specified channel.
        /// </summary>
        /// <param name="channel">The channel to be reset.</param>
        public void ResetAllControllers(int channel)
        {
            if (!(0 <= channel && channel < channels.Length))
            {
                return;
            }

            channels[channel].ResetAllControllers();
        }

        /// <summary>
        /// Resets the synthesizer.
        /// </summary>
        public void Reset()
        {
            voices.Clear();

            foreach (var channel in channels)
            {
                channel.Reset();
            }

            if (enableReverbAndChorus)
            {
                reverb.Mute();
                chorus.Mute();
            }

            blockRead = blockSize;
        }

        /// <inheritdoc/>
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
                var previousGainLeft = masterVolume * voice.PreviousMixGainLeft;
                var currentGainLeft = masterVolume * voice.CurrentMixGainLeft;
                WriteBlock(previousGainLeft, currentGainLeft, voice.Block, blockLeft);
                var previousGainRight = masterVolume * voice.PreviousMixGainRight;
                var currentGainRight = masterVolume * voice.CurrentMixGainRight;
                WriteBlock(previousGainRight, currentGainRight, voice.Block, blockRight);
            }

            if (enableReverbAndChorus)
            {
                Array.Clear(chorusInputLeft, 0, chorusInputLeft.Length);
                Array.Clear(chorusInputRight, 0, chorusInputRight.Length);
                foreach (var voice in voices)
                {
                    var previousGainLeft = voice.PreviousChorusSend * voice.PreviousMixGainLeft;
                    var currentGainLeft = voice.CurrentChorusSend * voice.CurrentMixGainLeft;
                    WriteBlock(previousGainLeft, currentGainLeft, voice.Block, chorusInputLeft);
                    var previousGainRight = voice.PreviousChorusSend * voice.PreviousMixGainRight;
                    var currentGainRight = voice.CurrentChorusSend * voice.CurrentMixGainRight;
                    WriteBlock(previousGainRight, currentGainRight, voice.Block, chorusInputRight);
                }
                chorus.Process(chorusInputLeft, chorusInputRight, chorusOutputLeft, chorusOutputRight);
                ArrayMath.MultiplyAdd(masterVolume, chorusOutputLeft, blockLeft);
                ArrayMath.MultiplyAdd(masterVolume, chorusOutputRight, blockRight);

                Array.Clear(reverbInput, 0, reverbInput.Length);
                foreach (var voice in voices)
                {
                    var previousGain = reverb.InputGain * voice.PreviousReverbSend * (voice.PreviousMixGainLeft + voice.PreviousMixGainRight);
                    var currentGain = reverb.InputGain * voice.CurrentReverbSend * (voice.CurrentMixGainLeft + voice.CurrentMixGainRight);
                    WriteBlock(previousGain, currentGain, voice.Block, reverbInput);
                }
                reverb.Process(reverbInput, reverbOutputLeft, reverbOutputRight);
                ArrayMath.MultiplyAdd(masterVolume, reverbOutputLeft, blockLeft);
                ArrayMath.MultiplyAdd(masterVolume, reverbOutputRight, blockRight);
            }
        }

        private void WriteBlock(float previousGain, float currentGain, float[] source, float[] destination)
        {
            if (Math.Max(previousGain, currentGain) < SoundFontMath.NonAudible)
            {
                return;
            }

            if (MathF.Abs(currentGain - previousGain) < 1.0E-3)
            {
                ArrayMath.MultiplyAdd(currentGain, source, destination);
            }
            else
            {
                var step = inverseBlockSize * (currentGain - previousGain);
                ArrayMath.MultiplyAdd(previousGain, step, source, destination);
            }
        }

        /// <summary>
        /// The block size for waveform rendering.
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
        /// Gets or sets the master volume.
        /// </summary>
        public float MasterVolume {
            get => masterVolume;
            set => masterVolume = value;
        }

        internal int MinimumVoiceDuration => minimumVoiceDuration;
        internal Channel[] Channels => channels;
    }
}
