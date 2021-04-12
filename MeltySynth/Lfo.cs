using System;

namespace MeltySynth
{
    internal sealed class Lfo
    {
        private Synthesizer synthesizer;

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
            this.delay = delay;
            this.period = 1.0 / frequency;

            processedSampleCount = 0;
        }

        public void Process()
        {
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
