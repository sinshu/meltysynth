using System;
using System.Runtime.InteropServices;
using CSCore;
using MeltySynth;

public class MidiWaveSource : IWaveSource
{
    private static WaveFormat format = new WaveFormat(44100, 16, 2);

    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private object mutex;

    public MidiWaveSource(string soundFontPath)
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

    public int Read(byte[] buffer, int offset, int count)
    {
        lock (mutex)
        {
            sequencer.RenderInterleavedInt16(MemoryMarshal.Cast<byte, short>(buffer.AsSpan(offset, count)));
        }

        return count;
    }

    public void Dispose()
    {
    }

    public WaveFormat WaveFormat => format;

    public bool CanSeek => false;

    public long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public long Length => throw new NotSupportedException();
}
