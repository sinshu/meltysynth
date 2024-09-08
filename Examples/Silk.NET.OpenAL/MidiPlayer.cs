using System;
using Silk.NET.OpenAL;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private object mutex;
    private AudioStream? stream;

    public MidiPlayer(AL al, string soundFontPath)
    {
        var settings = new SynthesizerSettings(44100);

        synthesizer = new Synthesizer(soundFontPath, settings);
        sequencer = new MidiFileSequencer(synthesizer);

        mutex = new object();
        stream = new AudioStream(al, settings.SampleRate, 2);
    }

    private void FillBlock(short[] data)
    {
        lock (mutex)
        {
            sequencer.RenderInterleavedInt16(data);
        }
    }

    public void Play(MidiFile midiFile, bool loop)
    {
        if (stream == null)
        {
            throw new ObjectDisposedException(nameof(MidiPlayer));
        }

        lock (mutex)
        {
            sequencer.Play(midiFile, loop);
        }

        if (stream.State != PlaybackState.Playing)
        {
            stream.Play(FillBlock);
        }
    }

    public void Stop()
    {
        if (stream == null)
        {
            throw new ObjectDisposedException(nameof(MidiPlayer));
        }

        lock (mutex)
        {
            sequencer.Stop();
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
