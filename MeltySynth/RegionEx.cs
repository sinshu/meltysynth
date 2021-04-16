using System;

namespace MeltySynth
{
    internal static class RegionEx
    {
        public static void Start(this Generator generator, short[] data, InstrumentRegion region)
        {
            Start(generator, data, new RegionPair(PresetRegion.Default, region));
        }

        public static void Start(this Generator generator, short[] data, RegionPair region)
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

            generator.Start(data, loopMode, sampleRate, start, end, startLoop, endLoop, rootKey, coarseTune, fineTune);
        }

        public static void Start(this VolumeEnvelope envelope, InstrumentRegion region, int key, int velocity)
        {
            Start(envelope, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        public static void Start(this VolumeEnvelope envelope, RegionPair region, int key, int velocity)
        {
            var delay = region.DelayVolumeEnvelope;
            var attack = region.AttackVolumeEnvelope;
            var hold = region.HoldVolumeEnvelope;
            var decay = region.DecayVolumeEnvelope;
            var sustain = SoundFontMath.DecibelsToLinear(-region.SustainVolumeEnvelope);
            var release = region.ReleaseVolumeEnvelope;

            envelope.Start(delay, attack, hold, decay, sustain, release);
        }

        public static void Start(this ModulationEnvelope envelope, InstrumentRegion region, int key, int velocity)
        {
            Start(envelope, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        public static void Start(this ModulationEnvelope envelope, RegionPair region, int key, int velocity)
        {
            var delay = region.DelayModulationEnvelope;
            var attack = region.AttackModulationEnvelope * ((145 - velocity) / 144F);
            var hold = region.HoldModulationEnvelope;
            var decay = region.DecayModulationEnvelope;
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
