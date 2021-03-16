using System;
using System.IO;
using System.Linq;

namespace MeltySynth
{
    public sealed class InstrumentRegion
    {
        public static readonly InstrumentRegion Default = new InstrumentRegion(null, null, null, null);

        private Instrument instrument;
        private SampleHeader sample;
        private short[] gps;

        private InstrumentRegion(Instrument instrument, GeneratorParameter[] global, GeneratorParameter[] local, SampleHeader[] samples)
        {
            this.instrument = instrument;

            gps = new short[61];
            gps[(int)GeneratorParameterType.InitialFilterCutoffFrequency] = 13500;
            gps[(int)GeneratorParameterType.DelayModulationLfo] = -12000;
            gps[(int)GeneratorParameterType.DelayVibratoLfo] = -12000;
            gps[(int)GeneratorParameterType.DelayModulationEnvelope] = -12000;
            gps[(int)GeneratorParameterType.AttackModulationEnvelope] = -12000;
            gps[(int)GeneratorParameterType.HoldModulationEnvelope] = -12000;
            gps[(int)GeneratorParameterType.DecayModulationEnvelope] = -12000;
            gps[(int)GeneratorParameterType.ReleaseModulationEnvelope] = -12000;
            gps[(int)GeneratorParameterType.DelayVolumeEnvelope] = -12000;
            gps[(int)GeneratorParameterType.AttackVolumeEnvelope] = -12000;
            gps[(int)GeneratorParameterType.HoldVolumeEnvelope] = -12000;
            gps[(int)GeneratorParameterType.DecayVolumeEnvelope] = -12000;
            gps[(int)GeneratorParameterType.ReleaseVolumeEnvelope] = -12000;
            gps[(int)GeneratorParameterType.KeyRange] = 0x7F00;
            gps[(int)GeneratorParameterType.VelocityRange] = 0x7F00;
            gps[(int)GeneratorParameterType.KeyNumber] = -1;
            gps[(int)GeneratorParameterType.Velocity] = -1;
            gps[(int)GeneratorParameterType.ScaleTuning] = 100;
            gps[(int)GeneratorParameterType.OverridingRootKey] = -1;

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

            if (samples != null)
            {
                var id = gps[(int)GeneratorParameterType.SampleID];
                if (!(0 <= id && id < samples.Length))
                {
                    throw new InvalidDataException($"The instrument {instrument.Name} contains an invalid sample ID.");
                }
                sample = samples[id];
            }
        }

        internal static InstrumentRegion[] Create(Instrument instrument, Zone[] zones, SampleHeader[] samples)
        {
            Zone global = null;

            // Is the first one the global zone?
            if (zones[0].GeneratorParameters.Length == 0 || zones[0].GeneratorParameters.Last().Type != GeneratorParameterType.SampleID)
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
                    regions[i] = new InstrumentRegion(instrument, global.GeneratorParameters, zones[i + 1].GeneratorParameters, samples);
                }
                return regions;
            }
            else
            {
                // No global zone.
                var regions = new InstrumentRegion[zones.Length];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new InstrumentRegion(instrument, null, zones[i].GeneratorParameters, samples);
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
            if (sample != null)
            {
                return $"{sample.Name} (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
            }
            else
            {
                return $"Default (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
            }
        }

        internal short this[GeneratorParameterType generatortType] => gps[(int)generatortType];

        public Instrument Instrument => instrument;
        public SampleHeader Sample => sample;

        public int StartAddressOffset => 32768 * gps[(int)GeneratorParameterType.StartAddressCoarseOffset] + gps[(int)GeneratorParameterType.StartAddressOffset];
        public int EndAddressOffset => 32768 * gps[(int)GeneratorParameterType.EndAddressCoarseOffset] + gps[(int)GeneratorParameterType.EndAddressOffset];
        public int StartLoopAddressOffset => 32768 * gps[(int)GeneratorParameterType.StartLoopAddressCoarseOffset] + gps[(int)GeneratorParameterType.StartLoopAddressOffset];
        public int EndLoopAddressOffset => 32768 * gps[(int)GeneratorParameterType.EndLoopAddressCoarseOffset] + gps[(int)GeneratorParameterType.EndLoopAddressOffset];

        public int ModulationLfoToPitch => gps[(int)GeneratorParameterType.ModulationLfoToPitch];
        public int VibratoLfoToPitch => gps[(int)GeneratorParameterType.VibratoLfoToPitch];
        public int ModulationEnvelopeToPitch => gps[(int)GeneratorParameterType.ModulationEnvelopeToPitch];
        public float InitialFilterCutoffFrequency => SoundFontMath.CentsToHertz(gps[(int)GeneratorParameterType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => gps[(int)GeneratorParameterType.InitialFilterQ] / 10F;
        public int ModulationLfoToFilterCutoffFrequency => gps[(int)GeneratorParameterType.ModulationLfoToFilterCutoffFrequency];
        public int ModulationEnvelopeToFilterCutoffFrequency => gps[(int)GeneratorParameterType.ModulationEnvelopeToFilterCutoffFrequency];

        public float ModulationLfoToVolume => gps[(int)GeneratorParameterType.ModulationLfoToVolume] / 10F;

        public float ChorusEffectsSend => gps[(int)GeneratorParameterType.ChorusEffectsSend] / 10F;
        public float ReverbEffectsSend => gps[(int)GeneratorParameterType.ReverbEffectsSend] / 10F;
        public float Pan => gps[(int)GeneratorParameterType.Pan] / 10F;

        public float DelayModulationLfo => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.DelayModulationLfo]);
        public float FrequencyModulationLfo => SoundFontMath.CentsToHertz(gps[(int)GeneratorParameterType.FrequencyModulationLfo]);
        public float DelayVibratoLfo => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.DelayVibratoLfo]);
        public float FrequencyVibratoLfo => SoundFontMath.CentsToHertz(gps[(int)GeneratorParameterType.FrequencyVibratoLfo]);
        public float DelayModulationEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.DelayModulationEnvelope]);
        public float AttackModulationEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.AttackModulationEnvelope]);
        public float HoldModulationEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.HoldModulationEnvelope]);
        public float DecayModulationEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.DecayModulationEnvelope]);
        public float SustainModulationEnvelope => gps[(int)GeneratorParameterType.SustainModulationEnvelope] / 10F;
        public float ReleaseModulationEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.ReleaseModulationEnvelope]);
        public int KeyNumberToModulationEnvelopeHold => gps[(int)GeneratorParameterType.KeyNumberToModulationEnvelopeHold];
        public int KeyNumberToModulationEnvelopeDecay => gps[(int)GeneratorParameterType.KeyNumberToModulationEnvelopeDecay];
        public float DelayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => gps[(int)GeneratorParameterType.SustainVolumeEnvelope] / 10F;
        public float ReleaseVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gps[(int)GeneratorParameterType.ReleaseVolumeEnvelope]);
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
