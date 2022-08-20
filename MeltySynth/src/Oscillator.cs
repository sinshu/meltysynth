using System;

namespace MeltySynth
{
    internal sealed class Oscillator
    {
        // In this class, fixed-point numbers are used for speed-up.
        // A fixed-point number is expressed by Int64, whose lower 24 bits represent the fraction part,
        // and the rest represent the integer part.
        // For clarity, fixed-point number variables have a suffix "_fp".

        private const int fracBits = 24;
        private const long fracUnit = 1L << fracBits;
        private const float fpToSample = 1F / (32768 * fracUnit);

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

        private long position_fp;

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

            position_fp = (long)start << fracBits;
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
            var pitchRatio_fp = (long)(fracUnit * pitchRatio);

            if (looping)
            {
                return FillBlock_Continuous(block, pitchRatio_fp);
            }
            else
            {
                return FillBlock_NoLoop(block, pitchRatio_fp);
            }
        }

        private bool FillBlock_NoLoop(float[] block, long pitchRatio_fp)
        {
            for (var t = 0; t < block.Length; t++)
            {
                var index = position_fp >> fracBits;

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
                var a = position_fp & (fracUnit - 1);
                block[t] = fpToSample * (((long)x1 << fracBits) + a * (x2 - x1));

                position_fp += pitchRatio_fp;
            }

            return true;
        }

        private bool FillBlock_Continuous(float[] block, long pitchRatio_fp)
        {
            var endLoop_fp = (long)endLoop << fracBits;

            var loopLength = (long)(endLoop - startLoop);
            var loopLength_fp = loopLength << fracBits;

            for (var t = 0; t < block.Length; t++)
            {
                if (position_fp >= endLoop_fp)
                {
                    position_fp -= loopLength_fp;
                }

                var index1 = position_fp >> fracBits;
                var index2 = index1 + 1;

                if (index2 >= endLoop)
                {
                    index2 -= loopLength;
                }

                var x1 = data[index1];
                var x2 = data[index2];
                var a = position_fp & (fracUnit - 1);
                block[t] = fpToSample * (((long)x1 << fracBits) + a * (x2 - x1));

                position_fp += pitchRatio_fp;
            }

            return true;
        }
    }
}
