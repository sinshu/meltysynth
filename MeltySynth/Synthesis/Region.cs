using System;
using MeltySynth.SoundFont;

namespace MeltySynth.Synthesis
{
    public sealed class Region
    {
        private short[] gs;

        internal Region()
        {
            gs = new short[61];
            gs[(int)GeneratorParameterType.InitialFilterCutoffFrequency] = 13500;
            gs[(int)GeneratorParameterType.DelayModulationLfo] = -12000;
            gs[(int)GeneratorParameterType.DelayVibratoLfo] = -12000;
            gs[(int)GeneratorParameterType.DelayModulationEnvelope] = -12000;
            gs[(int)GeneratorParameterType.AttackModulationEnvelope] = -12000;
            gs[(int)GeneratorParameterType.HoldModulationEnvelope] = -12000;
            gs[(int)GeneratorParameterType.DecayModulationEnvelope] = -12000;
            gs[(int)GeneratorParameterType.ReleaseModulationEnvelope] = -12000;
            gs[(int)GeneratorParameterType.DelayVolumeEnvelope] = -12000;
            gs[(int)GeneratorParameterType.AttackVolumeEnvelope] = -12000;
            gs[(int)GeneratorParameterType.HoldVolumeEnvelope] = -12000;
            gs[(int)GeneratorParameterType.DecayVolumeEnvelope] = -12000;
            gs[(int)GeneratorParameterType.ReleaseVolumeEnvelope] = -12000;
            gs[(int)GeneratorParameterType.KeyRange] = 0x7F00;
            gs[(int)GeneratorParameterType.VelocityRange] = 0x7F00;
            gs[(int)GeneratorParameterType.KeyNumber] = -1;
            gs[(int)GeneratorParameterType.Velocity] = -1;
            gs[(int)GeneratorParameterType.ScaleTuning] = 100;
            gs[(int)GeneratorParameterType.OverridingRootKey] = -1;
        }

        public void SetParameter(GeneratorParameter parameter)
        {
            gs[(int)parameter.Type] = (short)parameter.Value;
        }

        public override string ToString()
        {
            return $"Key: ({KeyRangeStart}, {KeyRangeEnd}), Velocity: ({VelocityRangeStart}, {VelocityRangeEnd})";
        }

        public short this[int generatortType] => gs[generatortType];
        public short this[GeneratorParameterType generatortType] => gs[(int)generatortType];

        public float InitialFilterCutoffFrequency => SoundFontMath.CentsToHertz(gs[(int)GeneratorParameterType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => gs[(int)GeneratorParameterType.InitialFilterQ] / 10F;

        public float Pan => gs[(int)GeneratorParameterType.Pan] / 10F;

        public float DelayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gs[(int)GeneratorParameterType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gs[(int)GeneratorParameterType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gs[(int)GeneratorParameterType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gs[(int)GeneratorParameterType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => gs[(int)GeneratorParameterType.SustainVolumeEnvelope] / 10F;
        public float ReleaseVolumeEnvelope => SoundFontMath.TimecentsToSeconds(gs[(int)GeneratorParameterType.ReleaseVolumeEnvelope]);
        public float KeyNumberToVolumeEnvelopeHold => gs[(int)GeneratorParameterType.KeyNumberToVolumeEnvelopeHold];
        public float KeyNumberToVolumeEnvelopeDecay => gs[(int)GeneratorParameterType.KeyNumberToVolumeEnvelopeDecay];

        public int KeyRangeStart => gs[(int)GeneratorParameterType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (gs[(int)GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => gs[(int)GeneratorParameterType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (gs[(int)GeneratorParameterType.VelocityRange] >> 8) & 0xFF;

        public float InitialAttenuation => gs[(int)GeneratorParameterType.InitialAttenuation] / 10F;

        public int CoarseTune => gs[(int)GeneratorParameterType.CoarseTune];
        public int FineTune => gs[(int)GeneratorParameterType.FineTune];
        public int SampleID => gs[(int)GeneratorParameterType.SampleID];
        public LoopMode SampleMode => gs[(int)GeneratorParameterType.SampleModes] != 2 ? (LoopMode)gs[(int)GeneratorParameterType.SampleModes] : LoopMode.NoLoop;

        public int OverridingRootKey => gs[(int)GeneratorParameterType.OverridingRootKey];
    }
}
