// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.Collections.Generic;
using System.IO;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class ControlBoundaryTest
    {
        public static IEnumerable<float> InvalidSpeeds => new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity,
            -1F, float.MaxValue, float.Epsilon, MathF.BitDecrement(MidiFileSequencer.MinimumSpeed), MathF.BitIncrement(MidiFileSequencer.MaximumSpeed) };
        public static IEnumerable<float> InvalidVolumes => new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity,
            -1F, float.MaxValue, MathF.BitDecrement(0F), MathF.BitIncrement(1F) };
        private static MidiFileSequencer Sequence()
        {
            var sequence = new MidiFileSequencer(SynthesisBaselineScenario.Synth(false));
            using var source = new MemoryStream(SynthesisBaselineScenario.MidiBytes());
            sequence.Play(new MidiFile(source), true);
            return sequence;
        }

        [TestCaseSource(nameof(InvalidSpeeds))]
        public void InvalidSpeedLeavesPlaybackAndSubsequentAudioUnchanged(float value)
        {
            var actual = Sequence(); var expected = Sequence();
            var left = new float[513]; var right = new float[513];
            actual.Render(left, right); expected.Render(left, right);
            var position = actual.Position; var voices = actual.Synthesizer.ActiveVoiceCount;
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => actual.Speed = value);
            Assert.That(error?.ParamName, Is.EqualTo("value"));
            Assert.That(actual.Speed, Is.EqualTo(1F)); Assert.That(actual.Position, Is.EqualTo(position));
            Assert.That(actual.Synthesizer.ActiveVoiceCount, Is.EqualTo(voices));
            var referenceLeft = new float[513]; var referenceRight = new float[513];
            actual.Render(left, right); expected.Render(referenceLeft, referenceRight);
            Assert.That(left, Is.EqualTo(referenceLeft)); Assert.That(right, Is.EqualTo(referenceRight));
        }

        [TestCaseSource(nameof(InvalidVolumes))]
        public void InvalidVolumeLeavesGainAndSubsequentAudioUnchanged(float value)
        {
            var actual = SynthesisBaselineScenario.Synth(false); var expected = SynthesisBaselineScenario.Synth(false);
            actual.NoteOn(0, 60, 100); expected.NoteOn(0, 60, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => actual.MasterVolume = value);
            Assert.That(actual.MasterVolume, Is.EqualTo(.5F));
            var left = new float[513]; var right = new float[513]; var refLeft = new float[513]; var refRight = new float[513];
            actual.Render(left, right); expected.Render(refLeft, refRight);
            Assert.That(left, Is.EqualTo(refLeft)); Assert.That(right, Is.EqualTo(refRight));
        }

        [TestCase(0F)]
        [TestCase(.25F)]
        [TestCase(1F)]
        public void GainIsLinearAndMuteDoesNotStopVoices(float gain)
        {
            var actual = SynthesisBaselineScenario.Synth(false); var reference = SynthesisBaselineScenario.Synth(false);
            actual.MasterVolume = gain; reference.MasterVolume = 1;
            actual.NoteOn(0, 60, 100); reference.NoteOn(0, 60, 100);
            var left = new float[512]; var right = new float[512]; var refLeft = new float[512]; var refRight = new float[512];
            actual.Render(left, right); reference.Render(refLeft, refRight);
            for (var i = 0; i < left.Length; i++)
            {
                Assert.That(left[i], Is.EqualTo(refLeft[i] * gain).Within(1E-6));
                Assert.That(right[i], Is.EqualTo(refRight[i] * gain).Within(1E-6));
            }
            Assert.That(actual.ActiveVoiceCount, Is.GreaterThan(0));
            actual.MasterVolume = 1; actual.Render(left, right);
            Assert.That(SynthesisBaselineScenario.Metrics(left, right)[0], Is.GreaterThan(0));
        }

        [Test]
        public void ZeroSpeedFreezesEventTimeWhileVoicesRenderAndCanResume()
        {
            var sequence = Sequence(); sequence.Speed = 0;
            var left = new float[512]; var right = new float[512]; sequence.Render(left, right);
            Assert.That(sequence.Position, Is.EqualTo(TimeSpan.Zero));
            Assert.That(SynthesisBaselineScenario.Metrics(left, right)[0], Is.GreaterThan(0));
            sequence.Speed = 1; sequence.Render(left, right);
            Assert.That(sequence.Position, Is.GreaterThan(TimeSpan.Zero));
        }

        [TestCase(MidiFileSequencer.MinimumSpeed)]
        [TestCase(1F)]
        [TestCase(MidiFileSequencer.MaximumSpeed)]
        public void PositiveSpeedBoundsAdvanceEvenAtSmallestBlockAndHighestSampleRate(float speed)
        {
            var synth = new Synthesizer(MidiLoopTestAssets.SoundFont(), new SynthesizerSettings(192000) { BlockSize = 8 });
            var sequence = new MidiFileSequencer(synth) { Speed = speed };
            using var source = new MemoryStream(SynthesisBaselineScenario.MidiBytes()); sequence.Play(new MidiFile(source), true);
            sequence.Render(new float[8], new float[8]);
            Assert.That(sequence.Position.Ticks, Is.GreaterThan(0));
        }
    }
}
