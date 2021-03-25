using System;

namespace MeltySynth
{
    internal struct RegionPair
    {
        private PresetRegion preset;
        private InstrumentRegion instrument;

        internal RegionPair(PresetRegion preset, InstrumentRegion instrument)
        {
            this.preset = preset;
            this.instrument = instrument;
        }

        private int this[GeneratorParameterType generatortType] => instrument[generatortType] + preset[generatortType];

        internal PresetRegion Preset => preset;
        internal InstrumentRegion Instrument => instrument;

        internal int StartAddressOffset => 32768 * instrument[GeneratorParameterType.StartAddressCoarseOffset] + instrument[GeneratorParameterType.StartAddressOffset];
        internal int EndAddressOffset => 32768 * instrument[GeneratorParameterType.EndAddressCoarseOffset] + instrument[GeneratorParameterType.EndAddressOffset];
        internal int StartLoopAddressOffset => 32768 * instrument[GeneratorParameterType.StartLoopAddressCoarseOffset] + instrument[GeneratorParameterType.StartLoopAddressOffset];
        internal int EndLoopAddressOffset => 32768 * instrument[GeneratorParameterType.EndLoopAddressCoarseOffset] + instrument[GeneratorParameterType.EndLoopAddressOffset];

        internal int ModulationLfoToPitch => this[GeneratorParameterType.ModulationLfoToPitch];
        internal int VibratoLfoToPitch => this[GeneratorParameterType.VibratoLfoToPitch];
        internal int ModulationEnvelopeToPitch => this[GeneratorParameterType.ModulationEnvelopeToPitch];
        internal float InitialFilterCutoffFrequency => SoundFontMath.CentsToHertz(this[GeneratorParameterType.InitialFilterCutoffFrequency]);
        internal float InitialFilterQ => this[GeneratorParameterType.InitialFilterQ] / 10F;
        internal int ModulationLfoToFilterCutoffFrequency => this[GeneratorParameterType.ModulationLfoToFilterCutoffFrequency];
        internal int ModulationEnvelopeToFilterCutoffFrequency => this[GeneratorParameterType.ModulationEnvelopeToFilterCutoffFrequency];

        internal float ModulationLfoToVolume => this[GeneratorParameterType.ModulationLfoToVolume] / 10F;

        internal float ChorusEffectsSend => this[GeneratorParameterType.ChorusEffectsSend] / 10F;
        internal float ReverbEffectsSend => this[GeneratorParameterType.ReverbEffectsSend] / 10F;
        internal float Pan => this[GeneratorParameterType.Pan] / 10F;

        internal float DelayModulationLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayModulationLfo]);
        internal float FrequencyModulationLfo => SoundFontMath.CentsToHertz(this[GeneratorParameterType.FrequencyModulationLfo]);
        internal float DelayVibratoLfo => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayVibratoLfo]);
        internal float FrequencyVibratoLfo => SoundFontMath.CentsToHertz(this[GeneratorParameterType.FrequencyVibratoLfo]);
        internal float DelayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayModulationEnvelope]);
        internal float AttackModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.AttackModulationEnvelope]);
        internal float HoldModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.HoldModulationEnvelope]);
        internal float DecayModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DecayModulationEnvelope]);
        internal float SustainModulationEnvelope => this[GeneratorParameterType.SustainModulationEnvelope] / 10F;
        internal float ReleaseModulationEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.ReleaseModulationEnvelope]);
        internal int KeyNumberToModulationEnvelopeHold => this[GeneratorParameterType.KeyNumberToModulationEnvelopeHold];
        internal int KeyNumberToModulationEnvelopeDecay => this[GeneratorParameterType.KeyNumberToModulationEnvelopeDecay];
        internal float DelayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DelayVolumeEnvelope]);
        internal float AttackVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.AttackVolumeEnvelope]);
        internal float HoldVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.HoldVolumeEnvelope]);
        internal float DecayVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.DecayVolumeEnvelope]);
        internal float SustainVolumeEnvelope => this[GeneratorParameterType.SustainVolumeEnvelope] / 10F;
        internal float ReleaseVolumeEnvelope => SoundFontMath.TimecentsToSeconds(this[GeneratorParameterType.ReleaseVolumeEnvelope]);
        internal int KeyNumberToVolumeEnvelopeHold => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeHold];
        internal int KeyNumberToVolumeEnvelopeDecay => this[GeneratorParameterType.KeyNumberToVolumeEnvelopeDecay];

        // internal int KeyRangeStart => this[GeneratorParameterType.KeyRange] & 0xFF;
        // internal int KeyRangeEnd => (this[GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        // internal int VelocityRangeStart => this[GeneratorParameterType.VelocityRange] & 0xFF;
        // internal int VelocityRangeEnd => (this[GeneratorParameterType.VelocityRange] >> 8) & 0xFF;

        internal float InitialAttenuation => this[GeneratorParameterType.InitialAttenuation] / 10F;

        internal int CoarseTune => this[GeneratorParameterType.CoarseTune];
        internal int FineTune => this[GeneratorParameterType.FineTune];
        internal LoopMode SampleModes => instrument[GeneratorParameterType.SampleModes] != 2 ? (LoopMode)this[GeneratorParameterType.SampleModes] : LoopMode.NoLoop;

        internal int ScaleTuning => this[GeneratorParameterType.ScaleTuning];
        internal int ExclusiveClass => instrument[GeneratorParameterType.ExclusiveClass];
        internal int OverridingRootKey => instrument[GeneratorParameterType.OverridingRootKey];
    }
}
