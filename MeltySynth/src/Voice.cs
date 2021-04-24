using System;

namespace MeltySynth
{
    internal sealed class Voice
    {
        private const float panToAngle = MathF.PI / 200F;

        private Synthesizer synthesizer;

        private VolumeEnvelope volEnv;
        private ModulationEnvelope modEnv;

        private Lfo vibLfo;
        private Lfo modLfo;

        private Generator generator;
        private BiQuadFilter filter;

        private float[] block;
        private float mixGainLeft;
        private float mixGainRight;

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

        private float instrumentPan;

        private VoiceState voiceState;
        private int voiceLength;

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
                noteGain = 0F;
            }

            cutoff = region.InitialFilterCutoffFrequency;
            resonance = SoundFontMath.DecibelsToLinear(region.InitialFilterQ);

            vibLfoToPitch = 0.01F * region.VibratoLfoToPitch;
            modLfoToPitch = 0.01F * region.ModulationLfoToPitch;
            modEnvToPitch = 0.01F * region.ModulationEnvelopeToPitch;

            modLfoToCutoff = region.ModulationLfoToFilterCutoffFrequency;
            modEnvToCutoff = region.ModulationEnvelopeToFilterCutoffFrequency;

            instrumentPan = Math.Clamp(region.Pan, -50F, 50F);

            volEnv.Start(region, key, velocity);
            modEnv.Start(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);
            vibLfo.StartVibrato(region, key, velocity);
            modLfo.StartModulation(region, key, velocity);
            filter.ClearBuffer();
            filter.SetLowPassFilter(cutoff, resonance);

            voiceState = VoiceState.Playing;
            voiceLength = 0;
        }

        public void End()
        {
            if (voiceState == VoiceState.Playing)
            {
                voiceState = VoiceState.ReleaseRequested;
            }
        }

        public void Kill()
        {
            noteGain = 0F;
        }

        public bool Process()
        {
            if (noteGain < SoundFontMath.NonAudible)
            {
                return false;
            }

            var channelInfo = synthesizer.Channels[channel];

            ReleaseIfNecessary(channelInfo);

            if (!volEnv.Process())
            {
                return false;
            }

            modEnv.Process();
            vibLfo.Process();
            modLfo.Process();

            var vibPitchChange = (0.01F * channelInfo.Modulation + vibLfoToPitch) * vibLfo.Value;
            var modPitchChange = modLfoToPitch * modLfo.Value + modEnvToPitch * modEnv.Value;
            var channelPitchChange = channelInfo.Tune + channelInfo.PitchBend;
            var pitch = key + vibPitchChange + modPitchChange + channelPitchChange;
            if (!generator.Process(block, pitch))
            {
                return false;
            }

            if (modLfoToCutoff != 0 || modEnvToCutoff != 0)
            {
                var cents = modLfoToCutoff * modLfo.Value + modEnvToCutoff * modEnv.Value;
                var factor = SoundFontMath.CentsToMultiplyingFactor(cents);
                filter.SetLowPassFilter(factor * cutoff, resonance);
            }
            filter.Process(block);

            var channelGain = channelInfo.Volume * channelInfo.Expression;
            var mixGain = noteGain * channelGain * volEnv.Value;

            var angle = panToAngle * (channelInfo.Pan + instrumentPan + 50F);
            if (angle <= 0F)
            {
                mixGainLeft = mixGain;
                mixGainRight = 0F;
            }
            else if (angle >= SoundFontMath.HalfPi)
            {
                mixGainLeft = 0F;
                mixGainRight = mixGain;
            }
            else
            {
                mixGainLeft = mixGain * MathF.Cos(angle);
                mixGainRight = mixGain * MathF.Sin(angle);
            }

            voiceLength += synthesizer.BlockSize;

            return true;
        }

        private void ReleaseIfNecessary(Channel channelInfo)
        {
            if (voiceLength < synthesizer.MinimumVoiceLength)
            {
                return;
            }

            if (voiceState == VoiceState.ReleaseRequested && !channelInfo.HoldPedal)
            {
                volEnv.Release();
                modEnv.Release();
                generator.Release();

                voiceState = VoiceState.Released;
            }
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
        public float MixGainLeft => mixGainLeft;
        public float MixGainRight => mixGainRight;

        public InstrumentRegion Region => instrumentRegion;
        public int ExclusiveClass => exclusiveClass;
        public int Channel => channel;
        public int Key => key;
        public int Velocity => velocity;



        private enum VoiceState
        {
            Playing,
            ReleaseRequested,
            Released
        }
    }
}
