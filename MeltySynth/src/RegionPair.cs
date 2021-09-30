using System;

namespace MeltySynth
{
    internal struct RegionPair
    {
        private readonly PresetRegion preset;
        private readonly InstrumentRegion instrument;

        internal RegionPair(PresetRegion preset, InstrumentRegion instrument)
        {
            this.preset = preset;
            this.instrument = instrument;
        }

        private int this[GeneratorType generatortType] => instrument[generatortType] + preset[generatortType];

        public PresetRegion Preset => preset;
        public InstrumentRegion Instrument => instrument;

        public int SampleStart => instrument.SampleStart;
        public int SampleEnd => instrument.SampleEnd;
        public int SampleStartLoop => instrument.SampleStartLoop;
        public int SampleEndLoop => instrument.SampleEndLoop;

        public int StartAddressOffset => instrument.StartAddressOffset;
        public int EndAddressOffset => instrument.EndAddressOffset;
        public int StartLoopAddressOffset => instrument.StartLoopAddressOffset;
        public int EndLoopAddressOffset => instrument.EndLoopAddressOffset;

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

        // public int KeyRangeStart => this[GeneratorParameterType.KeyRange] & 0xFF;
        // public int KeyRangeEnd => (this[GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        // public int VelocityRangeStart => this[GeneratorParameterType.VelocityRange] & 0xFF;
        // public int VelocityRangeEnd => (this[GeneratorParameterType.VelocityRange] >> 8) & 0xFF;

        public float InitialAttenuation => 0.1F * this[GeneratorType.InitialAttenuation];

        public int CoarseTune => this[GeneratorType.CoarseTune];
        public int FineTune => this[GeneratorType.FineTune] + instrument.Sample.PitchCorrection;
        public LoopMode SampleModes => instrument.SampleModes;

        public int ScaleTuning => this[GeneratorType.ScaleTuning];
        public int ExclusiveClass => instrument.ExclusiveClass;
        public int RootKey => instrument.RootKey;
    }
}
