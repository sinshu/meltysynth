// SPDX-License-Identifier: MIT
// Copyright and inherited upstream notices: see LICENSE.txt.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class PresetTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFonts))]
        public void ReadTest(string soundFontName)
        {
            var soundFont = TestSettings.LoadExternalSoundFont(soundFontName);
            var expected = new NAudio.SoundFont.SoundFont(TestAssets.RequireExternal(soundFontName + ".sf2")).Presets;
            var actual = soundFont.Presets;

            Assert.That(expected.Length, Is.EqualTo(actual.Count));
            for (var i = 0; i < expected.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.Preset expected, MeltySynth.Preset actual)
        {
            Assert.That(expected.Name, Is.EqualTo(actual.Name));
            Assert.That(expected.PatchNumber, Is.EqualTo(actual.PatchNumber));
            Assert.That(expected.Bank, Is.EqualTo(actual.BankNumber));
        }
    }
}
