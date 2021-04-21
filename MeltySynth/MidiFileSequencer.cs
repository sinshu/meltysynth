using System;

namespace MeltySynth
{
    public sealed class MidiFileSequencer
    {
        private Synthesizer synthesizer;

        private MidiFile midiFile;
        private bool loop;
        private long startSampleCount;

        private int ptr;

        private long previousSampleCount;
        private double previousTick;
        private int tempo;

        public MidiFileSequencer(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        public void Play(MidiFile midiFile, bool loop)
        {
            this.midiFile = midiFile;
            this.loop = loop;
            this.startSampleCount = synthesizer.ProcessedSampleCount;

            ptr = 0;

            previousSampleCount = startSampleCount;
            previousTick = 0;
            tempo = 140;

            synthesizer.Reset();
        }

        public void ProcessEvents()
        {
            var currentSampleCount = synthesizer.ProcessedSampleCount;
            var deltaTime = (double)(currentSampleCount - previousSampleCount) / synthesizer.SampleRate;
            var deltaTick = (double)(midiFile.Resolution * tempo) / 60 * deltaTime;
            var currentTick = previousTick + deltaTick;

            while (midiFile.Ticks[ptr] <= currentTick)
            {
                if (ptr < midiFile.Messages.Length)
                {
                    if (midiFile.Messages[ptr].Type == MidiFile.MessageType.Normal)
                    {
                        var msg = midiFile.Messages[ptr];
                        synthesizer.ProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                    }
                    ptr++;
                }
                else
                {
                    break;
                }
            }

            previousSampleCount = currentSampleCount;
            previousTick = currentTick;
        }
    }
}
