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
        private MidiFileSequencer sequencer;

        private int batchLength;
        private float[] left;
        private float[] right;
        private short[] batch;

        private object mutex;

        public MidiSoundStream(string soundFontPath, int sampleRate)
        {
            synthesizer = new Synthesizer(soundFontPath, sampleRate);
            sequencer = new MidiFileSequencer(synthesizer);

            batchLength = (int)Math.Round(0.05 * sampleRate);
            left = new float[batchLength];
            right = new float[batchLength];
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
                sequencer.Render(left, right);

                var pos = 0;

                for (var t = 0; t < batchLength; t++)
                {
                    var sampleLeft = (int)(32768 * left[t]);
                    if (sampleLeft < short.MinValue)
                    {
                        sampleLeft = short.MinValue;
                    }
                    else if (sampleLeft > short.MaxValue)
                    {
                        sampleLeft = short.MaxValue;
                    }

                    var sampleRight = (int)(32768 * right[t]);
                    if (sampleRight < short.MinValue)
                    {
                        sampleRight = short.MinValue;
                    }
                    else if (sampleRight > short.MaxValue)
                    {
                        sampleRight = short.MaxValue;
                    }

                    batch[pos++] = (short)sampleLeft;
                    batch[pos++] = (short)sampleRight;
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
