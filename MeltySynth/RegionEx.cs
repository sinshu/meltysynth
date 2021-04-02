using System;

namespace MeltySynth
{
    internal static class RegionEx
    {
        internal static void Start(this Generator generator, short[] data, InstrumentRegion region)
        {
            Start(generator, data, new RegionPair(PresetRegion.Default, region));
        }

        internal static void Start(this Generator generator, short[] data, RegionPair region)
        {
            var loopMode = region.SampleModes;
            var start = region.SampleStart;
            var end = region.SampleEnd;
            var startLoop = region.SampleStartLoop;
            var endLoop = region.SampleEndLoop;

            generator.Start(data, loopMode, start, end, startLoop, endLoop);
        }

        internal static void Start(this VolumeEnvelope envelope, InstrumentRegion region, int key, int velocity)
        {
            Start(envelope, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        internal static void Start(this VolumeEnvelope envelope, RegionPair region, int key, int velocity)
        {
            var delay = region.DelayVolumeEnvelope;
            var attack = region.AttackVolumeEnvelope;
            var hold = region.HoldVolumeEnvelope;
            var decay = region.DecayVolumeEnvelope;
            var sustain = SoundFontMath.DecibelsToLinear(-region.SustainVolumeEnvelope);
            var release = region.ReleaseVolumeEnvelope;

            envelope.Start(delay, attack, hold, decay, sustain, release);
        }

        internal static void Start(this ModulationEnvelope envelope, InstrumentRegion region, int key, int velocity)
        {
            Start(envelope, new RegionPair(PresetRegion.Default, region), key, velocity);
        }

        internal static void Start(this ModulationEnvelope envelope, RegionPair region, int key, int velocity)
        {
            var delay = region.DelayModulationEnvelope;
            var attack = region.AttackModulationEnvelope;
            var hold = region.HoldModulationEnvelope;
            var decay = region.DecayModulationEnvelope;
            var sustain = 1F - region.SustainModulationEnvelope / 100F;
            var release = region.ReleaseModulationEnvelope;

            envelope.Start(delay, attack, hold, decay, sustain, release);
        }
    }
}
