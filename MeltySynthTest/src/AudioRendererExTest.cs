using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class AudioRendererExTest
    {
        [TestCase(64)]
        [TestCase(63)]
        [TestCase(65)]
        [TestCase(41)]
        [TestCase(57)]
        [TestCase(278)]
        [TestCase(314)]
        public void RenderInterleavedTest(int length)
        {
            var random = new Random(31415);

            var srcLeft = Enumerable.Range(0, length).Select(i => (float)(2 * (random.NextDouble() - 0.5))).ToArray();
            var srcRight = Enumerable.Range(0, length).Select(i => (float)(2 * (random.NextDouble() - 0.5))).ToArray();
            var renderer = new DummyRenderer(srcLeft, srcRight);

            var expected = srcLeft.Zip(srcRight, (x, y) => new[] { x, y }).SelectMany(x => x).ToArray();

            var actual = new float[2 * length];
            renderer.RenderInterleaved(actual);

            for (var i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i], 1.0E-6F);
            }
        }

        [TestCase(64)]
        [TestCase(63)]
        [TestCase(65)]
        [TestCase(41)]
        [TestCase(57)]
        [TestCase(278)]
        [TestCase(314)]
        public void RenderMonoTest(int length)
        {
            var random = new Random(31415);

            var srcLeft = Enumerable.Range(0, length).Select(i => (float)(2 * (random.NextDouble() - 0.5))).ToArray();
            var srcRight = Enumerable.Range(0, length).Select(i => (float)(2 * (random.NextDouble() - 0.5))).ToArray();
            var renderer = new DummyRenderer(srcLeft, srcRight);

            var expected = srcLeft.Zip(srcRight, (x, y) => (x + y) / 2).ToArray();

            var actual = new float[length];
            renderer.RenderMono(actual);

            for (var i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i], 1.0E-6F);
            }
        }

        [TestCase(64)]
        [TestCase(63)]
        [TestCase(65)]
        [TestCase(41)]
        [TestCase(57)]
        [TestCase(278)]
        [TestCase(314)]
        public void RenderInterleavedInt16Test(int length)
        {
            var random = new Random(31415);

            var srcLeft = Enumerable.Range(0, length).Select(i => (float)(4 * (random.NextDouble() - 0.5))).ToArray();
            var srcRight = Enumerable.Range(0, length).Select(i => (float)(4 * (random.NextDouble() - 0.5))).ToArray();
            var renderer = new DummyRenderer(srcLeft, srcRight);

            var expected = srcLeft.Zip(srcRight, (x, y) => new[] { ToShort(x), ToShort(y) }).SelectMany(x => x).ToArray();

            var actual = new short[2 * length];
            renderer.RenderInterleavedInt16(actual);

            for (var i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestCase(64)]
        [TestCase(63)]
        [TestCase(65)]
        [TestCase(41)]
        [TestCase(57)]
        [TestCase(278)]
        [TestCase(314)]
        public void RenderMonoInt16Test(int length)
        {
            var random = new Random(31415);

            var srcLeft = Enumerable.Range(0, length).Select(i => (float)(4 * (random.NextDouble() - 0.5))).ToArray();
            var srcRight = Enumerable.Range(0, length).Select(i => (float)(4 * (random.NextDouble() - 0.5))).ToArray();
            var renderer = new DummyRenderer(srcLeft, srcRight);

            var expected = srcLeft.Zip(srcRight, (x, y) => ToShort((x + y) / 2)).ToArray();

            var actual = new short[length];
            renderer.RenderMonoInt16(actual);

            for (var i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        private static short ToShort(float value)
        {
            return (short)Math.Clamp((int)(32768 * value), short.MinValue, short.MaxValue);
        }



        private class DummyRenderer : IAudioRenderer
        {
            private float[] srcLeft;
            private float[] srcRight;

            public DummyRenderer(float[] srcLeft, float[] srcRight)
            {
                this.srcLeft = srcLeft;
                this.srcRight = srcRight;
            }

            public void Render(Span<float> left, Span<float> right)
            {
                srcLeft.AsSpan().CopyTo(left);
                srcRight.AsSpan().CopyTo(right);
            }
        }
    }
}
