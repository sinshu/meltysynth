using System;
using System.IO;
using System.Linq;

namespace MeltySynth
{
    /// <summary>
    /// Represents a preset region.
    /// </summary>
    /// <remarks>
    /// A preset region indicates how the parameters of the instrument should be modified in the preset.
    /// </remarks>
    public sealed class PresetRegion
    {
        internal static readonly PresetRegion Default = new PresetRegion();

        private readonly short[] gs;

        private readonly Instrument instrument;

        private PresetRegion()
        {
            gs = new short[61];
            gs[(int)GeneratorType.KeyRange] = 0x7F00;
            gs[(int)GeneratorType.VelocityRange] = 0x7F00;

            instrument = Instrument.Default;
        }

        private PresetRegion(Preset preset, ArraySegment<Generator> global, ArraySegment<Generator> local, Instrument[] instruments) : this()
        {
            foreach (var parameter in global)
            {
                SetParameter(parameter);
            }

            foreach (var parameter in local)
            {
                SetParameter(parameter);
            }

            var id = gs[(int)GeneratorType.Instrument];
            if (!(0 <= id && id < instruments.Length))
            {
                throw new InvalidDataException($"The preset '{preset.Name}' contains an invalid instrument ID '{id}'.");
            }

            instrument = instruments[id];
        }

        internal static PresetRegion[] Create(Preset preset, Span<Zone> zones, Instrument[] instruments)
        {
            Zone global = null;

            // Is the first one the global zone?
            if (zones[0].Generators.Count == 0 || zones[0].Generators.Last().Type != GeneratorType.Instrument)
            {
                // The first one is the global zone.
                global = zones[0];
            }

            if (global != null)
            {
                // The global zone is regarded as the base setting of subsequent zones.
                var regions = new PresetRegion[zones.Length - 1];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new PresetRegion(preset, global.Generators, zones[i + 1].Generators, instruments);
                }
                return regions;
            }
            else
            {
                // No global zone.
                var regions = new PresetRegion[zones.Length];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new PresetRegion(preset, ArraySegment<Generator>.Empty, zones[i].Generators, instruments);
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
            return $"{instrument.Name} (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
        }

        internal short this[GeneratorType generatortType] => gs[(int)generatortType];

        /// <summary>
        /// The instrument corresponding to the region.
        /// </summary>
        public Instrument Instrument => instrument;

#pragma warning disable CS1591 // I'm too lazy to add comments for all the following things.

        // public int StartAddressOffset => 32768 * this[GeneratorParameterType.StartAddressCoarseOffset] + this[GeneratorParameterType.StartAddressOffset];
        // public int EndAddressOffset => 32768 * this[GeneratorParameterType.EndAddressCoarseOffset] + this[GeneratorParameterType.EndAddressOffset];
        // public int StartLoopAddressOffset => 32768 * this[GeneratorParameterType.StartLoopAddressCoarseOffset] + this[GeneratorParameterType.StartLoopAddressOffset];
        // public int EndLoopAddressOffset => 32768 * this[GeneratorParameterType.EndLoopAddressCoarseOffset] + this[GeneratorParameterType.EndLoopAddressOffset];

        public int ModulationLfoToPitch => this[GeneratorType.ModulationLfoToPitch];
        public int VibratoLfoToPitch => this[GeneratorType.VibratoLfoToPitch];
        public int ModulationEnvelopeToPitch => this[GeneratorType.ModulationEnvelopeToPitch];
        public float InitialFilterCutoffFrequency => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => 0.1F * this[GeneratorType.InitialFilterQ];
        public int ModulationLfoToFilterCutoffFrequency => this[GeneratorType.ModulationLfoToFilterCutoffFrequency];
        public int ModulationEnvelopeToFilterCutoffFrequency => this[GeneratorType.ModulationEnvelopeToFilterCutoffFrequency];

        public float ModulationLfoToVolume => 0.1F * this[GeneratorType.ModulationLfoToVolume];

        public float ChorusEffectsSend => 0.1F * this[GeneratorType.ChorusEffectsSend];
        public float ReverbEffectsSend => 0.1F * this[GeneratorType.ReverbEffectsSend];
        public float Pan => 0.1F * this[GeneratorType.Pan];

        public float DelayModulationLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.DelayModulationLfo]);
        public float FrequencyModulationLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.FrequencyModulationLfo]);
        public float DelayVibratoLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.DelayVibratoLfo]);
        public float FrequencyVibratoLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.FrequencyVibratoLfo]);
        public float DelayModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.DelayModulationEnvelope]);
        public float AttackModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.AttackModulationEnvelope]);
        public float HoldModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.HoldModulationEnvelope]);
        public float DecayModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.DecayModulationEnvelope]);
        public float SustainModulationEnvelope => 0.1F * this[GeneratorType.SustainModulationEnvelope];
        public float ReleaseModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.ReleaseModulationEnvelope]);
        public int KeyNumberToModulationEnvelopeHold => this[GeneratorType.KeyNumberToModulationEnvelopeHold];
        public int KeyNumberToModulationEnvelopeDecay => this[GeneratorType.KeyNumberToModulationEnvelopeDecay];
        public float DelayVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => 0.1F * this[GeneratorType.SustainVolumeEnvelope];
        public float ReleaseVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorType.ReleaseVolumeEnvelope]);
        public int KeyNumberToVolumeEnvelopeHold => this[GeneratorType.KeyNumberToVolumeEnvelopeHold];
        public int KeyNumberToVolumeEnvelopeDecay => this[GeneratorType.KeyNumberToVolumeEnvelopeDecay];

        public int KeyRangeStart => this[GeneratorType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (this[GeneratorType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => this[GeneratorType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (this[GeneratorType.VelocityRange] >> 8) & 0xFF;

        public float InitialAttenuation => 0.1F * this[GeneratorType.InitialAttenuation];

        public int CoarseTune => this[GeneratorType.CoarseTune];
        public int FineTune => this[GeneratorType.FineTune];
        // public LoopMode SampleModes => this[GeneratorParameterType.SampleModes];

        public int ScaleTuning => this[GeneratorType.ScaleTuning];
        // public int ExclusiveClass => this[GeneratorParameterType.ExclusiveClass];
        // public int RootKey => this[GeneratorParameterType.OverridingRootKey];

#pragma warning restore CS1591
    }
}
