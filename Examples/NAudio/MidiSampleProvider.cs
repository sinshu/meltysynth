using System;
using NAudio.Wave;
using MeltySynth;

public class MidiSampleProvider : ISampleProvider
{
    private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private object mutex;

    private float[] left;
    private float[] right;

    public MidiSampleProvider(string soundFontPath)
    {
        synthesizer = new Synthesizer(soundFontPath, format.SampleRate);
        sequencer = new MidiFileSequencer(synthesizer);

        mutex = new object();
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        lock (mutex)
        {
            sequencer.Play(midiFile, loop);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (mutex)
        {
            var dstLength = count / 2;

            if (left == null)
            {
                left = new float[dstLength];
                right = new float[dstLength];
            }

            if (dstLength > left.Length)
            {
                Array.Resize(ref left, dstLength);
                Array.Resize(ref right, dstLength);
            }

            sequencer.Render(left, right);

            var pos = offset;
            for (var t = 0; t < dstLength; t++)
            {
                buffer[pos++] = left[t];
                buffer[pos++] = right[t];
            }
        }

        return count;
    }

    public WaveFormat WaveFormat => format;
}
