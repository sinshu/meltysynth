using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class SoundFontMathTest
    {
        [TestCase(0F, 1F, 1.0E-5)]
        [TestCase(1200F, 2F, 1.0E-5)]
        [TestCase(-1200F, 0.5F, 1.0E-5)]
        [TestCase(-7973F, 0.01F, 1.0E-2)]
        public void TimecentsToSecondsTest(float x, float expected, double delta)
        {
            var actual = SoundFontMath.TimecentsToSeconds(x);
            Assert.AreEqual(expected, actual, delta);
        }

        [TestCase(0F, 1F * 8.176F, 1.0E-6)]
        [TestCase(1200F, 2F * 8.176F, 1.0E-6)]
        [TestCase(-1200F, 0.5F * 8.176F, 1.0E-6)]
        [TestCase(1500F, 20F, 1.0)]
        public void CentsToHertzTest(float x, float expected, double delta)
        {
            var actual = SoundFontMath.CentsToHertz(x);
            Assert.AreEqual(expected, actual, delta);
        }

        [TestCase(0F, 1F, 1.0E-6)]
        [TestCase(1200F, 2F, 1.0E-6)]
        [TestCase(-1200F, 0.5F, 1.0E-6)]
        [TestCase(12000F, 1024F, 1.0E-6)]
        public void CentsToMultiplyingFactorTest(float x, float expected, double delta)
        {
            var actual = SoundFontMath.CentsToMultiplyingFactor(x);
            Assert.AreEqual(expected, actual, delta);
        }

        [TestCase(0F, 1F, 1.0E-5)]
        [TestCase(6F, 1.99526F, 1.0E-5)]
        [TestCase(12F, 3.98107F, 1.0E-5)]
        [TestCase(-6F, 0.501187F, 1.0E-5)]
        [TestCase(-12F, 0.251189F, 1.0E-5)]
        public void DecibelsToLinearTest(float x, float expected, double delta)
        {
            var actual = SoundFontMath.DecibelsToLinear(x);
            Assert.AreEqual(expected, actual, delta);
        }

        [TestCase(1F, 0F, 1.0E-4)]
        [TestCase(1.99526F, 6F, 1.0E-4)]
        [TestCase(3.98107F, 12F, 1.0E-4)]
        [TestCase(0.501187F, -6F, 1.0E-4)]
        [TestCase(0.251189F, -12F, 1.0E-4)]
        public void LinearToDecibelsTest(float x, float expected, double delta)
        {
            var actual = SoundFontMath.LinearToDecibels(x);
            Assert.AreEqual(expected, actual, delta);
        }
    }
}
