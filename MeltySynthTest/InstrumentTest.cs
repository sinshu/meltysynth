using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;


namespace MeltySynthTest
{
    public class InstrumentTest
    {
        [TestCaseSource(typeof(TestData), nameof(TestData.SoundFontPaths))]
        public void ReadTest(string path)
        {
            var expected = new NAudio.SoundFont.SoundFont(path).Instruments;
            var actual = new MeltySynth.SoundFont.SoundFont(path).Instruments;

            Assert.AreEqual(expected.Length, actual.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.Instrument expected, MeltySynth.SoundFont.Instrument actual)
        {
            Assert.AreEqual(expected.Name, actual.Name);

            Assert.AreEqual(expected.Zones.Length, actual.Zones.Count);
            for (var i = 0; i < expected.Zones.Length; i++)
            {
                AreEqual(expected.Zones[i], actual.Zones[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.Zone expected, MeltySynth.SoundFont.Zone actual)
        {
            Assert.AreEqual(expected.Modulators.Length, actual.ModulatorParameters.Count);
            for (var i = 0; i < expected.Modulators.Length; i++)
            {
                AreEqual(expected.Modulators[i], actual.ModulatorParameters[i]);
            }

            Assert.AreEqual(expected.Generators.Length, actual.GeneratorParameters.Count);
            for (var i = 0; i < expected.Generators.Length; i++)
            {
                AreEqual(expected.Generators[i], actual.GeneratorParameters[i]);
            }
        }

        private void AreEqual(NAudio.SoundFont.Modulator expected, MeltySynth.SoundFont.ModulatorParameter actual)
        {
            AreEqual(expected.SourceModulationData, actual.SourceModulationData);
            Assert.AreEqual((int)expected.DestinationGenerator, (int)actual.DestinationGenerator);
            Assert.AreEqual(expected.Amount, actual.Amount);
            AreEqual(expected.SourceModulationAmount, actual.SourceModulationAmount);
            Assert.AreEqual((int)expected.SourceTransform, (int)actual.SourceTransform);
        }

        private void AreEqual(NAudio.SoundFont.ModulatorType expected, MeltySynth.SoundFont.ModulatorParameterType actual)
        {
            // NAudio haven't yet implemented ModulatorType.
        }

        private void AreEqual(NAudio.SoundFont.Generator expected, MeltySynth.SoundFont.GeneratorParameter actual)
        {
            Assert.AreEqual(expected.UInt16Amount, actual.Value);
        }
    }
}
