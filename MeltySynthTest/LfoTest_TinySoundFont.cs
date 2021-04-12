using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class LfoTest_TinySoundFont
    {
        [Test]
        public void Lfo_D03_F50()
        {
            var path = Path.Combine("ReferenceData", "TinySoundFont", "Lfo", "D03_F50.csv");
            var expected = File.ReadLines(path).Select(line => float.Parse(line.Split(',')[1])).ToArray();
            Assert.IsTrue(expected.Length == 1000);
        }
    }
}
