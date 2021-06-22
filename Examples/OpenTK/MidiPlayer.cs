using System;
using OpenTK.Audio.OpenAL;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private Synthesizer synthesizer;

    private int blocksPerBatch;
    private int batchLength;

    private float[] left;
    private float[] right;

    private MidiFileSequencer sequencer;

    private SoundStream stream;

    private object mutex;

    private bool started;

    public MidiPlayer(string soundFontPath)
    {
        var settings = new SynthesizerSettings(44100);
        synthesizer = new Synthesizer(soundFontPath, settings);

        blocksPerBatch = 2048 / settings.BlockSize;
        batchLength = synthesizer.BlockSize * blocksPerBatch;

        left = new float[synthesizer.BlockSize];
        right = new float[synthesizer.BlockSize];

        sequencer = new MidiFileSequencer(synthesizer);

        stream = new SoundStream(44100, 2, batchLength, FillBuffer);

        mutex = new object();

        started = false;
    }

    private void FillBuffer(short[] data)
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

                    data[t++] = (short)sampleLeft;
                    data[t++] = (short)sampleRight;
                }
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
