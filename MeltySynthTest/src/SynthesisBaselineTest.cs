// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class SynthesisBaselineTest
    {
        private const double SampleTolerance = 1E-6;
        private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        [TestCase(false)]
        [TestCase(true)]
        public void SilenceOverwritesTheEntireDestination(bool effects)
        {
            var synth = SynthesisBaselineScenario.Synth(effects);
            var left = new float[513]; var right = new float[513];
            Array.Fill(left, float.NaN); Array.Fill(right, float.NaN);
            synth.Render(left, right);
            Assert.That(left, Is.All.Zero); Assert.That(right, Is.All.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResetClearsVoicesEffectsAndPartialBlocks(bool effects)
        {
            var synth = SynthesisBaselineScenario.Synth(effects);
            var left = new float[513]; var right = new float[513];
            synth.NoteOn(0, 60, 100); synth.Render(left, right);
            Assert.That(SynthesisBaselineScenario.Metrics(left, right)[0], Is.GreaterThan(0));
            synth.Reset(); synth.Render(left, right);
            Assert.That(left, Is.All.Zero); Assert.That(right, Is.All.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FragmentedRenderingPreservesLoopedStereoOutput(bool effects)
        {
            var expected = SynthesisBaselineScenario.Render(effects, false);
            var actual = SynthesisBaselineScenario.Render(effects, true);
            double error = 0;
            for (var i = 0; i < actual.Left.Length; i++)
            {
                error = Math.Max(error, Math.Abs(actual.Left[i] - expected.Left[i]));
                error = Math.Max(error, Math.Abs(actual.Right[i] - expected.Right[i]));
            }
            Assert.That(error, Is.LessThanOrEqualTo(SampleTolerance));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OriginalScoreMatchesFrozenNumericalBaseline(bool effects)
        {
            using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Baselines", "synthesis.json")));
            var root = data.RootElement;
            Assert.That(root.GetProperty("soundFontSha256").GetString(), Is.EqualTo(Hash(MidiLoopTestAssets.SoundFontBytes())));
            Assert.That(root.GetProperty("midiSha256").GetString(), Is.EqualTo(Hash(SynthesisBaselineScenario.MidiBytes())));
            Assert.That(root.GetProperty("frames").GetInt32(), Is.EqualTo(SynthesisBaselineScenario.Frames));
            Assert.That(root.GetProperty("sampleRate").GetInt32(), Is.EqualTo(SynthesisBaselineScenario.SampleRate));
            Assert.That(root.GetProperty("blockSize").GetInt32(), Is.EqualTo(SynthesisBaselineScenario.BlockSize));
            Assert.That(root.GetProperty("polyphony").GetInt32(), Is.EqualTo(SynthesisBaselineScenario.Polyphony));
            Assert.That(root.GetProperty("masterVolume").GetDouble(), Is.EqualTo(SynthesisBaselineScenario.MasterVolume));
            var expected = root.GetProperty("scenarios")[effects ? 1 : 0];
            Assert.That(expected.GetProperty("effects").GetBoolean(), Is.EqualTo(effects));
            var output = SynthesisBaselineScenario.Render(effects, false);
            Assert.That(output.Left, Has.All.Matches<float>(float.IsFinite));
            Assert.That(output.Right, Has.All.Matches<float>(float.IsFinite));
            var metrics = SynthesisBaselineScenario.Metrics(output.Left, output.Right);
            Assert.That(metrics[0], Is.GreaterThan(0)); Assert.That(metrics[1], Is.GreaterThan(0));
            for (var i = 0; i < metrics.Length; i++)
            {
                var value = expected.GetProperty("metrics")[i].GetDouble();
                Assert.That(metrics[i], Is.EqualTo(value).Within(Math.Max(1E-6, Math.Abs(value) * 1E-6)), "Metric " + i);
            }
            foreach (var point in expected.GetProperty("checkpoints").EnumerateArray())
            {
                var frame = point.GetProperty("frame").GetInt32();
                Assert.That(output.Left[frame], Is.EqualTo(point.GetProperty("left").GetDouble()).Within(SampleTolerance), "Left frame " + frame);
                Assert.That(output.Right[frame], Is.EqualTo(point.GetProperty("right").GetDouble()).Within(SampleTolerance), "Right frame " + frame);
            }
        }
    }
}
