using System;
using NAudio.Wave;
using MeltySynth;

public class MidiSampleProvider : ISampleProvider
{
    private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private Synthesizer synthesizer;
    private float[] left;
    private float[] right;

    private int blockRead;

    private MidiFileSequencer sequencer;

    private object mutex;

    public MidiSampleProvider(string soundFontPath)
    {
        synthesizer = new Synthesizer(soundFontPath, format.SampleRate);
        left = new float[synthesizer.BlockSize];
        right = new float[synthesizer.BlockSize];

        blockRead = synthesizer.BlockSize;

        mutex = new object();
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        lock (mutex)
        {
            sequencer = new MidiFileSequencer(synthesizer);
            sequencer.Play(midiFile, loop);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (mutex)
        {
            if (sequencer == null)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }

            var dstLength = count / 2;

            var wrote = 0;
            while (wrote < dstLength)
            {
                if (blockRead == synthesizer.BlockSize)
                {
                    sequencer.ProcessEvents();
                    synthesizer.Render(left, right);
                    blockRead = 0;
                }

                var srcRem = synthesizer.BlockSize - blockRead;
                var dstRem = dstLength - wrote;
                var rem = Math.Min(srcRem, dstRem);

                for (var i = 0; i < rem; i++)
                {
                    var t = 2 * (wrote + i);
                    buffer[t] = left[blockRead + i];
                    buffer[t + 1] = right[blockRead + i];
                }

                blockRead += rem;
                wrote += rem;
            }
        }

        return count;
    }

    public WaveFormat WaveFormat => format;
}
