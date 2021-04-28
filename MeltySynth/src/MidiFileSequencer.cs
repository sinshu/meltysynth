using System;

namespace MeltySynth
{
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

        public MidiFileSequencer(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

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

        public void ProcessEvents()
        {
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
