using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class SampleHeaderTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFonts))]
        public void ReadTest(string soundFontName, MeltySynth.SoundFont soundFont)
        {
            var expected = new NAudio.SoundFont.SoundFont(soundFontName + ".sf2").SampleHeaders;
            var actual = soundFont.SampleHeaders;

            Assert.That(expected.Length, Is.EqualTo(actual.Count));
            for (var i = 0; i < expected.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.SampleHeader expected, MeltySynth.SampleHeader actual)
        {
            Assert.That(expected.SampleName, Is.EqualTo(actual.Name));
            Assert.That(expected.Start, Is.EqualTo(actual.Start));
            Assert.That(expected.End, Is.EqualTo(actual.End));
            Assert.That(expected.StartLoop, Is.EqualTo(actual.StartLoop));
            Assert.That(expected.EndLoop, Is.EqualTo(actual.EndLoop));
            Assert.That(expected.SampleRate, Is.EqualTo(actual.SampleRate));
            Assert.That(expected.OriginalPitch, Is.EqualTo(actual.OriginalPitch));
            Assert.That(expected.PitchCorrection, Is.EqualTo(actual.PitchCorrection));
        }
    }
}
