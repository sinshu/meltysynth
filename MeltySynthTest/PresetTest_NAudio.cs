using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class PresetTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFontNames))]
        public void ReadTest(string soundFontName)
        {
            var expected = new NAudio.SoundFont.SoundFont(soundFontName + ".sf2").Presets;
            var actual = new MeltySynth.SoundFont(soundFontName + ".sf2").Presets;

            Assert.AreEqual(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.Preset expected, MeltySynth.Preset actual)
        {
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.PatchNumber, actual.PatchNumber);
            Assert.AreEqual(expected.Bank, actual.BankNumber);
        }
    }
}
