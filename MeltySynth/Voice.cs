using System;

namespace MeltySynth
{
    internal sealed class Voice
    {
        private Synthesizer synthesizer;

        private VolumeEnvelope volumeEnvelope;
        private ModulationEnvelope modulationEnvelope;

        private Lfo vibratoLfo;
        private Lfo modulationLfo;

        private Generator generator;
        private BiQuadFilter filter;

        private float[] block;
        private float mixGain;

        private InstrumentRegion instrumentRegion;
        private int channel;
        private int key;
        private int velocity;

        private float noteGain;
        private float cutoffFrequency;
        private float resonance;
        private float vibratoLfoToPitch;
        private int modulationEnvelopeToFilterCutoffFrequency;

        internal Voice(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;

            volumeEnvelope = new VolumeEnvelope(synthesizer);
            modulationEnvelope = new ModulationEnvelope(synthesizer);

            vibratoLfo = new Lfo(synthesizer);
            modulationLfo = new Lfo(synthesizer);

            generator = new Generator(synthesizer);
            filter = new BiQuadFilter(synthesizer);

            block = new float[synthesizer.BlockSize];
        }

        public void Start(RegionPair region, int channel, int key, int velocity)
        {
            this.instrumentRegion = region.Instrument;
            this.channel = channel;
            this.key = key;
            this.velocity = velocity;

            if (velocity > 0)
            {
                var decibels = 2 * SoundFontMath.LinearToDecibels(velocity / 127F) - region.InitialAttenuation - region.InitialFilterQ / 2;
                noteGain = SoundFontMath.DecibelsToLinear(decibels);
            }
            else
            {
                noteGain = 0;
            }
            cutoffFrequency = region.InitialFilterCutoffFrequency;
            resonance = SoundFontMath.DecibelsToLinear(region.InitialFilterQ);
            vibratoLfoToPitch = 0.01F * region.VibratoLfoToPitch;
            modulationEnvelopeToFilterCutoffFrequency = region.ModulationEnvelopeToFilterCutoffFrequency;

            volumeEnvelope.Start(region, key, velocity);
            modulationEnvelope.Start(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);
            vibratoLfo.StartVibrato(region, key, velocity);
            modulationLfo.StartModulation(region, key, velocity);
            filter.ClearBuffer();
            filter.SetLowPassFilter(cutoffFrequency, resonance);
        }

        public void End()
        {
            volumeEnvelope.Release();
            modulationEnvelope.Release();
            generator.Release();
        }

        public bool Process()
        {
            if (noteGain < SoundFontMath.NonAudible)
            {
                return false;
            }

            if (!volumeEnvelope.Process())
            {
                return false;
            }

            var pitch = key + vibratoLfoToPitch * vibratoLfo.Value;
            if (!generator.Process(block, pitch))
            {
                return false;
            }

            modulationEnvelope.Process();

            if (modulationEnvelopeToFilterCutoffFrequency != 0)
            {
                var factor = SoundFontMath.CentsToMultiplyingFactor(modulationEnvelopeToFilterCutoffFrequency * modulationEnvelope.Value);
                filter.SetLowPassFilter(factor * cutoffFrequency, resonance);
            }
            filter.Process(block);

            var channelInfo = synthesizer.Channels[channel];
            var channelGain = channelInfo.Volume * channelInfo.Expression;

            mixGain = noteGain * channelGain * volumeEnvelope.Value;

            vibratoLfo.Process();
            modulationLfo.Process();

            return true;
        }

        public float[] Block => block;
        public float MixGain => mixGain;

        public InstrumentRegion Region => instrumentRegion;
        public int Channel => channel;
        public int Key => key;
        public int Velocity => velocity;
    }
}
