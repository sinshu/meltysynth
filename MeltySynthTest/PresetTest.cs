using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;


namespace MeltySynthTest
{
    public class PresetTest
    {
        [TestCaseSource(typeof(TestData), nameof(TestData.SoundFontPaths))]
        public void ReadTest(string path)
        {
            var expected = new NAudio.SoundFont.SoundFont(path).Presets;
            var actual = new MeltySynth.SoundFont(path).Presets;

            Assert.AreEqual(expected.Length, actual.Count);
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
