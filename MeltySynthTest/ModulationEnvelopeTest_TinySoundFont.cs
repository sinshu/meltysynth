using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class ModulationEnvelopeTest_TinySoundFont
    {
        [Test]
        public void D03_A05_H07_D11_S02_R13_Continuous()
        {
            var dir = Path.Combine("ReferenceData", "TinySoundFont", "ModulationEnvelope", "D03_A05_H07_D11_S02_R13");
            var expected = File.ReadLines(Path.Combine(dir, "Continuous.csv")).Select(line => float.Parse(line.Split(',')[1])).ToArray();
            Assert.IsTrue(expected.Length == 3000);

            var synthesizer = new Synthesizer(44100);
            var envelope = new ModulationEnvelope(synthesizer);

            envelope.Start(0.3F, 0.5F, 0.7F, 1.1F, 0.2F, 1.3F);

            for (var i = 0; i < expected.Length; i++)
            {
                envelope.Process(synthesizer.BlockSize);

                if (Math.Abs(envelope.Value - expected[i]) >= 2.0E-2)
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void D03_A05_H07_D11_S02_R13_NoteOff()
        {
            var dir = Path.Combine("ReferenceData", "TinySoundFont", "ModulationEnvelope", "D03_A05_H07_D11_S02_R13");

            var synthesizer = new Synthesizer(44100);
            var envelope = new ModulationEnvelope(synthesizer);

            for (var noteOffBlock = 0; noteOffBlock <= 2000; noteOffBlock += 50)
            {
                var name = "NoteOff" + noteOffBlock.ToString("0000") + ".csv";
                var expected = File.ReadLines(Path.Combine(dir, name)).Select(line => float.Parse(line.Split(',')[1])).ToArray();
                Assert.IsTrue(expected.Length == 3000);

                envelope.Start(0.3F, 0.5F, 0.7F, 1.1F, 0.2F, 1.3F);

                for (var i = 0; i < expected.Length; i++)
                {
                    if (i == noteOffBlock)
                    {
                        envelope.Release();
                    }

                    envelope.Process(synthesizer.BlockSize);

                    if (Math.Abs(envelope.Value - expected[i]) >= 2.0E-2)
                    {
                        Assert.Fail();
                    }
                }
            }
        }

        [Test]
        public void D00_A00_H00_D23_S01_R57_Continuous()
        {
            var dir = Path.Combine("ReferenceData", "TinySoundFont", "ModulationEnvelope", "D00_A00_H00_D23_S01_R57");
            var expected = File.ReadLines(Path.Combine(dir, "Continuous.csv")).Select(line => float.Parse(line.Split(',')[1])).ToArray();
            Assert.IsTrue(expected.Length == 3000);

            var synthesizer = new Synthesizer(44100);
            var envelope = new ModulationEnvelope(synthesizer);

            envelope.Start(0.0F, 0.0F, 0.0F, 2.3F, 0.1F, 5.7F);

            for (var i = 0; i < expected.Length; i++)
            {
                envelope.Process(synthesizer.BlockSize);

                if (Math.Abs(envelope.Value - expected[i]) >= 2.0E-2)
                {
                    Assert.Fail();
                }
            }
        }

        [Test]
        public void D00_A00_H00_D23_S01_R57_NoteOff()
        {
            var dir = Path.Combine("ReferenceData", "TinySoundFont", "ModulationEnvelope", "D00_A00_H00_D23_S01_R57");

            var synthesizer = new Synthesizer(44100);
            var envelope = new ModulationEnvelope(synthesizer);

            for (var noteOffBlock = 0; noteOffBlock <= 2000; noteOffBlock += 50)
            {
                var name = "NoteOff" + noteOffBlock.ToString("0000") + ".csv";
                var expected = File.ReadLines(Path.Combine(dir, name)).Select(line => float.Parse(line.Split(',')[1])).ToArray();
                Assert.IsTrue(expected.Length == 3000);

                envelope.Start(0.0F, 0.0F, 0.0F, 2.3F, 0.1F, 5.7F);

                for (var i = 0; i < expected.Length; i++)
                {
                    if (i == noteOffBlock)
                    {
                        envelope.Release();
                    }

                    envelope.Process(synthesizer.BlockSize);

                    if (Math.Abs(envelope.Value - expected[i]) >= 2.0E-2)
                    {
                        Assert.Fail();
                    }
                }
            }
        }
    }
}
