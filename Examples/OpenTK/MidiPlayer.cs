﻿using System;
using OpenTK.Audio.OpenAL;
using MeltySynth;

public class MidiPlayer : IDisposable
{
    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private object mutex;
    private AudioStream? stream;

    public MidiPlayer(string soundFontPath)
    {
        var settings = new SynthesizerSettings(44100);

        synthesizer = new Synthesizer(soundFontPath, settings);
        sequencer = new MidiFileSequencer(synthesizer);

        stream = new AudioStream(settings.SampleRate, 2);
        mutex = new object();
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
