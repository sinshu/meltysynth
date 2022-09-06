using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class BiQuadFilterTest
    {
        [TestCase(44100, 1000)]
        [TestCase(44100, 500)]
        [TestCase(44100, 5000)]
        [TestCase(22050, 3000)]
        [TestCase(44100, 22050)]
        [TestCase(44100, 50000)]
        [TestCase(48000, 10000)]
        [TestCase(48000, 24000)]
        public void LowPassFilterTest(int sampleRate, int cutoffFrequency)
        {
            var synthesizer = new Synthesizer(TestSettings.DefaultSoundFont, sampleRate);

            var lpf = new BiQuadFilter(synthesizer);
            lpf.SetLowPassFilter(cutoffFrequency, 1);

            var block = new float[4096];
            block[0] = 1;

            lpf.Process(block);

            var fft = block.Select(x => (Complex)x).ToArray();
            Fourier.Forward(fft, FourierOptions.AsymmetricScaling);

            var spectrum = fft.Select(x => x.Magnitude).ToArray();

            for (var i = 0; i < spectrum.Length / 2; i++)
            {
                var frequency = (double)i / spectrum.Length * sampleRate;

                if (frequency < cutoffFrequency - 1)
                {
                    Assert.True(spectrum[i] > 1 / Math.Sqrt(2));
                }

                if (frequency > cutoffFrequency + 1)
                {
                    Assert.True(spectrum[i] < 1 / Math.Sqrt(2));
                }

                if (frequency < cutoffFrequency / 10)
                {
                    Assert.AreEqual(spectrum[i], 1.0, 0.1);
                }
            }
        }

        [TestCase(44100, 1000, 2.0F)]
        [TestCase(44100, 500, 3.14F)]
        [TestCase(44100, 5000, 5.7F)]
        [TestCase(22050, 3000, 12.3F)]
        [TestCase(48000, 500, 2.7F)]
        public void ResonanceTest(int sampleRate, int cutoffFrequency, float resonance)
        {
            var synthesizer = new Synthesizer(TestSettings.DefaultSoundFont, sampleRate);

            var lpf = new BiQuadFilter(synthesizer);
            lpf.SetLowPassFilter(cutoffFrequency, resonance);

            var block = new float[4096];
            block[0] = 1;

            lpf.Process(block);

            var fft = block.Select(x => (Complex)x).ToArray();
            Fourier.Forward(fft, FourierOptions.AsymmetricScaling);

            var spectrum = fft.Select(x => x.Magnitude).ToArray();

            for (var i = 0; i < spectrum.Length / 2; i++)
            {
                var frequency = (double)i / spectrum.Length * sampleRate;

                if (frequency < cutoffFrequency / 10)
                {
                    Assert.AreEqual(spectrum[i], 1.0, 0.1);
                }
            }

            Assert.AreEqual(resonance, spectrum.Max(), 0.03);
        }
    }
}
