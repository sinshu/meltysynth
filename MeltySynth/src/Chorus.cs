using System;

namespace MeltySynth
{
    public sealed class Chorus
    {
        private readonly float[] bufferL;
        private readonly float[] bufferR;

        private readonly float[] delayTableL;
        private readonly float[] delayTableR;

        private int bufferIndexL;
        private int bufferIndexR;

        private int delayTableIndexL;
        private int delayTableIndexR;

        public Chorus(int sampleRate, double delay, double depth, double frequency)
        {
            bufferL = new float[(int)(sampleRate * (delay + depth)) + 2];
            bufferR = new float[(int)(sampleRate * (delay + depth)) + 2];

            delayTableL = new float[(int)Math.Round(sampleRate / frequency)];
            delayTableR = new float[(int)Math.Round(sampleRate / frequency)];
            for (var t = 0; t < delayTableL.Length; t++)
            {
                var phase = 2 * Math.PI * t / delayTableL.Length;
                delayTableL[t] = (float)(sampleRate * (delay + depth * Math.Sin(phase)));
                delayTableR[t] = (float)(sampleRate * (delay + depth * Math.Cos(phase)));
            }

            bufferIndexL = 0;
            bufferIndexR = 0;

            delayTableIndexL = 0;
            delayTableIndexR = 0;
        }

        public void Process(float[] inputLeft, float[] inputRight, float[] outputLeft, float[] outputRight)
        {
            for (var t = 0; t < outputLeft.Length; t++)
            {
                var position = bufferIndexL - delayTableL[delayTableIndexL];
                if (position < 0F)
                {
                    position += bufferL.Length;
                }

                var index1 = (int)position;
                var index2 = index1 + 1;

                if (index2 == bufferL.Length)
                {
                    index2 = 0;
                }

                var x1 = bufferL[index1];
                var x2 = bufferL[index2];
                var a = position - index1;
                outputLeft[t] = x1 + a * (x2 - x1);

                bufferL[bufferIndexL] = inputLeft[t];
                bufferIndexL++;
                if (bufferIndexL == bufferL.Length)
                {
                    bufferIndexL = 0;
                }

                delayTableIndexL++;
                if (delayTableIndexL == delayTableL.Length)
                {
                    delayTableIndexL = 0;
                }
            }

            for (var t = 0; t < outputRight.Length; t++)
            {
                var position = bufferIndexR - delayTableR[delayTableIndexR];
                if (position < 0F)
                {
                    position += bufferR.Length;
                }

                var index1 = (int)position;
                var index2 = index1 + 1;

                if (index2 == bufferR.Length)
                {
                    index2 = 0;
                }

                var x1 = bufferR[index1];
                var x2 = bufferR[index2];
                var a = position - index1;
                outputRight[t] = x1 + a * (x2 - x1);

                bufferR[bufferIndexR] = inputRight[t];
                bufferIndexR++;
                if (bufferIndexR == bufferR.Length)
                {
                    bufferIndexR = 0;
                }

                delayTableIndexR++;
                if (delayTableIndexR == delayTableR.Length)
                {
                    delayTableIndexR = 0;
                }
            }
        }

        public void Mute()
        {
            Array.Clear(bufferL, 0, bufferL.Length);
            Array.Clear(bufferR, 0, bufferR.Length);
        }
    }
}
