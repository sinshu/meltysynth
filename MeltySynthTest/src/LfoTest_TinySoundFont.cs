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

            var synthesizer = new Synthesizer(44100);
            var lfo = new Lfo(synthesizer);

            lfo.Start(0.3F, 5.0F);

            for (var i = 0; i < expected.Length; i++)
            {
                lfo.Process();

                if (Math.Abs(lfo.Value - expected[i]) >= 2.5E-2)
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void Lfo_D00_F70()
        {
            var path = Path.Combine("ReferenceData", "TinySoundFont", "Lfo", "D00_F70.csv");
            var expected = File.ReadLines(path).Select(line => float.Parse(line.Split(',')[1])).ToArray();
            Assert.IsTrue(expected.Length == 1000);

            var synthesizer = new Synthesizer(44100);
            var lfo = new Lfo(synthesizer);

            lfo.Start(0.0F, 7.0F);

            for (var i = 0; i < expected.Length; i++)
            {
                lfo.Process();

                if (Math.Abs(lfo.Value - expected[i]) >= 2.5E-2)
                {
                    Assert.Fail();
                }
            }
        }
    }
}
