using System;

namespace MeltySynth
{
    internal sealed class Voice
    {
        private readonly Synthesizer synthesizer;

        private readonly VolumeEnvelope volEnv;
        private readonly ModulationEnvelope modEnv;

        private readonly Lfo vibLfo;
        private readonly Lfo modLfo;

        private readonly Generator generator;
        private readonly BiQuadFilter filter;

        private readonly float[] block;

        private float mixGainLeft;
        private float mixGainRight;

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
        private bool dynamicCutoff;

        private float modLfoToVolume;
        private bool dynamicVolume;

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
            dynamicCutoff = modLfoToCutoff != 0 || modEnvToCutoff != 0;

            modLfoToVolume = region.ModulationLfoToVolume;
            dynamicVolume = modLfoToVolume > 0.05F;

            instrumentPan = Math.Clamp(region.Pan, -50F, 50F);

            volEnv.Start(region, key, velocity);
            modEnv.Start(region, key, velocity);
            vibLfo.StartVibrato(region, key, velocity);
            modLfo.StartModulation(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);
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

            if (dynamicCutoff)
            {
                var cents = modLfoToCutoff * modLfo.Value + modEnvToCutoff * modEnv.Value;
                var factor = SoundFontMath.CentsToMultiplyingFactor(cents);
                filter.SetLowPassFilter(factor * cutoff, resonance);
            }
            filter.Process(block);

            var channelGain = channelInfo.Volume * channelInfo.Expression;
            var mixGain = noteGain * channelGain * volEnv.Value;
            if (dynamicVolume)
            {
                var decibels = modLfoToVolume * modLfo.Value;
                mixGain *= SoundFontMath.DecibelsToLinear(decibels);
            }

            var angle = (MathF.PI / 200F) * (channelInfo.Pan + instrumentPan + 50F);
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
            if (voiceLength < synthesizer.MinimumVoiceDuration)
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
