using System;
using System.Collections.Generic;
using DotFeather;
using MeltySynth;

public class MidiAudioStream : IAudioSource
{
    private Synthesizer synthesizer;
    private MidiFileSequencer sequencer;

    private float[] bufferLeft;
    private float[] bufferRight;

    private object mutex;

    public MidiAudioStream(string soundFontPath)
    {
        synthesizer = new Synthesizer(soundFontPath, SampleRate);
        sequencer = new MidiFileSequencer(synthesizer);

        bufferLeft = new float[SampleRate / 20];
        bufferRight = new float[SampleRate / 20];

        mutex = new object();
    }

    public IEnumerable<(short left, short right)> EnumerateSamples(int? loopStart)
    {
        while (true)
        {
            lock (mutex)
            {
                sequencer.Render(bufferLeft, bufferRight);
            }

            for (var t = 0; t < bufferLeft.Length; t++)
            {
                var sampleLeft = Math.Clamp((int)(32768 * bufferLeft[t]), short.MinValue, short.MaxValue);
                var sampleRight = Math.Clamp((int)(32768 * bufferRight[t]), short.MinValue, short.MaxValue);

                yield return ((short)sampleLeft, (short)sampleRight);
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
