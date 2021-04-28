using System;
using System.IO;
using System.Linq;

namespace MeltySynth
{
    public sealed class InstrumentRegion
    {
        internal static readonly InstrumentRegion Default = new InstrumentRegion(Instrument.Default, null, null, null);

        private readonly SampleHeader sample;

        private readonly short[] gps;

        private InstrumentRegion(Instrument instrument, GeneratorParameter[] global, GeneratorParameter[] local, SampleHeader[] samples)
        {
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
                    throw new InvalidDataException($"The instrument '{instrument.Name}' contains an invalid sample ID '{id}'.");
                }
                sample = samples[id];
            }
            else
            {
                sample = SampleHeader.Default;
            }
        }

        internal static InstrumentRegion[] Create(Instrument instrument, Span<Zone> zones, SampleHeader[] samples)
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

        public bool Contains(int key, int velocity)
        {
            var containsKey = KeyRangeStart <= key && key <= KeyRangeEnd;
            var containsVelocity = VelocityRangeStart <= velocity && velocity <= VelocityRangeEnd;
            return containsKey && containsVelocity;
        }

        public override string ToString()
        {
            return $"{sample.Name} (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
        }

        internal short this[GeneratorParameterType generatortType] => gps[(int)generatortType];

        public SampleHeader Sample => sample;

        public int SampleStart => sample.Start + StartAddressOffset;
        public int SampleEnd => sample.End + EndAddressOffset;
        public int SampleStartLoop => sample.StartLoop + StartLoopAddressOffset;
        public int SampleEndLoop => sample.EndLoop + EndLoopAddressOffset;

        public int StartAddressOffset => 32768 * this[GeneratorParameterType.StartAddressCoarseOffset] + this[GeneratorParameterType.StartAddressOffset];
        public int EndAddressOffset => 32768 * this[GeneratorParameterType.EndAddressCoarseOffset] + this[GeneratorParameterType.EndAddressOffset];
        public int StartLoopAddressOffset => 32768 * this[GeneratorParameterType.StartLoopAddressCoarseOffset] + this[GeneratorParameterType.StartLoopAddressOffset];
        public int EndLoopAddressOffset => 32768 * this[GeneratorParameterType.EndLoopAddressCoarseOffset] + this[GeneratorParameterType.EndLoopAddressOffset];

        public int ModulationLfoToPitch => this[GeneratorParameterType.ModulationLfoToPitch];
        public int VibratoLfoToPitch => this[GeneratorParameterType.VibratoLfoToPitch];
        public int ModulationEnvelopeToPitch => this[GeneratorParameterType.ModulationEnvelopeToPitch];
        public float InitialFilterCutoffFrequency => SoundFontMath.CentsToHertz(this[GeneratorParameterType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => 0.1F * this[GeneratorParameterType.InitialFilterQ];
        public int ModulationLfoToFilterCutoffFrequency => this[GeneratorParameterType.ModulationLfoToFilterCutoffFrequency];
        public int ModulationEnvelopeToFilterCutoffFrequency => this[GeneratorParameterType.ModulationEnvelopeToFilterCutoffFrequency];

        public float ModulationLfoToVolume => 0.1F * this[GeneratorParameterType.ModulationLfoToVolume];

        public float ChorusEffectsSend => 0.1F * this[GeneratorParameterType.ChorusEffectsSend];
        public float ReverbEffectsSend => 0.1F * this[GeneratorParameterType.ReverbEffectsSend];
        public float Pan => 0.1F * this[GeneratorParameterType.Pan];

        public float DelayModulationLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayModulationLfo]);
        public float FrequencyModulationLfo => SoundFontMath.CentsToHertz(this[GeneratorParameterType.FrequencyModulationLfo]);
        public float DelayVibratoLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayVibratoLfo]);
        public float FrequencyVibratoLfo => SoundFontMath.CentsToHertz(this[GeneratorParameterType.FrequencyVibratoLfo]);
        public float DelayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayModulationEnvelope]);
        public float AttackModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.AttackModulationEnvelope]);
        public float HoldModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.HoldModulationEnvelope]);
        public float DecayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DecayModulationEnvelope]);
        public float SustainModulationEnvelope => 0.1F * this[GeneratorParameterType.SustainModulationEnvelope];
        public float ReleaseModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.ReleaseModulationEnvelope]);
        public int KeyNumberToModulationEnvelopeHold => this[GeneratorParameterType.KeyNumberToModulationEnvelopeHold];
        public int KeyNumberToModulationEnvelopeDecay => this[GeneratorParameterType.KeyNumberToModulationEnvelopeDecay];
        public float DelayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => 0.1F * this[GeneratorParameterType.SustainVolumeEnvelope];
        public float ReleaseVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.ReleaseVolumeEnvelope]);
        public int KeyNumberToVolumeEnvelopeHold => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeHold];
        public int KeyNumberToVolumeEnvelopeDecay => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeDecay];

        public int KeyRangeStart => this[GeneratorParameterType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (this[GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => this[GeneratorParameterType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (this[GeneratorParameterType.VelocityRange] >> 8) & 0xFF;

        public float InitialAttenuation => 0.1F * this[GeneratorParameterType.InitialAttenuation];

        public int CoarseTune => this[GeneratorParameterType.CoarseTune];
        public int FineTune => this[GeneratorParameterType.FineTune] + sample.PitchCorrection;
        public LoopMode SampleModes => this[GeneratorParameterType.SampleModes] != 2 ? (LoopMode)this[GeneratorParameterType.SampleModes] : LoopMode.NoLoop;

        public int ScaleTuning => this[GeneratorParameterType.ScaleTuning];
        public int ExclusiveClass => this[GeneratorParameterType.ExclusiveClass];
        public int RootKey => this[GeneratorParameterType.OverridingRootKey] != -1 ? this[GeneratorParameterType.OverridingRootKey] : sample.OriginalPitch;
    }
}
