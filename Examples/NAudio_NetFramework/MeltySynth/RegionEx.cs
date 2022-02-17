using System;

namespace MeltySynth
{
    internal static class RegionEx
    {
        public static void Start(this Oscillator oscillator, short[] data, InstrumentRegion region)
        {
            Start(oscillator, data, new RegionPair(PresetRegion.Default, region));
        }

        public static void Start(this Oscillator oscillator, short[] data, RegionPair region)
        {
            var sampleRate = region.Instrument.Sample.SampleRate;
            var loopMode = region.SampleModes;
            var start = region.SampleStart;
            var end = region.SampleEnd;
            var startLoop = region.SampleStartLoop;
            var endLoop = region.SampleEndLoop;
            var rootKey = region.RootKey;
            var coarseTune = region.CoarseTune;
            var fineTune = region.FineTune;
            var scaleTuning = region.ScaleTuning;

            oscillator.Start(data, loopMode, sampleRate, start, end, startLoop, endLoop, rootKey, coarseTune, fineTune, scaleTuning);
        }

        public static void Start(this VolumeEnvelope envelope, InstrumentRegion region, int key, int velocity)
        {
            Start(envelope, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        public static void Start(this VolumeEnvelope envelope, RegionPair region, int key, int velocity)
        {
            // If the release time is shorter than 10 ms, it will be clamped to 10 ms to avoid pop noise.

            var delay = region.DelayVolumeEnvelope;
            var attack = region.AttackVolumeEnvelope;
            var hold = region.HoldVolumeEnvelope * SoundFontMath.KeyNumberToMultiplyingFactor(region.KeyNumberToVolumeEnvelopeHold, key);
            var decay = region.DecayVolumeEnvelope * SoundFontMath.KeyNumberToMultiplyingFactor(region.KeyNumberToVolumeEnvelopeDecay, key);
            var sustain = SoundFontMath.DecibelsToLinear(-region.SustainVolumeEnvelope);
            var release = Math.Max(region.ReleaseVolumeEnvelope, 0.01F);

            envelope.Start(delay, attack, hold, decay, sustain, release);
        }

        public static void Start(this ModulationEnvelope envelope, InstrumentRegion region, int key, int velocity)
        {
            Start(envelope, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        public static void Start(this ModulationEnvelope envelope, RegionPair region, int key, int velocity)
        {
            // According to the implementation of TinySoundFont, the attack time should be adjusted by the velocity.

            var delay = region.DelayModulationEnvelope;
            var attack = region.AttackModulationEnvelope * ((145 - velocity) / 144F);
            var hold = region.HoldModulationEnvelope * SoundFontMath.KeyNumberToMultiplyingFactor(region.KeyNumberToModulationEnvelopeHold, key);
            var decay = region.DecayModulationEnvelope * SoundFontMath.KeyNumberToMultiplyingFactor(region.KeyNumberToModulationEnvelopeDecay, key);
            var sustain = 1F - region.SustainModulationEnvelope / 100F;
            var release = region.ReleaseModulationEnvelope;

            envelope.Start(delay, attack, hold, decay, sustain, release);
        }

        public static void StartVibrato(this Lfo lfo, InstrumentRegion region, int key, int velocity)
        {
            StartVibrato(lfo, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        public static void StartVibrato(this Lfo lfo, RegionPair region, int key, int velocity)
        {
            lfo.Start(region.DelayVibratoLfo, region.FrequencyVibratoLfo);
        }

        public static void StartModulation(this Lfo lfo, InstrumentRegion region, int key, int velocity)
        {
            StartModulation(lfo, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        public static void StartModulation(this Lfo lfo, RegionPair region, int key, int velocity)
        {
            lfo.Start(region.DelayModulationLfo, region.FrequencyModulationLfo);
        }
    }
}
