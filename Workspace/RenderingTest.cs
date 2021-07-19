using System;
using System.Linq;
using NAudio.Wave;
using MeltySynth;

public static class RenderingTest
{
    public static void SimpleChord()
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        // The output buffer (3 seconds).
        var left = new float[3 * sampleRate];
        var right = new float[3 * sampleRate];

        // Play some notes (middle C, E, G).
        synthesizer.NoteOn(0, 60, 100);
        synthesizer.NoteOn(0, 64, 100);
        synthesizer.NoteOn(0, 67, 100);

        // Render the waveform.
        synthesizer.Render(left, right);

        WriteWaveFile(left, right, sampleRate, "SimpleChord.wav");
    }

    public static void Flourish()
    {
        // Create the synthesizer.
        var sampleRate = 44100;
        var synthesizer = new Synthesizer("TimGM6mb.sf2", sampleRate);

        // Read the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");
        var sequencer = new MidiFileSequencer(synthesizer);
        sequencer.Play(midiFile, false);

        // The output buffer.
        var left = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];
        var right = new float[(int)(sampleRate * midiFile.Length.TotalSeconds)];

        var midiEventInterval = 100;

        for (var t = 0; t < left.Length; t += midiEventInterval)
        {
            sequencer.ProcessEvents();

            var spanLength = Math.Min(t + midiEventInterval, left.Length) - t;
            var spanLeft = left.AsSpan(t, spanLength);
            var spanRight = right.AsSpan(t, spanLength);
            synthesizer.Render(spanLeft, spanRight);
        }

        WriteWaveFile(left, right, sampleRate, "Flourish.wav");
    }

    private static void WriteWaveFile(float[] left, float[] right, int sampleRate, string path)
    {
        var leftMax = left.Max(x => Math.Abs(x));
        var rightMax = right.Max(x => Math.Abs(x));
        var a = 0.99F / Math.Max(leftMax, rightMax);

        var format = new WaveFormat(sampleRate, 16, 2);
        using (var writer = new WaveFileWriter(path, format))
        {
            for (var t = 0; t < left.Length; t++)
            {
                writer.WriteSample(a * left[t]);
                writer.WriteSample(a * right[t]);
            }
        }
    }
}
