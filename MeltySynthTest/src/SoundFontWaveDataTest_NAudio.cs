using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class SoundFontWaveDataTest_NAudio
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFonts))]
        public void ReadTest(string soundFontName, MeltySynth.SoundFont soundFont)
        {
            var expected = new NAudio.SoundFont.SoundFont(soundFontName + ".sf2").SampleData;
            var actual = soundFont.WaveDataArray;

            // Since NAudio's sample data contains extra 12 bytes of the header,
            // the first 6 samples should be skipped.
            var p = MemoryMarshal.Cast<byte, short>(expected).Slice(6);

            Assert.AreEqual(p.Length, actual.Length);

            for (var i = 0; i < p.Length; i++)
            {
                if (p[i] != actual[i])
                {
                    Assert.Fail();
                }
            }
        }
    }
}
