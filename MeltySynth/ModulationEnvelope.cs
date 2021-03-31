using System;

namespace MeltySynth
{
    internal sealed class ModulationEnvelope
    {
        private Synthesizer synthesizer;

        private double attackSlope;
        private double decaySlope;
        private double releaseSlope;

        private double attackStartTime;
        private double holdStartTime;
        private double decayStartTime;

        private double decayEndTime;
        private double releaseEndTime;

        private float sustainLevel;
        private float releaseLevel;

        private int processedSampleCount;
        private Stage stage;
        private float value;

        internal ModulationEnvelope(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        internal void Start(float delay, float attack, float hold, float decay, float sustain, float release)
        {
            attackSlope = 1 / attack;
            decaySlope = 1 / decay;
            releaseSlope = 1 / release;

            attackStartTime = delay;
            holdStartTime = attackStartTime + attack;
            decayStartTime = holdStartTime + hold;

            decayEndTime = decayStartTime + decay;
            releaseEndTime = release;

            sustainLevel = sustain;
            releaseLevel = 0;

            processedSampleCount = 0;
            stage = Stage.Delay;
            value = 0;
        }

        internal void Release()
        {
            stage = Stage.Release;
            releaseEndTime += (double)processedSampleCount / synthesizer.SampleRate;
            releaseLevel = value;
        }

        internal bool Process(int sampleCount)
        {
            processedSampleCount += sampleCount;

            var currentTime = (double)processedSampleCount / synthesizer.SampleRate;

            if (stage <= Stage.Hold)
            {
                while (true)
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

                        if (stage == Stage.Decay)
                        {
                            break;
                        }
                    }
                }
            }

            switch (stage)
            {
                case Stage.Delay:
                    value = 0;
                    break;
                case Stage.Attack:
                    value = (float)(attackSlope * (currentTime - attackStartTime));
                    break;
                case Stage.Hold:
                    value = 1;
                    break;
                case Stage.Decay:
                    value = Math.Max((float)(decaySlope * (decayEndTime - currentTime)), sustainLevel);
                    break;
                case Stage.Release:
                    value = Math.Max(releaseLevel * (float)(releaseSlope * (releaseEndTime - currentTime)), 0F);
                    break;
            }

            if (stage <= Stage.Hold)
            {
                return true;
            }
            else
            {
                return value > SoundFontMath.NonAudible;
            }
        }

        internal float Value => value;



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
