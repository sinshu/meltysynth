using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;


namespace MeltySynthTest
{
    public class SoundFontSampleDataTest
    {
        [TestCaseSource(typeof(TestData), nameof(TestData.SoundFontPaths))]
        public void ReadTest(string path)
        {
            var expected = new NAudio.SoundFont.SoundFont(path).SampleData;
            var actual = new MeltySynth.SoundFont(path).SampleData;

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
