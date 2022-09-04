using System;
using System.IO;
using System.Linq;

namespace MeltySynth
{
    /// <summary>
    /// Represents an instrument region.
    /// </summary>
    /// <remarks>
    /// An instrument region contains all the parameters necessary to synthesize a note.
    /// </remarks>
    public sealed class InstrumentRegion
    {
        internal static readonly InstrumentRegion Default = new InstrumentRegion();

        private readonly short[] gs;

        private readonly SampleHeader sample;

        private InstrumentRegion()
        {
            gs = new short[61];
            gs[(int)GeneratorType.InitialFilterCutoffFrequency] = 13500;
            gs[(int)GeneratorType.DelayModulationLfo] = -12000;
            gs[(int)GeneratorType.DelayVibratoLfo] = -12000;
            gs[(int)GeneratorType.DelayModulationEnvelope] = -12000;
            gs[(int)GeneratorType.AttackModulationEnvelope] = -12000;
            gs[(int)GeneratorType.HoldModulationEnvelope] = -12000;
            gs[(int)GeneratorType.DecayModulationEnvelope] = -12000;
            gs[(int)GeneratorType.ReleaseModulationEnvelope] = -12000;
            gs[(int)GeneratorType.DelayVolumeEnvelope] = -12000;
            gs[(int)GeneratorType.AttackVolumeEnvelope] = -12000;
            gs[(int)GeneratorType.HoldVolumeEnvelope] = -12000;
            gs[(int)GeneratorType.DecayVolumeEnvelope] = -12000;
            gs[(int)GeneratorType.ReleaseVolumeEnvelope] = -12000;
            gs[(int)GeneratorType.KeyRange] = 0x7F00;
            gs[(int)GeneratorType.VelocityRange] = 0x7F00;
            gs[(int)GeneratorType.KeyNumber] = -1;
            gs[(int)GeneratorType.Velocity] = -1;
            gs[(int)GeneratorType.ScaleTuning] = 100;
            gs[(int)GeneratorType.OverridingRootKey] = -1;

            sample = SampleHeader.Default;
        }

        private InstrumentRegion(Instrument instrument, ArraySegment<Generator> global, ArraySegment<Generator> local, SampleHeader[] samples) : this()
        {
            foreach (var parameter in global)
            {
                SetParameter(parameter);
            }

            foreach (var parameter in local)
            {
                SetParameter(parameter);
            }

            var id = gs[(int)GeneratorType.SampleID];
            if (!(0 <= id && id < samples.Length))
            {
                throw new InvalidDataException($"The instrument '{instrument.Name}' contains an invalid sample ID '{id}'.");
            }

            sample = samples[id];
        }

        internal static InstrumentRegion[] Create(Instrument instrument, Span<Zone> zones, SampleHeader[] samples)
        {
            Zone global = null;

            // Is the first one the global zone?
            if (zones[0].Generators.Count == 0 || zones[0].Generators.Last().Type != GeneratorType.SampleID)
            {
                // The first one is the global zone.
                global = zones[0];
            }

            if (global != null)
            {
                // The global zone is regarded as the base setting of subsequent zones.
                var regions = new InstrumentRegion[zones.Length - 1];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new InstrumentRegion(instrument, global.Generators, zones[i + 1].Generators, samples);
                }
                return regions;
            }
            else
            {
                // No global zone.
                var regions = new InstrumentRegion[zones.Length];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new InstrumentRegion(instrument, ArraySegment<Generator>.Empty, zones[i].Generators, samples);
                }
                return regions;
            }
        }

        private void SetParameter(Generator parameter)
        {
            var index = (int)parameter.Type;

            // Unknown generators should be ignored.
            if (0 <= index && index < gs.Length)
            {
                gs[index] = (short)parameter.Value;
            }
        }

        /// <summary>
        /// Checks if the region covers the given key and velocity.
        /// </summary>
        /// <param name="key">The key of a note.</param>
        /// <param name="velocity">The velocity of a note.</param>
        /// <returns>
        /// <c>true</c> if the region covers the given key and velocity.
        /// </returns>
        public bool Contains(int key, int velocity)
        {
            var containsKey = KeyRangeStart <= key && key <= KeyRangeEnd;
            var containsVelocity = VelocityRangeStart <= velocity && velocity <= VelocityRangeEnd;
            return containsKey && containsVelocity;
        }

        /// <summary>
        /// Gets the string representation of the region.
        /// </summary>
        /// <returns>
        /// The string representation of the region.
        /// </returns>
        public override string ToString()
        {
            return $"{sample.Name} (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
        }

        internal short this[GeneratorType generatortType] => gs[(int)generatortType];

        /// <summary>
        /// The sample corresponding to the region.
        /// </summary>
        public SampleHeader Sample => sample;

#pragma warning disable CS1591 // I'm too lazy to add comments for all the following things.

        public int SampleStart => sample.Start + StartAddressOffset;
        public int SampleEnd => sample.End + EndAddressOffset;
        public int SampleStartLoop => sample.StartLoop + StartLoopAddressOffset;
        public int SampleEndLoop => sample.EndLoop + EndLoopAddressOffset;

        public int StartAddressOffset => 32768 * this[GeneratorType.StartAddressCoarseOffset] + this[GeneratorType.StartAddressOffset];
        public int EndAddressOffset => 32768 * this[GeneratorType.EndAddressCoarseOffset] + this[GeneratorType.EndAddressOffset];
        public int StartLoopAddressOffset => 32768 * this[GeneratorType.StartLoopAddressCoarseOffset] + this[GeneratorType.StartLoopAddressOffset];
        public int EndLoopAddressOffset => 32768 * this[GeneratorType.EndLoopAddressCoarseOffset] + this[GeneratorType.EndLoopAddressOffset];

        public int ModulationLfoToPitch => this[GeneratorType.ModulationLfoToPitch];
        public int VibratoLfoToPitch => this[GeneratorType.VibratoLfoToPitch];
        public int ModulationEnvelopeToPitch => this[GeneratorType.ModulationEnvelopeToPitch];
        public float InitialFilterCutoffFrequency => SoundFontMath.CentsToHertz(this[GeneratorType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => 0.1F * this[GeneratorType.InitialFilterQ];
        public int ModulationLfoToFilterCutoffFrequency => this[GeneratorType.ModulationLfoToFilterCutoffFrequency];
        public int ModulationEnvelopeToFilterCutoffFrequency => this[GeneratorType.ModulationEnvelopeToFilterCutoffFrequency];

        public float ModulationLfoToVolume => 0.1F * this[GeneratorType.ModulationLfoToVolume];

        public float ChorusEffectsSend => 0.1F * this[GeneratorType.ChorusEffectsSend];
        public float ReverbEffectsSend => 0.1F * this[GeneratorType.ReverbEffectsSend];
        public float Pan => 0.1F * this[GeneratorType.Pan];

        public float DelayModulationLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorType.DelayModulationLfo]);
        public float FrequencyModulationLfo => SoundFontMath.CentsToHertz(this[GeneratorType.FrequencyModulationLfo]);
        public float DelayVibratoLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorType.DelayVibratoLfo]);
        public float FrequencyVibratoLfo => SoundFontMath.CentsToHertz(this[GeneratorType.FrequencyVibratoLfo]);
        public float DelayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.DelayModulationEnvelope]);
        public float AttackModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.AttackModulationEnvelope]);
        public float HoldModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.HoldModulationEnvelope]);
        public float DecayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.DecayModulationEnvelope]);
        public float SustainModulationEnvelope => 0.1F * this[GeneratorType.SustainModulationEnvelope];
        public float ReleaseModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.ReleaseModulationEnvelope]);
        public int KeyNumberToModulationEnvelopeHold => this[GeneratorType.KeyNumberToModulationEnvelopeHold];
        public int KeyNumberToModulationEnvelopeDecay => this[GeneratorType.KeyNumberToModulationEnvelopeDecay];
        public float DelayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => 0.1F * this[GeneratorType.SustainVolumeEnvelope];
        public float ReleaseVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorType.ReleaseVolumeEnvelope]);
        public int KeyNumberToVolumeEnvelopeHold => this[GeneratorType.KeyNumberToVolumeEnvelopeHold];
        public int KeyNumberToVolumeEnvelopeDecay => this[GeneratorType.KeyNumberToVolumeEnvelopeDecay];

        public int KeyRangeStart => this[GeneratorType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (this[GeneratorType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => this[GeneratorType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (this[GeneratorType.VelocityRange] >> 8) & 0xFF;

        public float InitialAttenuation => 0.1F * this[GeneratorType.InitialAttenuation];

        public int CoarseTune => this[GeneratorType.CoarseTune];
        public int FineTune => this[GeneratorType.FineTune] + sample.PitchCorrection;
        public LoopMode SampleModes => this[GeneratorType.SampleModes] != 2 ? (LoopMode)this[GeneratorType.SampleModes] : LoopMode.NoLoop;

        public int ScaleTuning => this[GeneratorType.ScaleTuning];
        public int ExclusiveClass => this[GeneratorType.ExclusiveClass];
        public int RootKey => this[GeneratorType.OverridingRootKey] != -1 ? this[GeneratorType.OverridingRootKey] : sample.OriginalPitch;

#pragma warning restore CS1591
    }
}
