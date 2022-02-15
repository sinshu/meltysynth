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

        [TestCase(0, 60, 1F, 1.0E-4)]
        [TestCase(100, 60, 1F, 1.0E-4)]
        [TestCase(1000, 60, 1F, 1.0E-4)]
        [TestCase(100, 72, 0.5F, 1.0E-4)]
        [TestCase(100, 48, 2F, 1.0E-4)]
        [TestCase(50, 84, 0.5F, 1.0E-4)]
        [TestCase(50, 36, 2F, 1.0E-4)]
        public void KeyNumberToMultiplyingFactorTest(int cents, int key, float expected, double delta)
        {
            var actual = SoundFontMath.KeyNumberToMultiplyingFactor(cents, key);
            Assert.AreEqual(expected, actual, delta);
        }

        [TestCase(1.0, Math.E, 1.0E-5)]
        [TestCase(2.0, Math.E * Math.E, 1.0E-5)]
        [TestCase(3.0, Math.E * Math.E * Math.E, 1.0E-5)]
        [TestCase(0.0, 1.0, 1.0E-5)]
        [TestCase(-1.0, 1.0 / Math.E, 1.0E-5)]
        [TestCase(-2.0, 1.0 / (Math.E * Math.E), 1.0E-5)]
        [TestCase(-3.0, 1.0 / (Math.E * Math.E * Math.E), 1.0E-5)]
        [TestCase(-6.9, 0.001007, 1.0E-5)]
        [TestCase(-7.0, 0.0, 1.0E-9)]
        public void ExpCutoffTest(double x, double expected, double delta)
        {
            var actual = SoundFontMath.ExpCutoff(x);
            Assert.AreEqual(expected, actual, delta);
        }

        [TestCase(3.0F, 4.0F, 5.0F)]
        [TestCase(3.9F, 4.0F, 5.0F)]
        [TestCase(4.0F, 4.0F, 5.0F)]
        [TestCase(4.1F, 4.0F, 5.0F)]
        [TestCase(4.5F, 4.0F, 5.0F)]
        [TestCase(4.9F, 4.0F, 5.0F)]
        [TestCase(5.0F, 4.0F, 5.0F)]
        [TestCase(5.1F, 4.0F, 5.0F)]
        [TestCase(6.0F, 4.0F, 5.0F)]
        [TestCase(-1.0F, 0.0F, 1.0F)]
        [TestCase(-0.1F, 0.0F, 1.0F)]
        [TestCase(0.0F, 0.0F, 1.0F)]
        [TestCase(0.1F, 0.0F, 1.0F)]
        [TestCase(0.5F, 0.0F, 1.0F)]
        [TestCase(0.9F, 0.0F, 1.0F)]
        [TestCase(1.0F, 0.0F, 1.0F)]
        [TestCase(1.1F, 0.0F, 1.0F)]
        [TestCase(2.0F, 0.0F, 1.0F)]
        public void ClampTest(float value, float min, float max)
        {
            var actual = SoundFontMath.Clamp(value, min, max);
            var expected = Math.Clamp(value, min, max);
            Assert.AreEqual(actual, expected);
        }
    }
}
