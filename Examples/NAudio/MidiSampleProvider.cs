using System;
using NAudio.Wave;
using MeltySynth;

public class MidiSampleProvider : ISampleProvider
{
    private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private object mutex;

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

    public void Stop()
    {
        lock (mutex)
        {
            sequencer.Stop();
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        lock (mutex)
        {
            sequencer.RenderInterleaved(buffer.AsSpan(offset, count));
        }

        return count;
    }

    public WaveFormat WaveFormat => format;
}
