using System;

namespace MeltySynth
{
    /// <summary>
    /// An instance of the MIDI file sequencer.
    /// </summary>
    public sealed class MidiFileSequencer
    {
        private readonly Synthesizer synthesizer;

        private float speed;

        private MidiFile midiFile;
        private bool loop;

        private long currentSampleCount;
        private TimeSpan currentTime;
        private int msgIndex;
        private int loopIndex;

        /// <summary>
        /// Initializes a new instance of the sequencer.
        /// </summary>
        /// <param name="synthesizer">The synthesizer to be handled by the sequencer.</param>
        public MidiFileSequencer(Synthesizer synthesizer)
        {
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
            this.midiFile = midiFile;
            this.loop = loop;

            currentSampleCount = synthesizer.ProcessedSampleCount;
            currentTime = TimeSpan.Zero;
            msgIndex = 0;
            loopIndex = 0;

            synthesizer.Reset();
        }

        /// <summary>
        /// Send the MIDI events to the synthesizer.
        /// This method should be called enough frequently in the rendering process.
        /// </summary>
        public void ProcessEvents()
        {
            if (midiFile == null)
            {
                return;
            }

            var nextSampleCount = synthesizer.ProcessedSampleCount;
            var deltaSampleCount = nextSampleCount - currentSampleCount;
            var deltaTime = speed * TimeSpan.FromSeconds((double)deltaSampleCount / synthesizer.SampleRate);
            var nextTime = currentTime + deltaTime;

            while (msgIndex < midiFile.Messages.Length)
            {
                var time = midiFile.Times[msgIndex];
                var msg = midiFile.Messages[msgIndex];
                if (time <= nextTime)
                {
                    if (msg.Type == MidiFile.MessageType.Normal)
                    {
                        synthesizer.ProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                    }
                    else if (msg.Type == MidiFile.MessageType.LoopPoint)
                    {
                        loopIndex = msgIndex;
                    }
                    msgIndex++;
                }
                else
                {
                    break;
                }
            }

            currentSampleCount = nextSampleCount;
            currentTime = nextTime;

            if (msgIndex == midiFile.Messages.Length && loop)
            {
                currentTime = midiFile.Times[loopIndex];
                msgIndex = loopIndex;
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
                if (value > 0)
                {
                    speed = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("The speed must be a positive value.");
                }
            }
        }
    }
}
