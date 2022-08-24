using System;
using System.Collections.Generic;
using DotFeather;
using MeltySynth;

public class MidiAudioStream : IAudioSource
{
    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private short[] bufferLeft;
    private short[] bufferRight;

    private object mutex;

    public MidiAudioStream(string soundFontPath)
    {
        synthesizer = new Synthesizer(soundFontPath, SampleRate);
        sequencer = new MidiFileSequencer(synthesizer);

        bufferLeft = new short[SampleRate / 20];
        bufferRight = new short[SampleRate / 20];

        mutex = new object();
    }

    public IEnumerable<(short left, short right)> EnumerateSamples(int? loopStart)
    {
        while (true)
        {
            lock (mutex)
            {
                sequencer.RenderInt16(bufferLeft, bufferRight);
            }

            for (var t = 0; t < bufferLeft.Length; t++)
            {
                yield return (bufferLeft[t], bufferRight[t]);
            }
        }
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

    public int? Samples => null;
    public int Channels => 2;
    public int Bits => 16;
    public int SampleRate => 44100;
}
