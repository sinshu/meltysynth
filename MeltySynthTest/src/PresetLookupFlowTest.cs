// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class PresetLookupFlowTest
    {
        [TestCase(0, 0)] // Exact bank/program entry.
        [TestCase(1, 0)] // Requested bank missing; GM entry is present.
        [TestCase(0, 7)] // Requested program and GM entry missing; use default.
        [TestCase(127, 42)] // Missing percussion request/standard kit; use default.
        public void EveryLookupOutcomeSelectsAnOwnedNonNullPreset(int bank, int program)
        {
            var channel = bank == 127 ? 9 : 0;
            var expected = SynthesisBaselineScenario.Synth(false);
            var actual = SynthesisBaselineScenario.Synth(false);
            actual.ProcessMidiMessage(channel, 0xB0, 0, bank);
            actual.ProcessMidiMessage(channel, 0xC0, program, 0);
            expected.NoteOn(channel, 60, 100); actual.NoteOn(channel, 60, 100);
            var left = new float[513]; var right = new float[513];
            var expectedLeft = new float[513]; var expectedRight = new float[513];
            expected.Render(expectedLeft, expectedRight); actual.Render(left, right);
            Assert.That(SynthesisBaselineScenario.Metrics(left, right)[0], Is.GreaterThan(0));
            Assert.That(left, Is.EqualTo(expectedLeft).Within(1E-6));
            Assert.That(right, Is.EqualTo(expectedRight).Within(1E-6));
        }
    }
}
