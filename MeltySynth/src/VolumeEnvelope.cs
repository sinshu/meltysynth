using System;

namespace MeltySynth
{
    internal sealed class VolumeEnvelope
    {
        private readonly Synthesizer synthesizer;

        private double attackSlope;
        private double decaySlope;
        private double releaseSlope;

        private double attackStartTime;
        private double holdStartTime;
        private double decayStartTime;
        private double releaseStartTime;

        private float sustainLevel;
        private float releaseLevel;

        private int processedSampleCount;
        private Stage stage;
        private float value;

        private float priority;

        internal VolumeEnvelope(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        public void Start(float delay, float attack, float hold, float decay, float sustain, float release)
        {
            attackSlope = 1 / attack;
            decaySlope = -9.226 / decay;
            releaseSlope = -9.226 / release;

            attackStartTime = delay;
            holdStartTime = attackStartTime + attack;
            decayStartTime = holdStartTime + hold;
            releaseStartTime = 0;

            sustainLevel = Math.Clamp(sustain, 0F, 1F);
            releaseLevel = 0;

            processedSampleCount = 0;
            stage = Stage.Delay;
            value = 0;

            Process(0);
        }

        public void Release()
        {
            stage = Stage.Release;
            releaseStartTime = (double)processedSampleCount / synthesizer.SampleRate;
            releaseLevel = value;
        }

        public bool Process()
        {
            return Process(synthesizer.BlockSize);
        }

        private bool Process(int sampleCount)
        {
            processedSampleCount += sampleCount;

            var currentTime = (double)processedSampleCount / synthesizer.SampleRate;

            while (stage <= Stage.Hold)
            {
                double endTime;
                switch (stage)
                {
                    case Stage.Delay:
                        endTime = attackStartTime;
                        break;

                    case Stage.Attack:
                        endTime = holdStartTime;
                        break;

                    case Stage.Hold:
                        endTime = decayStartTime;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid envelope stage.");
                }

                if (currentTime < endTime)
                {
                    break;
                }
                else
                {
                    stage++;
                }
            }

            switch (stage)
            {
                case Stage.Delay:
                    value = 0;
                    priority = 4F + value;
                    return true;

                case Stage.Attack:
                    value = (float)(attackSlope * (currentTime - attackStartTime));
                    priority = 3F + value;
                    return true;

                case Stage.Hold:
                    value = 1;
                    priority = 2F + value;
                    return true;

                case Stage.Decay:
                    value = Math.Max((float)Math.Exp(decaySlope * (currentTime - decayStartTime)), sustainLevel);
                    priority = 1F + value;
                    return value > SoundFontMath.NonAudible;

                case Stage.Release:
                    value = (float)(releaseLevel * Math.Exp(releaseSlope * (currentTime - releaseStartTime)));
                    priority = value;
                    return value > SoundFontMath.NonAudible;

                default:
                    throw new InvalidOperationException("Invalid envelope stage.");
            }
        }

        public float Value => value;
        public float Priority => priority;



        private enum Stage
        {
            Delay,
            Attack,
            Hold,
            Decay,
            Release
        }
    }
}
