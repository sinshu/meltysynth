using System;
using MeltySynth.SoundFont;

namespace MeltySynth.Synthesis
{
    public sealed class Region
    {
        private short[] generatorSetting;

        internal Region()
        {
            generatorSetting = new short[61];
            generatorSetting[(int)GeneratorParameterType.InitialFilterCutoffFrequency] = 13500;
            generatorSetting[(int)GeneratorParameterType.DelayModulationLfo] = -12000;
            generatorSetting[(int)GeneratorParameterType.DelayVibratoLfo] = -12000;
            generatorSetting[(int)GeneratorParameterType.DelayModulationEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.AttackModulationEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.HoldModulationEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.DecayModulationEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.ReleaseModulationEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.DelayVolumeEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.AttackVolumeEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.HoldVolumeEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.DecayVolumeEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.ReleaseVolumeEnvelope] = -12000;
            generatorSetting[(int)GeneratorParameterType.KeyRange] = 0x7F00;
            generatorSetting[(int)GeneratorParameterType.VelocityRange] = 0x7F00;
            generatorSetting[(int)GeneratorParameterType.KeyNumber] = -1;
            generatorSetting[(int)GeneratorParameterType.Velocity] = -1;
            generatorSetting[(int)GeneratorParameterType.ScaleTuning] = 100;
            generatorSetting[(int)GeneratorParameterType.OverridingRootKey] = -1;
        }

        public void SetParameter(GeneratorParameter parameter)
        {
            generatorSetting[(int)parameter.Type] = (short)parameter.Value;
        }

        public override string ToString()
        {
            return $"Key: ({KeyRangeStart}, {KeyRangeEnd}), Velocity: ({VelocityRangeStart}, {VelocityRangeEnd})";
        }

        public short this[int generatortType] => generatorSetting[generatortType];
        public short this[GeneratorParameterType generatortType] => generatorSetting[(int)generatortType];

        public float AttackVolumeEnvelope => SoundFontMath.TimecentToSecond(generatorSetting[(int)GeneratorParameterType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.TimecentToSecond(generatorSetting[(int)GeneratorParameterType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.TimecentToSecond(generatorSetting[(int)GeneratorParameterType.DecayVolumeEnvelope]);
        public float ReleaseVolumeEnvelope => SoundFontMath.TimecentToSecond(generatorSetting[(int)GeneratorParameterType.ReleaseVolumeEnvelope]);

        public int KeyRangeStart => generatorSetting[(int)GeneratorParameterType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (generatorSetting[(int)GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => generatorSetting[(int)GeneratorParameterType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (generatorSetting[(int)GeneratorParameterType.VelocityRange] >> 8) & 0xFF;
    }
}
