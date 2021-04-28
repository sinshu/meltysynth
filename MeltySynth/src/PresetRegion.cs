using System;
using System.IO;
using System.Linq;

namespace MeltySynth
{
    public sealed class PresetRegion
    {
        internal static readonly PresetRegion Default = new PresetRegion(Preset.Default, null, null, null);

        private readonly Instrument instrument;

        private readonly short[] gps;

        private PresetRegion(Preset preset, GeneratorParameter[] global, GeneratorParameter[] local, Instrument[] instruments)
        {
            gps = new short[61];
            gps[(int)GeneratorParameterType.KeyRange] = 0x7F00;
            gps[(int)GeneratorParameterType.VelocityRange] = 0x7F00;

            if (global != null)
            {
                foreach (var parameter in global)
                {
                    SetParameter(parameter);
                }
            }

            if (local != null)
            {
                foreach (var parameter in local)
                {
                    SetParameter(parameter);
                }
            }

            if (instruments != null)
            {
                var id = gps[(int)GeneratorParameterType.Instrument];
                if (!(0 <= id && id < instruments.Length))
                {
                    throw new InvalidDataException($"The preset '{preset.Name}' contains an invalid instrument ID '{id}'.");
                }
                instrument = instruments[id];
            }
            else
            {
                instrument = Instrument.Default;
            }
        }

        internal static PresetRegion[] Create(Preset preset, Span<Zone> zones, Instrument[] instruments)
        {
            Zone global = null;

            // Is the first one the global zone?
            if (zones[0].GeneratorParameters.Length == 0 || zones[0].GeneratorParameters.Last().Type != GeneratorParameterType.Instrument)
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
                    regions[i] = new PresetRegion(preset, global.GeneratorParameters, zones[i + 1].GeneratorParameters, instruments);
                }
                return regions;
            }
            else
            {
                // No global zone.
                var regions = new PresetRegion[zones.Length];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new PresetRegion(preset, null, zones[i].GeneratorParameters, instruments);
                }
                return regions;
            }
        }

        private void SetParameter(GeneratorParameter parameter)
        {
            gps[(int)parameter.Type] = (short)parameter.Value;
        }

        public bool Contains(int key, int velocity)
        {
            var containsKey = KeyRangeStart <= key && key <= KeyRangeEnd;
            var containsVelocity = VelocityRangeStart <= velocity && velocity <= VelocityRangeEnd;
            return containsKey && containsVelocity;
        }

        public override string ToString()
        {
            return $"{instrument.Name} (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
        }

        internal short this[GeneratorParameterType generatortType] => gps[(int)generatortType];

        public Instrument Instrument => instrument;

        // public int StartAddressOffset => 32768 * this[GeneratorParameterType.StartAddressCoarseOffset] + this[GeneratorParameterType.StartAddressOffset];
        // public int EndAddressOffset => 32768 * this[GeneratorParameterType.EndAddressCoarseOffset] + this[GeneratorParameterType.EndAddressOffset];
        // public int StartLoopAddressOffset => 32768 * this[GeneratorParameterType.StartLoopAddressCoarseOffset] + this[GeneratorParameterType.StartLoopAddressOffset];
        // public int EndLoopAddressOffset => 32768 * this[GeneratorParameterType.EndLoopAddressCoarseOffset] + this[GeneratorParameterType.EndLoopAddressOffset];

        public int ModulationLfoToPitch => this[GeneratorParameterType.ModulationLfoToPitch];
        public int VibratoLfoToPitch => this[GeneratorParameterType.VibratoLfoToPitch];
        public int ModulationEnvelopeToPitch => this[GeneratorParameterType.ModulationEnvelopeToPitch];
        public float InitialFilterCutoffFrequency => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => 0.1F * this[GeneratorParameterType.InitialFilterQ];
        public int ModulationLfoToFilterCutoffFrequency => this[GeneratorParameterType.ModulationLfoToFilterCutoffFrequency];
        public int ModulationEnvelopeToFilterCutoffFrequency => this[GeneratorParameterType.ModulationEnvelopeToFilterCutoffFrequency];

        public float ModulationLfoToVolume => 0.1F * this[GeneratorParameterType.ModulationLfoToVolume];

        public float ChorusEffectsSend => 0.1F * this[GeneratorParameterType.ChorusEffectsSend];
        public float ReverbEffectsSend => 0.1F * this[GeneratorParameterType.ReverbEffectsSend];
        public float Pan => 0.1F * this[GeneratorParameterType.Pan];

        public float DelayModulationLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.DelayModulationLfo]);
        public float FrequencyModulationLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.FrequencyModulationLfo]);
        public float DelayVibratoLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.DelayVibratoLfo]);
        public float FrequencyVibratoLfo => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.FrequencyVibratoLfo]);
        public float DelayModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.DelayModulationEnvelope]);
        public float AttackModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.AttackModulationEnvelope]);
        public float HoldModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.HoldModulationEnvelope]);
        public float DecayModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.DecayModulationEnvelope]);
        public float SustainModulationEnvelope => 0.1F * this[GeneratorParameterType.SustainModulationEnvelope];
        public float ReleaseModulationEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.ReleaseModulationEnvelope]);
        public int KeyNumberToModulationEnvelopeHold => this[GeneratorParameterType.KeyNumberToModulationEnvelopeHold];
        public int KeyNumberToModulationEnvelopeDecay => this[GeneratorParameterType.KeyNumberToModulationEnvelopeDecay];
        public float DelayVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => 0.1F * this[GeneratorParameterType.SustainVolumeEnvelope];
        public float ReleaseVolumeEnvelope => SoundFontMath.CentsToMultiplyingFactor(this[GeneratorParameterType.ReleaseVolumeEnvelope]);
        public int KeyNumberToVolumeEnvelopeHold => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeHold];
        public int KeyNumberToVolumeEnvelopeDecay => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeDecay];

        public int KeyRangeStart => this[GeneratorParameterType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (this[GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => this[GeneratorParameterType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (this[GeneratorParameterType.VelocityRange] >> 8) & 0xFF;

        public float InitialAttenuation => 0.1F * this[GeneratorParameterType.InitialAttenuation];

        public int CoarseTune => this[GeneratorParameterType.CoarseTune];
        public int FineTune => this[GeneratorParameterType.FineTune];
        // public LoopMode SampleModes => this[GeneratorParameterType.SampleModes];

        public int ScaleTuning => this[GeneratorParameterType.ScaleTuning];
        // public int ExclusiveClass => this[GeneratorParameterType.ExclusiveClass];
        // public int RootKey => this[GeneratorParameterType.OverridingRootKey];
    }
}
