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

        // In the past, a mix gain never changed in a single block,
        // so instruments with a short release time could make click noise.
        // To avoid noise, now the mix gain can be a slope in a block
        // if the gain changes dramatically from the previous block.
        // The previous gains are saved for this.

        private float previousMixGainLeft;
        private float previousMixGainRight;
        private float currentMixGainLeft;
        private float currentMixGainRight;

        private float previousReverbSend;
        private float previousChorusSend;
        private float currentReverbSend;
        private float currentChorusSend;

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
        private float instrumentReverb;
        private float instrumentChorus;

        // Some instruments require fast cutoff change, which can make click noise.
        // This is used to smooth the cutoff frequency to reduce noise.
        private float smoothedCutoff;

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
                // According to the Polyphone's implementation, the initial attenuation should be reduced to 40%.
                // I'm not sure why, but this indeed improves the loudness variability.
                var sampleAttenuation = 0.4F * region.InitialAttenuation;
                var filterAttenuation = 0.5F * region.InitialFilterQ;
                var decibels = 2 * SoundFontMath.LinearToDecibels(velocity / 127F) - sampleAttenuation - filterAttenuation;
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
            instrumentReverb = 0.01F * region.ReverbEffectsSend;
            instrumentChorus = 0.01F * region.ChorusEffectsSend;

            volEnv.Start(region, key, velocity);
            modEnv.Start(region, key, velocity);
            vibLfo.StartVibrato(region, key, velocity);
            modLfo.StartModulation(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);
            filter.ClearBuffer();
            filter.SetLowPassFilter(cutoff, resonance);

            smoothedCutoff = cutoff;

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
                var newCutoff = factor * cutoff;
                if (newCutoff > smoothedCutoff)
                {
                    smoothedCutoff = 0.5F * (smoothedCutoff + newCutoff);
                }
                else
                {
                    smoothedCutoff = 0.8F * smoothedCutoff + 0.2F * newCutoff;
                }
                filter.SetLowPassFilter(smoothedCutoff, resonance);
            }
            filter.Process(block);

            previousMixGainLeft = currentMixGainLeft;
            previousMixGainRight = currentMixGainRight;
            previousReverbSend = currentReverbSend;
            previousChorusSend = currentChorusSend;

            // According to the GM spec, the following value should be squared.
            var ve = channelInfo.Volume * channelInfo.Expression;
            var channelGain = ve * ve;

            var mixGain = noteGain * channelGain * volEnv.Value;
            if (dynamicVolume)
            {
                var decibels = modLfoToVolume * modLfo.Value;
                mixGain *= SoundFontMath.DecibelsToLinear(decibels);
            }

            var angle = (MathF.PI / 200F) * (channelInfo.Pan + instrumentPan + 50F);
            if (angle <= 0F)
            {
                currentMixGainLeft = mixGain;
                currentMixGainRight = 0F;
            }
            else if (angle >= SoundFontMath.HalfPi)
            {
                currentMixGainLeft = 0F;
                currentMixGainRight = mixGain;
            }
            else
            {
                currentMixGainLeft = mixGain * MathF.Cos(angle);
                currentMixGainRight = mixGain * MathF.Sin(angle);
            }

            currentReverbSend = Math.Clamp(channelInfo.ReverbSend + instrumentReverb, 0F, 1F);
            currentChorusSend = Math.Clamp(channelInfo.ChorusSend + instrumentChorus, 0F, 1F);

            if (voiceLength == 0)
            {
                previousMixGainLeft = currentMixGainLeft;
                previousMixGainRight = currentMixGainRight;
                previousReverbSend = currentReverbSend;
                previousChorusSend = currentChorusSend;
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

        public float PreviousMixGainLeft => previousMixGainLeft;
        public float PreviousMixGainRight => previousMixGainRight;
        public float CurrentMixGainLeft => currentMixGainLeft;
        public float CurrentMixGainRight => currentMixGainRight;

        public float PreviousReverbSend => previousReverbSend;
        public float PreviousChorusSend => previousChorusSend;
        public float CurrentReverbSend => currentReverbSend;
        public float CurrentChorusSend => currentChorusSend;

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
