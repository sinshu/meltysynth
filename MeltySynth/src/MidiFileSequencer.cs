using System;

namespace MeltySynth
{
    /// <summary>
    /// An instance of the MIDI file sequencer.
    /// </summary>
    /// <remarks>
    /// Note that this class does not provide thread safety.
    /// If you want to control playback and render the waveform in separate threads,
    /// you must make sure that the methods are not called at the same time.
    /// </remarks>
    public sealed class MidiFileSequencer : IAudioRenderer
    {
        private readonly Synthesizer synthesizer;

        private float speed;

        private MidiFile? midiFile;
        private bool loop;

        private int blockWrote;

        private TimeSpan currentTime;
        private int msgIndex;
        private int loopIndex;

        private MessageHook? onSendMessage;

        /// <summary>
        /// Initializes a new instance of the sequencer.
        /// </summary>
        /// <param name="synthesizer">The synthesizer to be used by the sequencer.</param>
        public MidiFileSequencer(Synthesizer synthesizer)
        {
            if (synthesizer == null)
            {
                throw new ArgumentNullException(nameof(synthesizer));
            }

            this.synthesizer = synthesizer;

            speed = 1F;
        }

        /// <summary>
        /// Plays the MIDI file.
        /// </summary>
        /// <param name="midiFile">The MIDI file to be played.</param>
        /// <param name="loop">If <c>true</c>, the MIDI file loops after reaching the end.</param>
        public void Play(MidiFile midiFile, bool loop)
        {
            if (midiFile == null)
            {
                throw new ArgumentNullException(nameof(midiFile));
            }

            this.midiFile = midiFile;
            this.loop = loop;

            blockWrote = synthesizer.BlockSize;

            currentTime = TimeSpan.Zero;
            msgIndex = 0;
            loopIndex = 0;

            synthesizer.Reset();
        }

        /// <summary>
        /// Stops playing.
        /// </summary>
        public void Stop()
        {
            midiFile = null;

            synthesizer.Reset();
        }

        /// <inheritdoc/>
        public void Render(Span<float> left, Span<float> right)
        {
            if (left.Length != right.Length)
            {
                throw new ArgumentException("The output buffers for the left and right must be the same length.");
            }

            var wrote = 0;
            while (wrote < left.Length)
            {
                if (blockWrote == synthesizer.BlockSize)
                {
                    ProcessEvents();
                    blockWrote = 0;
                    currentTime += MidiFile.GetTimeSpanFromSeconds((double)speed * synthesizer.BlockSize / synthesizer.SampleRate);
                }

                var srcRem = synthesizer.BlockSize - blockWrote;
                var dstRem = left.Length - wrote;
                var rem = Math.Min(srcRem, dstRem);

                synthesizer.Render(left.Slice(wrote, rem), right.Slice(wrote, rem));

                blockWrote += rem;
                wrote += rem;
            }
        }

        private void ProcessEvents()
        {
            if (midiFile == null)
            {
                return;
            }

            while (msgIndex < midiFile.Messages.Length)
            {
                var time = midiFile.Times[msgIndex];
                var msg = midiFile.Messages[msgIndex];
                if (time <= currentTime)
                {
                    if (msg.Type == MidiFile.MessageType.Normal)
                    {
                        if (onSendMessage == null)
                        {
                            synthesizer.ProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                        }
                        else
                        {
                            onSendMessage(synthesizer, msg.Channel, msg.Command, msg.Data1, msg.Data2);
                        }
                    }
                    else if (loop)
                    {
                        if (msg.Type == MidiFile.MessageType.LoopStart)
                        {
                            loopIndex = msgIndex;
                        }
                        else if (msg.Type == MidiFile.MessageType.LoopEnd)
                        {
                            currentTime = midiFile.Times[loopIndex];
                            msgIndex = loopIndex;
                            synthesizer.NoteOffAll(false);
                        }
                    }
                    msgIndex++;
                }
                else
                {
                    break;
                }
            }

            if (msgIndex == midiFile.Messages.Length && loop)
            {
                currentTime = midiFile.Times[loopIndex];
                msgIndex = loopIndex;
                synthesizer.NoteOffAll(false);
            }
        }

        /// <summary>
        /// Gets the synthesizer used by the sequencer.
        /// </summary>
        public Synthesizer Synthesizer => synthesizer;

        /// <summary>
        /// Gets the currently playing MIDI file.
        /// </summary>
        public MidiFile? MidiFile => midiFile;

        /// <summary>
        /// Gets the current playback position.
        /// </summary>
        public TimeSpan Position => currentTime;

        /// <summary>
        /// Gets a value that indicates whether the current playback position is at the end of the sequence.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Play(MidiFile, bool)">Play</see> method has not yet been called, this value is true.
        /// This value will never be <c>true</c> when loop playback is enabled.
        /// </remarks>
        public bool EndOfSequence
        {
            get
            {
                if (midiFile == null)
                {
                    return true;
                }
                else
                {
                    return msgIndex == midiFile.Messages.Length;
                }
            }
        }

        /// <summary>
        /// Gets or sets the playback speed.
        /// </summary>
        /// <remarks>
        /// The default value is 1.
        /// The tempo will be multiplied by this value.
        /// </remarks>
        public float Speed
        {
            get => speed;

            set
            {
                if (value >= 0)
                {
                    speed = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("The playback speed must be a non-negative value.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the method for modifying MIDI messages during playback.
        /// If <c>null</c>, MIDI messages are sent to the synthesizer without any changes.
        /// </summary>
        public MessageHook? OnSendMessage
        {
            get => onSendMessage;
            set => onSendMessage = value;
        }



        /// <summary>
        /// Represents the method that is called each time a MIDI message is processed during playback.
        /// </summary>
        /// <param name="synthesizer">The synthesizer used by the sequencer.</param>
        /// <param name="channel">The channel to which the message will be sent.</param>
        /// <param name="command">The type of the message.</param>
        /// <param name="data1">The first data part of the message.</param>
        /// <param name="data2">The second data part of the message.</param>
        public delegate void MessageHook(Synthesizer synthesizer, int channel, int command, int data1, int data2);
    }
}
