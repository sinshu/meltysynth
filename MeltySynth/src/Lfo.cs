using System;

namespace MeltySynth
{
    internal sealed class Lfo
    {
        private readonly Synthesizer synthesizer;

        private bool active;

        private double delay;
        private double period;

        private int processedSampleCount;
        private float value;

        internal Lfo(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        public void Start(float delay, float frequency)
        {
            if (frequency > 1.0E-3)
            {
                active = true;

                this.delay = delay;
                this.period = 1.0 / frequency;

                processedSampleCount = 0;
                value = 0;
            }
            else
            {
                active = false;
                value = 0;
            }
        }

        public void Process()
        {
            if (!active)
            {
                return;
            }

            processedSampleCount += synthesizer.BlockSize;

            var currentTime = (double)processedSampleCount / synthesizer.SampleRate;

            if (currentTime < delay)
            {
                value = 0;
            }
            else
            {
                var phase = ((currentTime - delay) % period) / period;
                if (phase < 0.25)
                {
                    value = (float)(4 * phase);
                }
                else if (phase < 0.75)
                {
                    value = (float)(4 * (0.5 - phase));
                }
                else
                {
                    value = (float)(4 * (phase - 1.0));
                }
            }
        }

        public float Value => value;
    }
}
