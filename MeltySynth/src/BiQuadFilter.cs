using System;

namespace MeltySynth
{
    internal sealed class BiQuadFilter
    {
        private static readonly float resonancePeakOffset = 1 - 1 / MathF.Sqrt(2);

        private Synthesizer synthesizer;

        private bool active;

        private float a0;
        private float a1;
        private float a2;
        private float a3;
        private float a4;

        private float x1;
        private float x2;
        private float y1;
        private float y2;

        internal BiQuadFilter(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        public void ClearBuffer()
        {
            x1 = 0;
            x2 = 0;
            y1 = 0;
            y2 = 0;
        }

        public void SetLowPassFilter(float cutoffFrequency, float resonance)
        {
            if (cutoffFrequency < 0.499F * synthesizer.SampleRate)
            {
                active = true;

                // This equation gives the Q value which makes the desired resonance peak.
                // The error of the resultant peak height is less than 3%.
                var q = resonance - resonancePeakOffset / (1 + 6 * (resonance - 1));

                var w = 2 * MathF.PI * cutoffFrequency / synthesizer.SampleRate;
                var cos = MathF.Cos(w);
                var alpha = MathF.Sin(w) / (2 * q);

                var b0 = (1 - cos) / 2;
                var b1 = 1 - cos;
                var b2 = (1 - cos) / 2;
                var a0 = 1 + alpha;
                var a1 = -2 * cos;
                var a2 = 1 - alpha;

                SetCoefficients(a0, a1, a2, b0, b1, b2);
            }
            else
            {
                active = false;
            }
        }

        public void Process(float[] block)
        {
            if (active)
            {
                for (var t = 0; t < block.Length; t++)
                {
                    var input = block[t];
                    var output = a0 * input + a1 * x1 + a2 * x2 - a3 * y1 - a4 * y2;

                    x2 = x1;
                    x1 = input;
                    y2 = y1;
                    y1 = output;

                    block[t] = output;
                }
            }
            else
            {
                x2 = block[block.Length - 2];
                x1 = block[block.Length - 1];
                y2 = x2;
                y1 = x1;
            }
        }

        private void SetCoefficients(float a0, float a1, float a2, float b0, float b1, float b2)
        {
            this.a0 = b0 / a0;
            this.a1 = b1 / a0;
            this.a2 = b2 / a0;
            this.a3 = a1 / a0;
            this.a4 = a2 / a0;
        }
    }
}
