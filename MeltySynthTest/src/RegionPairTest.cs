using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MeltySynth;

namespace MeltySynthTest
{
    public class RegionPairTest
    {
        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFonts))]
        public void DefaultPresetRegionDoesNotAffect(string soundFontName, SoundFont soundFont)
        {
            foreach (var instrument in soundFont.Instruments)
            {
                foreach (var region in instrument.Regions)
                {
                    var pair = new RegionPair(PresetRegion.Default, region);
                    AreEqual(region, pair);
                }
            }
        }

        [TestCaseSource(typeof(TestSettings), nameof(TestSettings.SoundFonts))]
        public void ParameterCheck(string soundFontName, SoundFont soundFont)
        {
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
            Assert.That(expected.StartAddressOffset, Is.EqualTo(actual.StartAddressOffset));
            Assert.That(expected.EndAddressOffset, Is.EqualTo(actual.EndAddressOffset));
            Assert.That(expected.StartLoopAddressOffset, Is.EqualTo(actual.StartLoopAddressOffset));
            Assert.That(expected.EndLoopAddressOffset, Is.EqualTo(actual.EndLoopAddressOffset));

            Assert.That(expected.ModulationLfoToPitch, Is.EqualTo(actual.ModulationLfoToPitch));
            Assert.That(expected.VibratoLfoToPitch, Is.EqualTo(actual.VibratoLfoToPitch));
            Assert.That(expected.ModulationEnvelopeToPitch, Is.EqualTo(actual.ModulationEnvelopeToPitch));
            Assert.That(expected.InitialFilterCutoffFrequency, Is.EqualTo(actual.InitialFilterCutoffFrequency).Within(1.0E-6));
            Assert.That(expected.InitialFilterQ, Is.EqualTo(actual.InitialFilterQ).Within(1.0E-6));
            Assert.That(expected.ModulationLfoToFilterCutoffFrequency, Is.EqualTo(actual.ModulationLfoToFilterCutoffFrequency));
            Assert.That(expected.ModulationEnvelopeToFilterCutoffFrequency, Is.EqualTo(actual.ModulationEnvelopeToFilterCutoffFrequency));

            Assert.That(expected.ModulationLfoToVolume, Is.EqualTo(actual.ModulationLfoToVolume).Within(1.0E-6));

            Assert.That(expected.ChorusEffectsSend, Is.EqualTo(actual.ChorusEffectsSend).Within(1.0E-6));
            Assert.That(expected.ReverbEffectsSend, Is.EqualTo(actual.ReverbEffectsSend).Within(1.0E-6));
            Assert.That(expected.Pan, Is.EqualTo(actual.Pan).Within(1.0E-6));

            Assert.That(expected.DelayModulationLfo, Is.EqualTo(actual.DelayModulationLfo).Within(1.0E-6));
            Assert.That(expected.FrequencyModulationLfo, Is.EqualTo(actual.FrequencyModulationLfo).Within(1.0E-6));
            Assert.That(expected.DelayVibratoLfo, Is.EqualTo(actual.DelayVibratoLfo).Within(1.0E-6));
            Assert.That(expected.FrequencyVibratoLfo, Is.EqualTo(actual.FrequencyVibratoLfo).Within(1.0E-6));
            Assert.That(expected.DelayModulationEnvelope, Is.EqualTo(actual.DelayModulationEnvelope).Within(1.0E-6));
            Assert.That(expected.AttackModulationEnvelope, Is.EqualTo(actual.AttackModulationEnvelope).Within(1.0E-6));
            Assert.That(expected.HoldModulationEnvelope, Is.EqualTo(actual.HoldModulationEnvelope).Within(1.0E-6));
            Assert.That(expected.DecayModulationEnvelope, Is.EqualTo(actual.DecayModulationEnvelope).Within(1.0E-6));
            Assert.That(expected.SustainModulationEnvelope, Is.EqualTo(actual.SustainModulationEnvelope).Within(1.0E-6));
            Assert.That(expected.ReleaseModulationEnvelope, Is.EqualTo(actual.ReleaseModulationEnvelope).Within(1.0E-6));
            Assert.That(expected.KeyNumberToModulationEnvelopeHold, Is.EqualTo(actual.KeyNumberToModulationEnvelopeHold));
            Assert.That(expected.KeyNumberToModulationEnvelopeDecay, Is.EqualTo(actual.KeyNumberToModulationEnvelopeDecay));
            Assert.That(expected.DelayVolumeEnvelope, Is.EqualTo(actual.DelayVolumeEnvelope).Within(1.0E-6));
            Assert.That(expected.AttackVolumeEnvelope, Is.EqualTo(actual.AttackVolumeEnvelope).Within(1.0E-6));
            Assert.That(expected.HoldVolumeEnvelope, Is.EqualTo(actual.HoldVolumeEnvelope).Within(1.0E-6));
            Assert.That(expected.DecayVolumeEnvelope, Is.EqualTo(actual.DecayVolumeEnvelope).Within(1.0E-6));
            Assert.That(expected.SustainVolumeEnvelope, Is.EqualTo(actual.SustainVolumeEnvelope).Within(1.0E-6));
            Assert.That(expected.ReleaseVolumeEnvelope, Is.EqualTo(actual.ReleaseVolumeEnvelope).Within(1.0E-6));
            Assert.That(expected.KeyNumberToVolumeEnvelopeHold, Is.EqualTo(actual.KeyNumberToVolumeEnvelopeHold));
            Assert.That(expected.KeyNumberToVolumeEnvelopeDecay, Is.EqualTo(actual.KeyNumberToVolumeEnvelopeDecay));

            Assert.That(expected.InitialAttenuation, Is.EqualTo(actual.InitialAttenuation).Within(1.0E-6));

            Assert.That(expected.CoarseTune, Is.EqualTo(actual.CoarseTune));
            Assert.That(expected.FineTune, Is.EqualTo(actual.FineTune));
            Assert.That(expected.SampleModes, Is.EqualTo(actual.SampleModes));

            Assert.That(expected.ScaleTuning, Is.EqualTo(actual.ScaleTuning));
            Assert.That(expected.ExclusiveClass, Is.EqualTo(actual.ExclusiveClass));
            Assert.That(expected.RootKey, Is.EqualTo(actual.RootKey));
        }

        private static void Check(PresetRegion preset, InstrumentRegion instrument, RegionPair pair)
        {
            Assert.That(instrument.StartAddressOffset, Is.EqualTo(pair.StartAddressOffset));
            Assert.That(instrument.EndAddressOffset, Is.EqualTo(pair.EndAddressOffset));
            Assert.That(instrument.StartLoopAddressOffset, Is.EqualTo(pair.StartLoopAddressOffset));
            Assert.That(instrument.EndLoopAddressOffset, Is.EqualTo(pair.EndLoopAddressOffset));

            Assert.That(instrument.ModulationLfoToPitch + preset.ModulationLfoToPitch, Is.EqualTo(pair.ModulationLfoToPitch));
            Assert.That(instrument.VibratoLfoToPitch + preset.VibratoLfoToPitch, Is.EqualTo(pair.VibratoLfoToPitch));
            Assert.That(instrument.ModulationEnvelopeToPitch + preset.ModulationEnvelopeToPitch, Is.EqualTo(pair.ModulationEnvelopeToPitch));
            Assert.That(instrument.InitialFilterCutoffFrequency * preset.InitialFilterCutoffFrequency, Is.EqualTo(pair.InitialFilterCutoffFrequency).Within(1.0));
            Assert.That(instrument.InitialFilterQ + preset.InitialFilterQ, Is.EqualTo(pair.InitialFilterQ).Within(1.0E-3));
            Assert.That(instrument.ModulationLfoToFilterCutoffFrequency + preset.ModulationLfoToFilterCutoffFrequency, Is.EqualTo(pair.ModulationLfoToFilterCutoffFrequency));
            Assert.That(instrument.ModulationEnvelopeToFilterCutoffFrequency + preset.ModulationEnvelopeToFilterCutoffFrequency, Is.EqualTo(pair.ModulationEnvelopeToFilterCutoffFrequency));

            Assert.That(instrument.ModulationLfoToVolume + preset.ModulationLfoToVolume, Is.EqualTo(pair.ModulationLfoToVolume).Within(1.0E-3));

            Assert.That(instrument.ChorusEffectsSend + preset.ChorusEffectsSend, Is.EqualTo(pair.ChorusEffectsSend).Within(1.0E-3));
            Assert.That(instrument.ReverbEffectsSend + preset.ReverbEffectsSend, Is.EqualTo(pair.ReverbEffectsSend).Within(1.0E-3));
            Assert.That(instrument.Pan + preset.Pan, Is.EqualTo(pair.Pan).Within(1.0E-3));

            Assert.That(instrument.DelayModulationLfo * preset.DelayModulationLfo, Is.EqualTo(pair.DelayModulationLfo).Within(1.0E-3));
            Assert.That(instrument.FrequencyModulationLfo * preset.FrequencyModulationLfo, Is.EqualTo(pair.FrequencyModulationLfo).Within(1.0E-3));
            Assert.That(instrument.DelayVibratoLfo * preset.DelayVibratoLfo, Is.EqualTo(pair.DelayVibratoLfo).Within(1.0E-3));
            Assert.That(instrument.FrequencyVibratoLfo * preset.FrequencyVibratoLfo, Is.EqualTo(pair.FrequencyVibratoLfo).Within(1.0E-3));
            Assert.That(instrument.DelayModulationEnvelope * preset.DelayModulationEnvelope, Is.EqualTo(pair.DelayModulationEnvelope).Within(1.0E-3));
            Assert.That(instrument.AttackModulationEnvelope * preset.AttackModulationEnvelope, Is.EqualTo(pair.AttackModulationEnvelope).Within(1.0E-3));
            Assert.That(instrument.HoldModulationEnvelope * preset.HoldModulationEnvelope, Is.EqualTo(pair.HoldModulationEnvelope).Within(1.0E-3));
            Assert.That(instrument.DecayModulationEnvelope * preset.DecayModulationEnvelope, Is.EqualTo(pair.DecayModulationEnvelope).Within(1.0E-3));
            Assert.That(instrument.SustainModulationEnvelope + preset.SustainModulationEnvelope, Is.EqualTo(pair.SustainModulationEnvelope).Within(1.0E-2));
            Assert.That(instrument.ReleaseModulationEnvelope * preset.ReleaseModulationEnvelope, Is.EqualTo(pair.ReleaseModulationEnvelope).Within(1.0E-2));
            Assert.That(instrument.KeyNumberToModulationEnvelopeHold + preset.KeyNumberToModulationEnvelopeHold, Is.EqualTo(pair.KeyNumberToModulationEnvelopeHold));
            Assert.That(instrument.KeyNumberToModulationEnvelopeDecay + preset.KeyNumberToModulationEnvelopeDecay, Is.EqualTo(pair.KeyNumberToModulationEnvelopeDecay));
            Assert.That(instrument.DelayVolumeEnvelope * preset.DelayVolumeEnvelope, Is.EqualTo(pair.DelayVolumeEnvelope).Within(1.0E-3));
            Assert.That(instrument.AttackVolumeEnvelope * preset.AttackVolumeEnvelope, Is.EqualTo(pair.AttackVolumeEnvelope).Within(1.0E-3));
            Assert.That(instrument.HoldVolumeEnvelope * preset.HoldVolumeEnvelope, Is.EqualTo(pair.HoldVolumeEnvelope).Within(1.0E-3));
            Assert.That(instrument.DecayVolumeEnvelope * preset.DecayVolumeEnvelope, Is.EqualTo(pair.DecayVolumeEnvelope).Within(1.0E-3));
            Assert.That(instrument.SustainVolumeEnvelope + preset.SustainVolumeEnvelope, Is.EqualTo(pair.SustainVolumeEnvelope).Within(1.0E-3));
            Assert.That(instrument.ReleaseVolumeEnvelope * preset.ReleaseVolumeEnvelope, Is.EqualTo(pair.ReleaseVolumeEnvelope).Within(1.0E-3));
            Assert.That(instrument.KeyNumberToVolumeEnvelopeHold + preset.KeyNumberToVolumeEnvelopeHold, Is.EqualTo(pair.KeyNumberToVolumeEnvelopeHold));
            Assert.That(instrument.KeyNumberToVolumeEnvelopeDecay + preset.KeyNumberToVolumeEnvelopeDecay, Is.EqualTo(pair.KeyNumberToVolumeEnvelopeDecay));

            Assert.That(instrument.InitialAttenuation + preset.InitialAttenuation, Is.EqualTo(pair.InitialAttenuation).Within(1.0E-3));

            Assert.That(instrument.CoarseTune + preset.CoarseTune, Is.EqualTo(pair.CoarseTune));
            Assert.That(instrument.FineTune + preset.FineTune, Is.EqualTo(pair.FineTune));
            Assert.That(instrument.SampleModes, Is.EqualTo(pair.SampleModes));

            Assert.That(instrument.ScaleTuning + preset.ScaleTuning, Is.EqualTo(pair.ScaleTuning));
            Assert.That(instrument.ExclusiveClass, Is.EqualTo(pair.ExclusiveClass));
            Assert.That(instrument.RootKey, Is.EqualTo(pair.RootKey));
        }
    }
}
