using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class RegionPairTest
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFontNames))]
        public void DefaultPresetRegionDoesNotAffect(string soundFontName)
        {
            var soundFont = new SoundFont(soundFontName + ".sf2");

            foreach (var instrument in soundFont.Instruments)
            {
                foreach (var region in instrument.Regions)
                {
                    var pair = new RegionPair(PresetRegion.Default, region);
                    AreEqual(region, pair);
                }
            }
        }

        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFontNames))]
        public void ParameterCheck(string soundFontName)
        {
            var soundFont = new SoundFont(soundFontName + ".sf2");

            foreach (var preset in soundFont.Presets)
            {
                foreach (var presetRegion in preset.Regions)
                {
                    foreach (var instrumentRegion in presetRegion.Instrument.Regions)
                    {
                        var pair = new RegionPair(presetRegion, instrumentRegion);
                        Check(presetRegion, instrumentRegion, pair);
                    }
                }
            }
        }

        private static void AreEqual(InstrumentRegion expected, RegionPair actual)
        {
            Assert.AreEqual(expected.StartAddressOffset, actual.StartAddressOffset);
            Assert.AreEqual(expected.EndAddressOffset, actual.EndAddressOffset);
            Assert.AreEqual(expected.StartLoopAddressOffset, actual.StartLoopAddressOffset);
            Assert.AreEqual(expected.EndLoopAddressOffset, actual.EndLoopAddressOffset);

            Assert.AreEqual(expected.ModulationLfoToPitch, actual.ModulationLfoToPitch);
            Assert.AreEqual(expected.VibratoLfoToPitch, actual.VibratoLfoToPitch);
            Assert.AreEqual(expected.ModulationEnvelopeToPitch, actual.ModulationEnvelopeToPitch);
            Assert.AreEqual(expected.InitialFilterCutoffFrequency, actual.InitialFilterCutoffFrequency, 1.0E-6);
            Assert.AreEqual(expected.InitialFilterQ, actual.InitialFilterQ, 1.0E-6);
            Assert.AreEqual(expected.ModulationLfoToFilterCutoffFrequency, actual.ModulationLfoToFilterCutoffFrequency);
            Assert.AreEqual(expected.ModulationEnvelopeToFilterCutoffFrequency, actual.ModulationEnvelopeToFilterCutoffFrequency);

            Assert.AreEqual(expected.ModulationLfoToVolume, actual.ModulationLfoToVolume, 1.0E-6);

            Assert.AreEqual(expected.ChorusEffectsSend, actual.ChorusEffectsSend, 1.0E-6);
            Assert.AreEqual(expected.ReverbEffectsSend, actual.ReverbEffectsSend, 1.0E-6);
            Assert.AreEqual(expected.Pan, actual.Pan, 1.0E-6);

            Assert.AreEqual(expected.DelayModulationLfo, actual.DelayModulationLfo, 1.0E-6);
            Assert.AreEqual(expected.FrequencyModulationLfo, actual.FrequencyModulationLfo, 1.0E-6);
            Assert.AreEqual(expected.DelayVibratoLfo, actual.DelayVibratoLfo, 1.0E-6);
            Assert.AreEqual(expected.FrequencyVibratoLfo, actual.FrequencyVibratoLfo, 1.0E-6);
            Assert.AreEqual(expected.DelayModulationEnvelope, actual.DelayModulationEnvelope, 1.0E-6);
            Assert.AreEqual(expected.AttackModulationEnvelope, actual.AttackModulationEnvelope, 1.0E-6);
            Assert.AreEqual(expected.HoldModulationEnvelope, actual.HoldModulationEnvelope, 1.0E-6);
            Assert.AreEqual(expected.DecayModulationEnvelope, actual.DecayModulationEnvelope, 1.0E-6);
            Assert.AreEqual(expected.SustainModulationEnvelope, actual.SustainModulationEnvelope, 1.0E-6);
            Assert.AreEqual(expected.ReleaseModulationEnvelope, actual.ReleaseModulationEnvelope, 1.0E-6);
            Assert.AreEqual(expected.KeyNumberToModulationEnvelopeHold, actual.KeyNumberToModulationEnvelopeHold);
            Assert.AreEqual(expected.KeyNumberToModulationEnvelopeDecay, actual.KeyNumberToModulationEnvelopeDecay);
            Assert.AreEqual(expected.DelayVolumeEnvelope, actual.DelayVolumeEnvelope, 1.0E-6);
            Assert.AreEqual(expected.AttackVolumeEnvelope, actual.AttackVolumeEnvelope, 1.0E-6);
            Assert.AreEqual(expected.HoldVolumeEnvelope, actual.HoldVolumeEnvelope, 1.0E-6);
            Assert.AreEqual(expected.DecayVolumeEnvelope, actual.DecayVolumeEnvelope, 1.0E-6);
            Assert.AreEqual(expected.SustainVolumeEnvelope, actual.SustainVolumeEnvelope, 1.0E-6);
            Assert.AreEqual(expected.ReleaseVolumeEnvelope, actual.ReleaseVolumeEnvelope, 1.0E-6);
            Assert.AreEqual(expected.KeyNumberToVolumeEnvelopeHold, actual.KeyNumberToVolumeEnvelopeHold);
            Assert.AreEqual(expected.KeyNumberToVolumeEnvelopeDecay, actual.KeyNumberToVolumeEnvelopeDecay);

            Assert.AreEqual(expected.InitialAttenuation, actual.InitialAttenuation, 1.0E-6);

            Assert.AreEqual(expected.CoarseTune, actual.CoarseTune);
            Assert.AreEqual(expected.FineTune, actual.FineTune);
            Assert.AreEqual(expected.SampleModes, actual.SampleModes);

            Assert.AreEqual(expected.ScaleTuning, actual.ScaleTuning);
            Assert.AreEqual(expected.ExclusiveClass, actual.ExclusiveClass);
            Assert.AreEqual(expected.OverridingRootKey, actual.OverridingRootKey);
        }

        private static void Check(PresetRegion preset, InstrumentRegion instrument, RegionPair pair)
        {
            Assert.AreEqual(instrument.StartAddressOffset, pair.StartAddressOffset);
            Assert.AreEqual(instrument.EndAddressOffset, pair.EndAddressOffset);
            Assert.AreEqual(instrument.StartLoopAddressOffset, pair.StartLoopAddressOffset);
            Assert.AreEqual(instrument.EndLoopAddressOffset, pair.EndLoopAddressOffset);

            Assert.AreEqual(instrument.ModulationLfoToPitch + preset.ModulationLfoToPitch, pair.ModulationLfoToPitch);
            Assert.AreEqual(instrument.VibratoLfoToPitch + preset.VibratoLfoToPitch, pair.VibratoLfoToPitch);
            Assert.AreEqual(instrument.ModulationEnvelopeToPitch + preset.ModulationEnvelopeToPitch, pair.ModulationEnvelopeToPitch);
            Assert.AreEqual(instrument.InitialFilterCutoffFrequency * preset.InitialFilterCutoffFrequency, pair.InitialFilterCutoffFrequency, 1.0);
            Assert.AreEqual(instrument.InitialFilterQ + preset.InitialFilterQ, pair.InitialFilterQ, 1.0E-3);
            Assert.AreEqual(instrument.ModulationLfoToFilterCutoffFrequency + preset.ModulationLfoToFilterCutoffFrequency, pair.ModulationLfoToFilterCutoffFrequency);
            Assert.AreEqual(instrument.ModulationEnvelopeToFilterCutoffFrequency + preset.ModulationEnvelopeToFilterCutoffFrequency, pair.ModulationEnvelopeToFilterCutoffFrequency);

            Assert.AreEqual(instrument.ModulationLfoToVolume + preset.ModulationLfoToVolume, pair.ModulationLfoToVolume, 1.0E-3);

            Assert.AreEqual(instrument.ChorusEffectsSend + preset.ChorusEffectsSend, pair.ChorusEffectsSend, 1.0E-3);
            Assert.AreEqual(instrument.ReverbEffectsSend + preset.ReverbEffectsSend, pair.ReverbEffectsSend, 1.0E-3);
            Assert.AreEqual(instrument.Pan + preset.Pan, pair.Pan, 1.0E-3);

            Assert.AreEqual(instrument.DelayModulationLfo * preset.DelayModulationLfo, pair.DelayModulationLfo, 1.0E-3);
            Assert.AreEqual(instrument.FrequencyModulationLfo * preset.FrequencyModulationLfo, pair.FrequencyModulationLfo, 1.0E-3);
            Assert.AreEqual(instrument.DelayVibratoLfo * preset.DelayVibratoLfo, pair.DelayVibratoLfo, 1.0E-3);
            Assert.AreEqual(instrument.FrequencyVibratoLfo * preset.FrequencyVibratoLfo, pair.FrequencyVibratoLfo, 1.0E-3);
            Assert.AreEqual(instrument.DelayModulationEnvelope * preset.DelayModulationEnvelope, pair.DelayModulationEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.AttackModulationEnvelope * preset.AttackModulationEnvelope, pair.AttackModulationEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.HoldModulationEnvelope * preset.HoldModulationEnvelope, pair.HoldModulationEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.DecayModulationEnvelope * preset.DecayModulationEnvelope, pair.DecayModulationEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.SustainModulationEnvelope + preset.SustainModulationEnvelope, pair.SustainModulationEnvelope, 1.0E-2);
            Assert.AreEqual(instrument.ReleaseModulationEnvelope * preset.ReleaseModulationEnvelope, pair.ReleaseModulationEnvelope, 1.0E-2);
            Assert.AreEqual(instrument.KeyNumberToModulationEnvelopeHold + preset.KeyNumberToModulationEnvelopeHold, pair.KeyNumberToModulationEnvelopeHold);
            Assert.AreEqual(instrument.KeyNumberToModulationEnvelopeDecay + preset.KeyNumberToModulationEnvelopeDecay, pair.KeyNumberToModulationEnvelopeDecay);
            Assert.AreEqual(instrument.DelayVolumeEnvelope * preset.DelayVolumeEnvelope, pair.DelayVolumeEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.AttackVolumeEnvelope * preset.AttackVolumeEnvelope, pair.AttackVolumeEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.HoldVolumeEnvelope * preset.HoldVolumeEnvelope, pair.HoldVolumeEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.DecayVolumeEnvelope * preset.DecayVolumeEnvelope, pair.DecayVolumeEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.SustainVolumeEnvelope + preset.SustainVolumeEnvelope, pair.SustainVolumeEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.ReleaseVolumeEnvelope * preset.ReleaseVolumeEnvelope, pair.ReleaseVolumeEnvelope, 1.0E-3);
            Assert.AreEqual(instrument.KeyNumberToVolumeEnvelopeHold + preset.KeyNumberToVolumeEnvelopeHold, pair.KeyNumberToVolumeEnvelopeHold);
            Assert.AreEqual(instrument.KeyNumberToVolumeEnvelopeDecay + preset.KeyNumberToVolumeEnvelopeDecay, pair.KeyNumberToVolumeEnvelopeDecay);

            Assert.AreEqual(instrument.InitialAttenuation + preset.InitialAttenuation, pair.InitialAttenuation, 1.0E-3);

            Assert.AreEqual(instrument.CoarseTune + preset.CoarseTune, pair.CoarseTune);
            Assert.AreEqual(instrument.FineTune + preset.FineTune, pair.FineTune);
            Assert.AreEqual(instrument.SampleModes, pair.SampleModes);

            Assert.AreEqual(instrument.ScaleTuning + preset.ScaleTuning, pair.ScaleTuning);
            Assert.AreEqual(instrument.ExclusiveClass, pair.ExclusiveClass);
            Assert.AreEqual(instrument.OverridingRootKey, pair.OverridingRootKey);
        }
    }
}
