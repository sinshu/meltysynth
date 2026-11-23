// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;
using MeltySynth;

namespace MeltySynthTest
{
    // Original test content. No game, downloaded bank, audio device or external path.
    internal static class SynthesisBaselineScenario
    {
        internal const int SampleRate = 32000, BlockSize = 64, Polyphony = 8, Frames = 32768;
        internal const float MasterVolume = .5F;
        internal static readonly int[] Checkpoints = { 0, 1, 63, 64, 511, 1023, 4095, 8191, 16383, 32767 };
        internal static byte[] MidiBytes() => MidiLoopTestAssets.MidiBytes(MidiLoopTestAssets.Track(
            (0, new byte[] { 0xB0, 10, 40 }), (0, new byte[] { 0x90, 60, 100 }),
            (24, new byte[] { 0xB0, 10, 88 }), (0, new byte[] { 0x90, 67, 80 }),
            (48, new byte[] { 0xB0, 123, 0 }), (24, new byte[] { 0xFF, 0x2F, 0 })));
        internal static Synthesizer Synth(bool effects) => new Synthesizer(MidiLoopTestAssets.SoundFont(),
            new SynthesizerSettings(SampleRate) { BlockSize = BlockSize, MaximumPolyphony = Polyphony, EnableReverbAndChorus = effects }) { MasterVolume = MasterVolume };
        internal static (float[] Left, float[] Right) Render(bool effects, bool fragmented)
        {
            var sequence = new MidiFileSequencer(Synth(effects));
            using var stream = new MemoryStream(MidiBytes(), false);
            sequence.Play(new MidiFile(stream), true);
            var left = new float[Frames]; var right = new float[Frames];
            if (!fragmented) sequence.Render(left, right);
            else
            {
                var sizes = new[] { 1, 63, 65, 257, 511 };
                for (int at = 0, i = 0; at < Frames; i++)
                {
                    var count = Math.Min(sizes[i % sizes.Length], Frames - at);
                    sequence.Render(left.AsSpan(at, count), right.AsSpan(at, count));
                    at += count;
                }
            }
            return (left, right);
        }
        internal static double[] Metrics(float[] left, float[] right)
        {
            double energyLeft = 0, energyRight = 0, peakLeft = 0, peakRight = 0;
            for (var i = 0; i < left.Length; i++)
            {
                energyLeft += (double)left[i] * left[i]; energyRight += (double)right[i] * right[i];
                peakLeft = Math.Max(peakLeft, Math.Abs(left[i])); peakRight = Math.Max(peakRight, Math.Abs(right[i]));
            }
            return new[] { energyLeft, energyRight, peakLeft, peakRight };
        }
    }
}
