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
