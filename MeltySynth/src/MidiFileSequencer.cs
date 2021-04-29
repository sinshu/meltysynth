using System;

namespace MeltySynth
{
    /// <summary>
    /// An instance of the MIDI file sequencer.
    /// </summary>
    public sealed class MidiFileSequencer
    {
        private readonly Synthesizer synthesizer;

        private MidiFile midiFile;
        private bool loop;
        private long startSampleCount;

        private int msgIndex;
        private TimeSpan previousTime;
        private double previousTick;
        private double tempo;

        /// <summary>
        /// Initializes a new instance of the sequencer.
        /// </summary>
        /// <param name="synthesizer">The synthesizer to be handled by the sequencer.</param>
        public MidiFileSequencer(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
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
            this.startSampleCount = synthesizer.ProcessedSampleCount;

            msgIndex = 0;
            previousTime = TimeSpan.Zero;
            previousTick = 0;
            tempo = 120;

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

            var sampleCount = synthesizer.ProcessedSampleCount - startSampleCount;
            var targetTime = TimeSpan.FromSeconds((double)sampleCount / synthesizer.SampleRate);

            while (msgIndex < midiFile.Messages.Length)
            {
                var currentTick = midiFile.Ticks[msgIndex];
                var deltaTick = currentTick - previousTick;
                var currentTime = previousTime + TimeSpan.FromSeconds(60.0 / (midiFile.Resolution * tempo) * deltaTick);

                previousTime = currentTime;
                previousTick = currentTick;

                if (currentTime <= targetTime)
                {
                    var msg = midiFile.Messages[msgIndex];
                    if (msg.Type == MidiFile.MessageType.Normal)
                    {
                        synthesizer.ProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                    }
                    else if (msg.Type == MidiFile.MessageType.TempoChange)
                    {
                        tempo = msg.Tempo;
                    }
                    msgIndex++;
                }
                else
                {
                    return;
                }
            }

            if (loop)
            {
                var currentTick = midiFile.Ticks[msgIndex];
                var deltaTick = currentTick - previousTick;
                var endTime = previousTime + TimeSpan.FromSeconds(60.0 / (midiFile.Resolution * tempo) * deltaTick);

                if (targetTime >= endTime)
                {
                    startSampleCount = synthesizer.ProcessedSampleCount + synthesizer.BlockSize;
                    msgIndex = 0;
                    previousTime = TimeSpan.Zero;
                    previousTick = 0;
                }
            }
        }
    }
}
