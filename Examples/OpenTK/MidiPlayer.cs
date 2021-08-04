using System;
using OpenTK.Audio.OpenAL;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private static readonly int bufferLength = 2048;

    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private float[] left;
    private float[] right;

    private SoundStream stream;

    private object mutex;

    private bool started;

    public MidiPlayer(string soundFontPath)
    {
        var settings = new SynthesizerSettings(44100);

        synthesizer = new Synthesizer(soundFontPath, settings);
        sequencer = new MidiFileSequencer(synthesizer);

        left = new float[bufferLength];
        right = new float[bufferLength];

        stream = new SoundStream(settings.SampleRate, 2, bufferLength, FillBuffer);

        mutex = new object();

        started = false;
    }

    private void FillBuffer(short[] data)
    {
        lock (mutex)
        {
            sequencer.Render(left, right);

            var pos = 0;

            for (var t = 0; t < bufferLength; t++)
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

                data[pos++] = (short)sampleLeft;
                data[pos++] = (short)sampleRight;
            }
        }
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        lock (mutex)
        {
            sequencer.Play(midiFile, loop);
        }

        if (!started)
        {
            stream.Start();
            started = true;
        }
    }

    public void Dispose()
    {
        if (stream != null)
        {
            stream.Dispose();
            stream = null;
        }
    }
}
