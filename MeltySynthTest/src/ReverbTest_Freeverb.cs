using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class ReverbTest_Freeverb
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(16)]
        [TestCase(23)]
        [TestCase(50)]
        [TestCase(100)]
        public void CombFilter_BufferSize16_Feedback08_Damp01(int delay)
        {
            var path = @"ReferenceData\Freeverb\cf_bs16_fb08_da01.csv";

            var data = File.ReadLines(path).Select(line => float.Parse(line)).ToArray();
            Assert.IsTrue(data.Length == 500);

            var expected = new float[delay].Concat(data).ToArray();

            var cf = new Reverb.CombFilter(16);
            cf.Feedback = 0.8F;
            cf.Damp = 0.1F;

            var input = new float[expected.Length];
            input[delay] = 1F;

            var actual = new float[expected.Length];
            cf.Process(input, actual);

            for (var t = 0; t < expected.Length; t++)
            {
                var error = actual[t] - expected[t];
                Assert.IsTrue(Math.Abs(error) < 1.0E-3);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(16)]
        [TestCase(23)]
        [TestCase(50)]
        [TestCase(100)]
        public void CombFilter_BufferSize23_Feedback07_Damp03(int delay)
        {
            var path = @"ReferenceData\Freeverb\cf_bs23_fb07_da03.csv";

            var data = File.ReadLines(path).Select(line => float.Parse(line)).ToArray();
            Assert.IsTrue(data.Length == 500);

            var expected = new float[delay].Concat(data).ToArray();

            var cf = new Reverb.CombFilter(23);
            cf.Feedback = 0.7F;
            cf.Damp = 0.3F;

            var input = new float[expected.Length];
            input[delay] = 1F;

            var actual = new float[expected.Length];
            cf.Process(input, actual);

            for (var t = 0; t < expected.Length; t++)
            {
                var error = actual[t] - expected[t];
                Assert.IsTrue(Math.Abs(error) < 1.0E-3);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(16)]
        [TestCase(23)]
        [TestCase(50)]
        [TestCase(100)]
        public void AllPassFilter_BufferSize16_Feedback05(int delay)
        {
            var path = @"ReferenceData\Freeverb\apf_bs16_fb05.csv";

            var data = File.ReadLines(path).Select(line => float.Parse(line)).ToArray();
            Assert.IsTrue(data.Length == 500);

            var expected = new float[delay].Concat(data).ToArray();

            var apf = new Reverb.AllPassFilter(16);
            apf.Feedback = 0.5F;

            var actual = new float[expected.Length];
            actual[delay] = 1F;
            apf.Process(actual);

            for (var t = 0; t < expected.Length; t++)
            {
                var error = actual[t] - expected[t];
                Assert.IsTrue(Math.Abs(error) < 1.0E-3);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(16)]
        [TestCase(23)]
        [TestCase(50)]
        [TestCase(100)]
        public void AllPassFilter_BufferSize23_Feedback07(int delay)
        {
            var path = @"ReferenceData\Freeverb\apf_bs23_fb07.csv";

            var data = File.ReadLines(path).Select(line => float.Parse(line)).ToArray();
            Assert.IsTrue(data.Length == 500);

            var expected = new float[delay].Concat(data).ToArray();

            var apf = new Reverb.AllPassFilter(23);
            apf.Feedback = 0.7F;

            var actual = new float[expected.Length];
            actual[delay] = 1F;
            apf.Process(actual);

            for (var t = 0; t < expected.Length; t++)
            {
                var error = actual[t] - expected[t];
                Assert.IsTrue(Math.Abs(error) < 1.0E-3);
            }
        }

        [Test]
        public void ImpulseResponse()
        {
            var length = 44100;

            var expectedLeft = new float[length];
            var expectedRight = new float[length];

            using (var reader = new WaveFileReader(@"ReferenceData\Freeverb\freeverb_default_ir.wav"))
            {
                for (var t = 0; t < length; t++)
                {
                    var frame = reader.ReadNextSampleFrame();
                    expectedLeft[t] = frame[0];
                    expectedRight[t] = frame[1];
                }
            }

            var reverb = new Reverb(44100);

            var input = new float[length];
            input[0] = reverb.InputGain;

            var actualLeft = new float[length];
            var actualRight = new float[length];

            reverb.Process(input, actualLeft, actualRight);

            for (var t = 0; t < length; t++)
            {
                var errorLeft = actualLeft[t] - expectedLeft[t];
                var errorRight = actualRight[t] - expectedRight[t];
                if (Math.Abs(errorLeft) > 1.0E-3)
                {
                    Assert.Fail();
                }
                if (Math.Abs(errorRight) > 1.0E-3)
                {
                    Assert.Fail();
                }
            }
        }
    }
}
