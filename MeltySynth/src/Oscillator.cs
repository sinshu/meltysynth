using System;

namespace MeltySynth
{
    internal sealed class Oscillator
    {
        private readonly Synthesizer synthesizer;

        private short[] data;
        private LoopMode loopMode;
        private int sampleRate;
        private int start;
        private int end;
        private int startLoop;
        private int endLoop;
        private int rootKey;

        private float tune;
        private float pitchChangeScale;
        private float sampleRateRatio;

        private bool looping;

        private double position;

        internal Oscillator(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        public void Start(short[] data, LoopMode loopMode, int sampleRate, int start, int end, int startLoop, int endLoop, int rootKey, int coarseTune, int fineTune, int scaleTuning)
        {
            this.data = data;
            this.loopMode = loopMode;
            this.sampleRate = sampleRate;
            this.start = start;
            this.end = end;
            this.startLoop = startLoop;
            this.endLoop = endLoop;
            this.rootKey = rootKey;

            tune = coarseTune + 0.01F * fineTune;
            pitchChangeScale = 0.01F * scaleTuning;
            sampleRateRatio = (float)sampleRate / synthesizer.SampleRate;

            if (loopMode == LoopMode.NoLoop)
            {
                looping = false;
            }
            else
            {
                looping = true;
            }

            position = start;
        }

        public void Release()
        {
            if (loopMode == LoopMode.LoopUntilNoteOff)
            {
                looping = false;
            }
        }

        public bool Process(float[] block, float pitch)
        {
            var pitchChange = pitchChangeScale * (pitch - rootKey) + tune;
            var pitchRatio = sampleRateRatio * MathF.Pow(2, pitchChange / 12);
            return FillBlock(block, pitchRatio);
        }

        internal bool FillBlock(float[] block, double pitchRatio)
        {
            if (looping)
            {
                return FillBlock_Continuous(block, pitchRatio);
            }
            else
            {
                return FillBlock_NoLoop(block, pitchRatio);
            }
        }

        private bool FillBlock_NoLoop(float[] block, double pitchRatio)
        {
            for (var t = 0; t < block.Length; t++)
            {
                var index = (int)position;

                if (index >= end)
                {
                    if (t > 0)
                    {
                        Array.Clear(block, t, block.Length - t);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                var x1 = data[index];
                var x2 = data[index + 1];
                var a = (float)(position - index);
                block[t] = (x1 + a * (x2 - x1)) / 32768;

                position += pitchRatio;
            }

            return true;
        }

        private bool FillBlock_Continuous(float[] block, double pitchRatio)
        {
            var endLoopPosition = (double)endLoop;

            var loopLength = endLoop - startLoop;

            for (var t = 0; t < block.Length; t++)
            {
                if (position >= endLoopPosition)
                {
                    position -= loopLength;
                }

                var index1 = (int)position;
                var index2 = index1 + 1;

                if (index2 >= endLoop)
                {
                    index2 -= loopLength;
                }

                var x1 = data[index1];
                var x2 = data[index2];
                var a = position - index1;
                block[t] = (float)((x1 + a * (x2 - x1)) / 32768);

                position += pitchRatio;
            }

            return true;
        }
    }
}
