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
    /// Play, Stop and recursive Render are rejected while Render is active, including
    /// inside OnSendMessage. Issue playback changes after Render returns.
    /// </remarks>
    public sealed class MidiFileSequencer : IAudioRenderer
    {
        private readonly Synthesizer synthesizer;

        private float speed;

        private MidiFile? midiFile;
        private bool loop;

        private int blockWrote;

        private TimeSpan currentTime;
        // Every supported binary32 speed is an integer multiple of 2^-30.
        // Keep the denominator fixed across speed changes and loop rewinds.
        private const ulong SpeedUnits = 1UL << 30;
        private readonly ulong clockDenominator;
        private ulong clockRemainder;
        private long blockTicks;
        private ulong blockRemainder;
        private int msgIndex;
        private int loopIndex;

        private MessageHook? onSendMessage;
        private bool rendering;

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

            clockDenominator = (ulong)synthesizer.SampleRate * SpeedUnits;
            Speed = 1F;
        }

        /// <summary>
        /// Plays the MIDI file.
        /// </summary>
        /// <param name="midiFile">The MIDI file to be played.</param>
        /// <param name="loop">If <c>true</c>, the MIDI file loops after reaching the end.</param>
        /// <exception cref="ArgumentException">
        /// Loop playback was requested, but the effective loop does not advance time.
        /// </exception>
        /// <exception cref="InvalidOperationException">Render or its message hook is active.</exception>
        public void Play(MidiFile midiFile, bool loop)
        {
            if (midiFile == null)
            {
                throw new ArgumentNullException(nameof(midiFile));
            }

            if (loop && !midiFile.CanLoop)
            {
                throw new ArgumentException("The MIDI loop must advance playback time.", nameof(midiFile));
            }

            ThrowIfRendering();
            this.midiFile = midiFile;
            this.loop = loop;

            blockWrote = synthesizer.BlockSize;

            currentTime = TimeSpan.Zero;
            clockRemainder = 0;
            msgIndex = 0;
            loopIndex = 0;

            synthesizer.Reset();
        }

        /// <summary>
        /// Stops playing.
        /// </summary>
        /// <exception cref="InvalidOperationException">Render or its message hook is active.</exception>
        public void Stop()
        {
            ThrowIfRendering();
            midiFile = null;

            synthesizer.Reset();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Hook exceptions propagate unchanged. The attempted event is consumed and
        /// the render guard is released; later events remain pending. Commands already
        /// forwarded and any rendered prefix are not rolled back. Discard this call's
        /// output buffers after an exception; resume, Play or Stop outside the hook.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Render is already active.</exception>
        public void Render(Span<float> left, Span<float> right)
        {
            ThrowIfRendering();
            if (left.Length != right.Length)
            {
                throw new ArgumentException("The output buffers for the left and right must be the same length.");
            }

            rendering = true;
            try
            {
                var wrote = 0;
                while (wrote < left.Length)
                {
                    if (blockWrote == synthesizer.BlockSize)
                    {
                        ProcessEvents();
                        blockWrote = 0;
                        AdvanceClock();
                    }

                    var srcRem = synthesizer.BlockSize - blockWrote;
                    var dstRem = left.Length - wrote;
                    var rem = Math.Min(srcRem, dstRem);

                    synthesizer.Render(left.Slice(wrote, rem), right.Slice(wrote, rem));

                    blockWrote += rem;
                    wrote += rem;
                }
            }
            finally { rendering = false; }
        }

        private void AdvanceClock()
        {
            var ticks = blockTicks;
            clockRemainder += blockRemainder;
            if (clockRemainder >= clockDenominator)
            {
                clockRemainder -= clockDenominator;
                ticks++;
            }
            currentTime += TimeSpan.FromTicks(ticks);
        }

        private void ThrowIfRendering()
        {
            if (rendering)
                throw new InvalidOperationException("Play, Stop and nested Render cannot be called during Render or its message hook. Call them after Render returns.");
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
                        // Delivery is at-most-once even if a hook forwards and then throws.
                        // Capture this hook; a replacement takes effect on the next event.
                        msgIndex++;
                        var hook = onSendMessage;
                        if (hook == null)
                        {
                            synthesizer.ProcessMidiMessageValidated(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                        }
                        else
                        {
                            hook(synthesizer, msg.Channel, msg.Command, msg.Data1, msg.Data2);
                        }
                        continue;
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
        /// <remarks>
        /// Whole 100 ns ticks at the end of the most recently started audio block;
        /// a partial Render does not advance the clock again until the next block.
        /// Sub-tick time is retained across speed changes, pauses and loop rewinds.
        /// Play resets the clock; Stop detaches playback but retains the render clock.
        /// </remarks>
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

        /// <summary>Minimum positive playback multiplier (1/128).</summary>
        public const float MinimumSpeed = 1F / 128;

        /// <summary>Maximum playback multiplier (16).</summary>
        public const float MaximumSpeed = 16F;

        /// <summary>Gets or sets the finite playback multiplier; default 1.</summary>
        /// <remarks>
        /// Accepts zero or MinimumSpeed through MaximumSpeed inclusive. Zero freezes
        /// MIDI event time; voices/effects still render, so this is not mute or an
        /// audio pause. Changes apply at the next sequencer block boundary.
        /// The caller must serialize control changes with rendering.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside the supported range.</exception>
        public float Speed
        {
            get => speed;
            set
            {
                if (!float.IsFinite(value) || (value != 0 && (value < MinimumSpeed || value > MaximumSpeed)))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Speed must be zero or between 1/128 and 16, and finite.");
                if (value == speed) return;
                // Setup only: exact binary32 scaling, with wide arithmetic for the
                // largest block/speed product. No division or floating clock work per block.
                var units = (ulong)((double)value * SpeedUnits);
                var numerator = (UInt128)units * (uint)synthesizer.BlockSize * TimeSpan.TicksPerSecond;
                blockTicks = (long)(numerator / clockDenominator);
                blockRemainder = (ulong)(numerator % clockDenominator);
                speed = value;
            }
        }

        /// <summary>
        /// Gets or sets the method for modifying MIDI messages during playback.
        /// If <c>null</c>, MIDI messages are sent to the synthesizer without any changes.
        /// </summary>
        /// <remarks>
        /// A hook may modify/forward synthesizer MIDI commands, change Speed or replace
        /// this hook (effective from the next event). Play, Stop and nested Render are
        /// rejected before mutation. A throwing hook consumes its event once, retains
        /// its completed changes and propagates its exception; it is not retried.
        /// The hook and synthesizer remain single-owner; this is not a concurrency guard.
        /// </remarks>
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
