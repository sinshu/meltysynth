using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class SampleHeaderTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFontNames))]
        public void ReadTest(string soundFontName)
        {
            var expected = new NAudio.SoundFont.SoundFont(soundFontName + ".sf2").SampleHeaders;
            var actual = new MeltySynth.SoundFont(soundFontName + ".sf2").SampleHeaders;

            Assert.AreEqual(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.SampleHeader expected, MeltySynth.SampleHeader actual)
        {
            Assert.AreEqual(expected.SampleName, actual.Name);
            Assert.AreEqual(expected.Start, actual.Start);
            Assert.AreEqual(expected.End, actual.End);
            Assert.AreEqual(expected.StartLoop, actual.StartLoop);
            Assert.AreEqual(expected.EndLoop, actual.EndLoop);
            Assert.AreEqual(expected.SampleRate, actual.SampleRate);
            Assert.AreEqual(expected.OriginalPitch, actual.OriginalPitch);
            Assert.AreEqual(expected.PitchCorrection, actual.PitchCorrection);
            Assert.AreEqual((int)expected.SampleLink, (int)actual.Link);
            Assert.AreEqual((int)expected.SFSampleLink, (int)actual.Type);
        }
    }
}
