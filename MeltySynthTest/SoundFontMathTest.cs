using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class SoundFontMathTest
    {
        [TestCase(0, 1F, 1.0E-5)]
        [TestCase(1200, 2F, 1.0E-5)]
        [TestCase(-1200, 0.5F, 1.0E-5)]
        [TestCase(-7973, 0.01F, 1.0E-2)]
        public void TimecentsToSecondsTest(int x, float expected, double delta)
        {
            var actual = SoundFontMath.TimecentsToSeconds(x);
            Assert.AreEqual(expected, actual, delta);
        }

        [TestCase(0, 1F * 8.176F, 1.0E-6)]
        [TestCase(1200, 2F * 8.176F, 1.0E-6)]
        [TestCase(-1200, 0.5F * 8.176F, 1.0E-6)]
        [TestCase(1500, 20F, 1.0)]
        public void CentsToHertzTest(int x, float expected, double delta)
        {
            var actual = SoundFontMath.CentsToHertz(x);
            Assert.AreEqual(expected, actual, delta);
        }
    }
}
