using System;
using System.Diagnostics;
using NAudio.Wave;
using MeltySynth;

public static class Benchmark
{
    public static TimeSpan Run(string soundFontPath, string midiFilePath, string outputPath, int outputLengthSec, SynthesizerSettings settings)
    {
        var synthesizer = new Synthesizer(soundFontPath, settings);

        var midiFile = new MidiFile(midiFilePath);
        var sequencer = new MidiFileSequencer(synthesizer);
        sequencer.Play(midiFile, false);

        var left = new float[outputLengthSec * settings.SampleRate];
        var right = new float[outputLengthSec * settings.SampleRate];

        var midiEventInterval = synthesizer.BlockSize;

        var sw = new Stopwatch();
        sw.Start();

        sequencer.Render(left, right);

        sw.Stop();

        if (outputPath != null)
        {
            WriteWaveFile(left, right, settings.SampleRate, outputPath);
        }

        return sw.Elapsed;
    }

    private static void WriteWaveFile(float[] left, float[] right, int sampleRate, string path)
    {
        var format = new WaveFormat(sampleRate, 16, 2);
        using (var writer = new WaveFileWriter(path, format))
        {
            for (var t = 0; t < left.Length; t++)
            {
                writer.WriteSample(left[t]);
                writer.WriteSample(right[t]);
            }
        }
    }
}
