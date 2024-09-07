using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class InstrumentTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFonts))]
        public static void ReadTest(string soundFontName, MeltySynth.SoundFont soundFont)
        {
            var expected = new NAudio.SoundFont.SoundFont(soundFontName + ".sf2").Instruments;
            var actual = soundFont.Instruments;

            Assert.That(expected.Length, Is.EqualTo(actual.Count));
            for (var i = 0; i < expected.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        private static void AreEqual(NAudio.SoundFont.Instrument expected, MeltySynth.Instrument actual)
        {
            Assert.That(expected.Name, Is.EqualTo(actual.Name));
        }
    }
}
