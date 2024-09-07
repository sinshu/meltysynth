using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class ArrayMathTest
    {
        [TestCase(64)]
        [TestCase(63)]
        [TestCase(65)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(123)]
        [TestCase(127)]
        [TestCase(128)]
        [TestCase(129)]
        [TestCase(130)]
        [TestCase(41)]
        [TestCase(57)]
        [TestCase(278)]
        [TestCase(314)]
        public void MultiplyAddTest(int length)
        {
            var random = new Random(31415);

            var x1 = Enumerable.Range(0, length).Select(i => (float)(2 * (random.NextDouble() - 0.5))).ToArray();
            var x2 = Enumerable.Range(0, length).Select(i => (float)(2 * (random.NextDouble() - 0.5))).ToArray();
            var a = (float)(1 + random.NextDouble());

            var expected = new float[length];
            for (var i = 0; i < length; i++)
            {
                expected[i] = x1[i] + a * x2[i];
            }

            var actual = x1.ToArray();
            ArrayMath.MultiplyAdd(a, x2, actual);

            for (var i = 0; i < length; i++)
            {
                Assert.That(expected[i], Is.EqualTo(actual[i]).Within(1.0E-3F));
            }
        }
    }
}
