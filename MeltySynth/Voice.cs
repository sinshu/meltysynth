using System;

namespace MeltySynth
{
    internal sealed class Voice
    {
        private Synthesizer synthesizer;

        private VolumeEnvelope volEnv;
        private ModulationEnvelope modEnv;

        private Lfo vibLfo;
        private Lfo modLfo;

        private Generator generator;
        private BiQuadFilter filter;

        private float[] block;
        private float mixGain;

        private InstrumentRegion instrumentRegion;
        private int exclusiveClass;
        private int channel;
        private int key;
        private int velocity;

        private float noteGain;

        private float cutoff;
        private float resonance;

        private float vibLfoToPitch;
        private float modLfoToPitch;
        private float modEnvToPitch;

        private int modLfoToCutoff;
        private int modEnvToCutoff;

        internal Voice(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;

            volEnv = new VolumeEnvelope(synthesizer);
            modEnv = new ModulationEnvelope(synthesizer);

            vibLfo = new Lfo(synthesizer);
            modLfo = new Lfo(synthesizer);

            generator = new Generator(synthesizer);
            filter = new BiQuadFilter(synthesizer);

            block = new float[synthesizer.BlockSize];
        }

        public void Start(RegionPair region, int channel, int key, int velocity)
        {
            this.instrumentRegion = region.Instrument;
            this.exclusiveClass = region.ExclusiveClass;
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

            cutoff = region.InitialFilterCutoffFrequency;
            resonance = SoundFontMath.DecibelsToLinear(region.InitialFilterQ);

            vibLfoToPitch = 0.01F * region.VibratoLfoToPitch;
            modLfoToPitch = 0.01F * region.ModulationLfoToPitch;
            modEnvToPitch = 0.01F * region.ModulationEnvelopeToPitch;

            modLfoToCutoff = region.ModulationLfoToFilterCutoffFrequency;
            modEnvToCutoff = region.ModulationEnvelopeToFilterCutoffFrequency;

            volEnv.Start(region, key, velocity);
            modEnv.Start(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);
            vibLfo.StartVibrato(region, key, velocity);
            modLfo.StartModulation(region, key, velocity);
            filter.ClearBuffer();
            filter.SetLowPassFilter(cutoff, resonance);
        }

        public void End()
        {
            volEnv.Release();
            modEnv.Release();
            generator.Release();
        }

        public void Kill()
        {
            noteGain = 0;
        }

        public bool Process()
        {
            if (noteGain < SoundFontMath.NonAudible)
            {
                return false;
            }

            if (!volEnv.Process())
            {
                return false;
            }

            var pitch = key + vibLfoToPitch * vibLfo.Value + modLfoToPitch * modLfo.Value + modEnvToPitch * modEnv.Value;
            if (!generator.Process(block, pitch))
            {
                return false;
            }

            modEnv.Process();

            if (modLfoToCutoff != 0 || modEnvToCutoff != 0)
            {
                var cents = modLfoToCutoff * modLfo.Value + modEnvToCutoff * modEnv.Value;
                var factor = SoundFontMath.CentsToMultiplyingFactor(cents);
                filter.SetLowPassFilter(factor * cutoff, resonance);
            }
            filter.Process(block);

            var channelInfo = synthesizer.Channels[channel];
            var channelGain = channelInfo.Volume * channelInfo.Expression;

            mixGain = noteGain * channelGain * volEnv.Value;

            vibLfo.Process();
            modLfo.Process();

            return true;
        }

        public float Priority
        {
            get
            {
                if (noteGain < SoundFontMath.NonAudible)
                {
                    return 0F;
                }
                else
                {
                    return volEnv.Priority;
                }
            }
        }

        public float[] Block => block;
        public float MixGain => mixGain;

        public InstrumentRegion Region => instrumentRegion;
        public int ExclusiveClass => exclusiveClass;
        public int Channel => channel;
        public int Key => key;
        public int Velocity => velocity;
    }
}
