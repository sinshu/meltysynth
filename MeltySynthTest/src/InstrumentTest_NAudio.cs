using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class InstrumentTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFontNames))]
        public void ReadTest(string soundFontName)
        {
            var expected = new NAudio.SoundFont.SoundFont(soundFontName + ".sf2").Instruments;
            var actual = new MeltySynth.SoundFont(soundFontName + ".sf2").Instruments;

            Assert.AreEqual(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.Instrument expected, MeltySynth.Instrument actual)
        {
            Assert.AreEqual(expected.Name, actual.Name);
        }
    }
}
