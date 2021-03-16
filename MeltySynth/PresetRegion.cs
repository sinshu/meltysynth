using System;
using System.IO;
using System.Linq;

namespace MeltySynth
{
    public sealed class PresetRegion
    {
        public static readonly PresetRegion Default = new PresetRegion(null, null, null, null);

        private Instrument instrument;

        private short[] gps;

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
                    throw new InvalidDataException($"The preset {preset.Name} contains an invalid instrument ID.");
                }
                instrument = instruments[id];
            }
        }

        internal static PresetRegion[] Create(Preset preset, Zone[] zones, Instrument[] instruments)
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

        public override string ToString()
        {
            if (instrument != null)
            {
                return $"{instrument.Name} (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
            }
            else
            {
                return $"Default (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
            }
        }

        private static float CentsToMultiplyingFactor(int x)
        {
            return MathF.Pow(2F, x / 1200F);
        }

        internal short this[GeneratorParameterType generatortType] => gps[(int)generatortType];

        public Instrument Instrument => instrument;

        public int StartAddressOffset => 32768 * gps[(int)GeneratorParameterType.StartAddressCoarseOffset] + gps[(int)GeneratorParameterType.StartAddressOffset];
        public int EndAddressOffset => 32768 * gps[(int)GeneratorParameterType.EndAddressCoarseOffset] + gps[(int)GeneratorParameterType.EndAddressOffset];
        public int StartLoopAddressOffset => 32768 * gps[(int)GeneratorParameterType.StartLoopAddressCoarseOffset] + gps[(int)GeneratorParameterType.StartLoopAddressOffset];
        public int EndLoopAddressOffset => 32768 * gps[(int)GeneratorParameterType.EndLoopAddressCoarseOffset] + gps[(int)GeneratorParameterType.EndLoopAddressOffset];

        public int ModulationLfoToPitch => gps[(int)GeneratorParameterType.ModulationLfoToPitch];
        public int VibratoLfoToPitch => gps[(int)GeneratorParameterType.VibratoLfoToPitch];
        public int ModulationEnvelopeToPitch => gps[(int)GeneratorParameterType.ModulationEnvelopeToPitch];
        public float InitialFilterCutoffFrequency => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => gps[(int)GeneratorParameterType.InitialFilterQ] / 10F;
        public int ModulationLfoToFilterCutoffFrequency => gps[(int)GeneratorParameterType.ModulationLfoToFilterCutoffFrequency];
        public int ModulationEnvelopeToFilterCutoffFrequency => gps[(int)GeneratorParameterType.ModulationEnvelopeToFilterCutoffFrequency];

        public float ModulationLfoToVolume => gps[(int)GeneratorParameterType.ModulationLfoToVolume] / 10F;

        public float ChorusEffectsSend => gps[(int)GeneratorParameterType.ChorusEffectsSend] / 10F;
        public float ReverbEffectsSend => gps[(int)GeneratorParameterType.ReverbEffectsSend] / 10F;
        public float Pan => gps[(int)GeneratorParameterType.Pan] / 10F;

        public float DelayModulationLfo => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.DelayModulationLfo]);
        public float FrequencyModulationLfo => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.FrequencyModulationLfo]);
        public float DelayVibratoLfo => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.DelayVibratoLfo]);
        public float FrequencyVibratoLfo => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.FrequencyVibratoLfo]);
        public float DelayModulationEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.DelayModulationEnvelope]);
        public float AttackModulationEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.AttackModulationEnvelope]);
        public float HoldModulationEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.HoldModulationEnvelope]);
        public float DecayModulationEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.DecayModulationEnvelope]);
        public float SustainModulationEnvelope => gps[(int)GeneratorParameterType.SustainModulationEnvelope] / 10F;
        public float ReleaseModulationEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.ReleaseModulationEnvelope]);
        public int KeyNumberToModulationEnvelopeHold => gps[(int)GeneratorParameterType.KeyNumberToModulationEnvelopeHold];
        public int KeyNumberToModulationEnvelopeDecay => gps[(int)GeneratorParameterType.KeyNumberToModulationEnvelopeDecay];
        public float DelayVolumeEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => gps[(int)GeneratorParameterType.SustainVolumeEnvelope] / 10F;
        public float ReleaseVolumeEnvelope => CentsToMultiplyingFactor(gps[(int)GeneratorParameterType.ReleaseVolumeEnvelope]);
        public int KeyNumberToVolumeEnvelopeHold => gps[(int)GeneratorParameterType.KeyNumberToVolumeEnvelopeHold];
        public int KeyNumberToVolumeEnvelopeDecay => gps[(int)GeneratorParameterType.KeyNumberToVolumeEnvelopeDecay];

        public int KeyRangeStart => gps[(int)GeneratorParameterType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (gps[(int)GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => gps[(int)GeneratorParameterType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (gps[(int)GeneratorParameterType.VelocityRange] >> 8) & 0xFF;

        public float InitialAttenuation => gps[(int)GeneratorParameterType.InitialAttenuation] / 10F;

        public int CoarseTune => gps[(int)GeneratorParameterType.CoarseTune];
        public int FineTune => gps[(int)GeneratorParameterType.FineTune];
        public LoopMode SampleModes => gps[(int)GeneratorParameterType.SampleModes] != 2 ? (LoopMode)gps[(int)GeneratorParameterType.SampleModes] : LoopMode.NoLoop;

        public int ScaleTuning => gps[(int)GeneratorParameterType.ScaleTuning];
        public int ExclusiveClass => gps[(int)GeneratorParameterType.ExclusiveClass];
        public int OverridingRootKey => gps[(int)GeneratorParameterType.OverridingRootKey];
    }
}
