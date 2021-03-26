using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class GeneratorTest
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.LightSoundFontNames))]
        public void NoLoop_Speed1(string soundFontName)
        {
            var soundFont = new SoundFont(soundFontName + ".sf2");

            foreach (var instrument in soundFont.Instruments)
            {
                foreach (var region in instrument.Regions.Take(3))
                {
                    if (region.SampleModes == LoopMode.NoLoop)
                    {
                        NoLoop_Speed1_Run(soundFont, instrument, region);
                    }
                }
            }
        }

        private void NoLoop_Speed1_Run(SoundFont soundFont, Instrument instrument, InstrumentRegion region)
        {
            Console.WriteLine(instrument.Name + ", " + region.Sample.Name);

            var synthesizer = new Synthesizer(soundFont, 44100);
            var generator = new Generator(synthesizer);

            var block = new float[synthesizer.BlockSize];

            generator.Start(region);

            var actual = new List<float>();

            while (true)
            {
                var result = generator.Process(block, 1);

                actual.AddRange(block);

                if (!result)
                {
                    break;
                }
            }

            var start = region.SampleStart;
            var end = region.SampleEnd;
            var length = end - start;

            var raw = soundFont.WaveData.AsSpan(start, length);
            var expected = new float[length];
            for (var i = 0; i < length; i++)
            {
                var x = (float)raw[i] / 32768;
                expected[i] = x;
            }

            Assert.IsTrue(actual.Count - length <= synthesizer.BlockSize);

            for (var t = 0; t < expected.Length; t++)
            {
                if (Math.Abs(expected[t] - actual[t]) > 1.0E-6)
                {
                    Assert.Fail();
                }
            }

            for (var t = expected.Length; t < actual.Count; t++)
            {
                if (actual[t] != 0)
                {
                    Assert.Fail();
                }
            }
        }

        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.LightSoundFontNames))]
        public void NoLoop_Speed05(string soundFontName)
        {
            var soundFont = new SoundFont(soundFontName + ".sf2");

            foreach (var instrument in soundFont.Instruments)
            {
                foreach (var region in instrument.Regions.Take(3))
                {
                    if (region.SampleModes == LoopMode.NoLoop)
                    {
                        NoLoop_Speed05_Run(soundFont, instrument, region);
                    }
                }
            }
        }

        private void NoLoop_Speed05_Run(SoundFont soundFont, Instrument instrument, InstrumentRegion region)
        {
            Console.WriteLine(instrument.Name + ", " + region.Sample.Name);

            var synthesizer = new Synthesizer(soundFont, 44100);
            var generator = new Generator(synthesizer);

            var block = new float[synthesizer.BlockSize];

            generator.Start(region);

            var actual = new List<float>();

            while (true)
            {
                var result = generator.Process(block, 0.5);

                actual.AddRange(block);

                if (!result)
                {
                    break;
                }
            }

            var start = region.Sample.Start + region.StartAddressOffset;
            var end = region.Sample.End + region.EndAddressOffset;
            var length = end - start;

            var raw = soundFont.WaveData.AsSpan(start, length + 1);
            var expected = new float[2 * length];
            for (var i = 0; i < length; i++)
            {
                var x1 = (float)raw[i] / 32768;
                var x2 = (float)raw[i + 1] / 32768;
                expected[2 * i] = x1;
                expected[2 * i + 1] = (x1 + x2) / 2;
            }

            Assert.IsTrue(actual.Count - 2 * length <= synthesizer.BlockSize);

            for (var t = 0; t < expected.Length; t++)
            {
                if (Math.Abs(expected[t] - actual[t]) > 1.0E-6)
                {
                    Assert.Fail();
                }
            }

            for (var t = expected.Length; t < actual.Count; t++)
            {
                if (actual[t] != 0)
                {
                    Assert.Fail();
                }
            }
        }
    }
}
