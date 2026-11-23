// SPDX-License-Identifier: MIT
// Copyright and inherited upstream notices: see LICENSE.txt.

using System;
using System.Diagnostics;
using MeltySynth;

public static class Benchmark
{
    public static TimeSpan Run(string soundFontPath, string midiFilePath, int outputLengthSec, SynthesizerSettings settings, string? outputPath)
    {
        if (outputLengthSec < 1 || outputLengthSec > 300)
            throw new ArgumentOutOfRangeException(nameof(outputLengthSec), "Use 1–300 seconds for the offline benchmark.");
        var synthesizer = new Synthesizer(soundFontPath, settings);

        var midiFile = new MidiFile(midiFilePath);
        var sequencer = new MidiFileSequencer(synthesizer);
        sequencer.Play(midiFile, false);

        var left = new float[outputLengthSec * settings.SampleRate];
        var right = new float[outputLengthSec * settings.SampleRate];

        var sw = new Stopwatch();
        sw.Start();

        sequencer.Render(left, right);

        sw.Stop();

        if (outputPath != null)
        {
            Examples.WriteWaveFile(left, right, settings.SampleRate, outputPath);
        }

        return sw.Elapsed;
    }

}
