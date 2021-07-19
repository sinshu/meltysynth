using System;
using SFML.Audio;
using SFML.System;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private MidiSoundStream stream;

    public MidiPlayer(string soundFontPath)
    {
        stream = new MidiSoundStream(soundFontPath, 44100);
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        stream.SetMidiFile(midiFile, loop);
    }

    public void Dispose()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }



    private class MidiSoundStream : SoundStream
    {
        private Synthesizer synthesizer;
        private float[] left;
        private float[] right;

        private MidiFileSequencer sequencer;

        private int blocksPerBatch;
        private int batchLength;
        private short[] batch;

        private object mutex;

        public MidiSoundStream(string soundFontPath, int sampleRate)
        {
            synthesizer = new Synthesizer(soundFontPath, sampleRate);
            left = new float[synthesizer.BlockSize];
            right = new float[synthesizer.BlockSize];

            sequencer = new MidiFileSequencer(synthesizer);

            var blockDuration = (double)synthesizer.BlockSize / synthesizer.SampleRate;
            blocksPerBatch = (int)Math.Ceiling(0.05 / blockDuration);
            batchLength = synthesizer.BlockSize * blocksPerBatch;
            batch = new short[2 * batchLength];

            mutex = new object();

            Initialize(2, (uint)sampleRate);
        }

        public void SetMidiFile(MidiFile midiFile, bool loop)
        {
            lock (mutex)
            {
                sequencer.Play(midiFile, loop);
            }

            if (Status == SoundStatus.Stopped)
            {
                Play();
            }
        }

        protected override bool OnGetData(out short[] samples)
        {
            lock (mutex)
            {
                var t = 0;

                for (var i = 0; i < blocksPerBatch; i++)
                {
                    sequencer.ProcessEvents();
                    synthesizer.Render(left, right);

                    for (var j = 0; j < left.Length; j++)
                    {
                        var sampleLeft = (int)(32768 * left[j]);
                        if (sampleLeft < short.MinValue)
                        {
                            sampleLeft = short.MinValue;
                        }
                        else if (sampleLeft > short.MaxValue)
                        {
                            sampleLeft = short.MaxValue;
                        }

                        var sampleRight = (int)(32768 * right[j]);
                        if (sampleRight < short.MinValue)
                        {
                            sampleRight = short.MinValue;
                        }
                        else if (sampleRight > short.MaxValue)
                        {
                            sampleRight = short.MaxValue;
                        }

                        batch[t++] = (short)sampleLeft;
                        batch[t++] = (short)sampleRight;
                    }
                }

                samples = batch;
            }

            return true;
        }

        protected override void OnSeek(Time timeOffset)
        {
            throw new NotSupportedException();
        }
    }
}
