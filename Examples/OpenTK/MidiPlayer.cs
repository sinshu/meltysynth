using System;
using OpenTK.Audio.OpenAL;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private static readonly int bufferLength = 2048;

    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private SoundStream stream;
    private object mutex;
    private bool started;

    public MidiPlayer(string soundFontPath)
    {
        var settings = new SynthesizerSettings(44100);

        synthesizer = new Synthesizer(soundFontPath, settings);
        sequencer = new MidiFileSequencer(synthesizer);

        stream = new SoundStream(settings.SampleRate, 2, bufferLength, FillBuffer);
        mutex = new object();
        started = false;
    }

    private void FillBuffer(short[] data)
    {
        lock (mutex)
        {
            sequencer.RenderInterleavedInt16(data);
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

    public void Stop()
    {
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
