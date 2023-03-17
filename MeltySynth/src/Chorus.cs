using System;

namespace MeltySynth
{
    internal sealed class Chorus
    {
        private readonly float[] bufferL;
        private readonly float[] bufferR;

        private readonly float[] delayTable;

        private int bufferIndex;

        private int delayTableIndexL;
        private int delayTableIndexR;

        internal Chorus(int sampleRate, double delay, double depth, double frequency)
        {
            bufferL = new float[(int)(sampleRate * (delay + depth)) + 2];
            bufferR = new float[(int)(sampleRate * (delay + depth)) + 2];

            delayTable = new float[(int)Math.Round(sampleRate / frequency)];
            for (var t = 0; t < delayTable.Length; t++)
            {
                var phase = 2 * Math.PI * t / delayTable.Length;
                delayTable[t] = (float)(sampleRate * (delay + depth * Math.Sin(phase)));
            }

            bufferIndex = 0;

            delayTableIndexL = 0;
            delayTableIndexR = delayTable.Length / 4;
        }

        public void Process(float[] inputLeft, float[] inputRight, float[] outputLeft, float[] outputRight)
        {
            for (var t = 0; t < outputLeft.Length; t++)
            {
                {
                    var position = bufferIndex - (double)delayTable[delayTableIndexL];
                    if (position < 0.0)
                    {
                        position += bufferL.Length;
                    }

                    var index1 = (int)position;
                    var index2 = index1 + 1;

                    if (index2 == bufferL.Length)
                    {
                        index2 = 0;
                    }

                    var x1 = (double)bufferL[index1];
                    var x2 = (double)bufferL[index2];
                    var a = position - index1;
                    outputLeft[t] = (float)(x1 + a * (x2 - x1));

                    delayTableIndexL++;
                    if (delayTableIndexL == delayTable.Length)
                    {
                        delayTableIndexL = 0;
                    }
                }

                {
                    var position = bufferIndex - (double)delayTable[delayTableIndexR];
                    if (position < 0.0)
                    {
                        position += bufferR.Length;
                    }

                    var index1 = (int)position;
                    var index2 = index1 + 1;

                    if (index2 == bufferR.Length)
                    {
                        index2 = 0;
                    }

                    var x1 = (double)bufferR[index1];
                    var x2 = (double)bufferR[index2];
                    var a = position - index1;
                    outputRight[t] = (float)(x1 + a * (x2 - x1));

                    delayTableIndexR++;
                    if (delayTableIndexR == delayTable.Length)
                    {
                        delayTableIndexR = 0;
                    }
                }

                bufferL[bufferIndex] = inputLeft[t];
                bufferR[bufferIndex] = inputRight[t];
                bufferIndex++;
                if (bufferIndex == bufferL.Length)
                {
                    bufferIndex = 0;
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
