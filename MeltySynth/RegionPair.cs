using System;

namespace MeltySynth
{
    public struct RegionPair
    {
        private PresetRegion preset;
        private InstrumentRegion instrument;

        public RegionPair(PresetRegion preset, InstrumentRegion instrument)
        {
            this.preset = preset;
            this.instrument = instrument;
        }

        private int this[GeneratorParameterType generatortType] => instrument[generatortType] + preset[generatortType];

        public PresetRegion Preset => preset;
        public InstrumentRegion Instrument => instrument;

        public int StartAddressOffset => 32768 * this[GeneratorParameterType.StartAddressCoarseOffset] + this[GeneratorParameterType.StartAddressOffset];
        public int EndAddressOffset => 32768 * this[GeneratorParameterType.EndAddressCoarseOffset] + this[GeneratorParameterType.EndAddressOffset];
        public int StartLoopAddressOffset => 32768 * this[GeneratorParameterType.StartLoopAddressCoarseOffset] + this[GeneratorParameterType.StartLoopAddressOffset];
        public int EndLoopAddressOffset => 32768 * this[GeneratorParameterType.EndLoopAddressCoarseOffset] + this[GeneratorParameterType.EndLoopAddressOffset];

        public int ModulationLfoToPitch => this[GeneratorParameterType.ModulationLfoToPitch];
        public int VibratoLfoToPitch => this[GeneratorParameterType.VibratoLfoToPitch];
        public int ModulationEnvelopeToPitch => this[GeneratorParameterType.ModulationEnvelopeToPitch];
        public float InitialFilterCutoffFrequency => SoundFontMath.CentsToHertz(this[GeneratorParameterType.InitialFilterCutoffFrequency]);
        public float InitialFilterQ => this[GeneratorParameterType.InitialFilterQ] / 10F;
        public int ModulationLfoToFilterCutoffFrequency => this[GeneratorParameterType.ModulationLfoToFilterCutoffFrequency];
        public int ModulationEnvelopeToFilterCutoffFrequency => this[GeneratorParameterType.ModulationEnvelopeToFilterCutoffFrequency];

        public float ModulationLfoToVolume => this[GeneratorParameterType.ModulationLfoToVolume] / 10F;

        public float ChorusEffectsSend => this[GeneratorParameterType.ChorusEffectsSend] / 10F;
        public float ReverbEffectsSend => this[GeneratorParameterType.ReverbEffectsSend] / 10F;
        public float Pan => this[GeneratorParameterType.Pan] / 10F;

        public float DelayModulationLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayModulationLfo]);
        public float FrequencyModulationLfo => SoundFontMath.CentsToHertz(this[GeneratorParameterType.FrequencyModulationLfo]);
        public float DelayVibratoLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayVibratoLfo]);
        public float FrequencyVibratoLfo => SoundFontMath.CentsToHertz(this[GeneratorParameterType.FrequencyVibratoLfo]);
        public float DelayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayModulationEnvelope]);
        public float AttackModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.AttackModulationEnvelope]);
        public float HoldModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.HoldModulationEnvelope]);
        public float DecayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DecayModulationEnvelope]);
        public float SustainModulationEnvelope => this[GeneratorParameterType.SustainModulationEnvelope] / 10F;
        public float ReleaseModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.ReleaseModulationEnvelope]);
        public int KeyNumberToModulationEnvelopeHold => this[GeneratorParameterType.KeyNumberToModulationEnvelopeHold];
        public int KeyNumberToModulationEnvelopeDecay => this[GeneratorParameterType.KeyNumberToModulationEnvelopeDecay];
        public float DelayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayVolumeEnvelope]);
        public float AttackVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.AttackVolumeEnvelope]);
        public float HoldVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.HoldVolumeEnvelope]);
        public float DecayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DecayVolumeEnvelope]);
        public float SustainVolumeEnvelope => this[GeneratorParameterType.SustainVolumeEnvelope] / 10F;
        public float ReleaseVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.ReleaseVolumeEnvelope]);
        public int KeyNumberToVolumeEnvelopeHold => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeHold];
        public int KeyNumberToVolumeEnvelopeDecay => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeDecay];

        public float InitialAttenuation => this[GeneratorParameterType.InitialAttenuation] / 10F;

        public int CoarseTune => this[GeneratorParameterType.CoarseTune];
        public int FineTune => this[GeneratorParameterType.FineTune];

        public int ScaleTuning => this[GeneratorParameterType.ScaleTuning];
        public int ExclusiveClass => this[GeneratorParameterType.ExclusiveClass];
        public int OverridingRootKey => this[GeneratorParameterType.OverridingRootKey];
    }
}
