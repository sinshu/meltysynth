using System;

namespace MeltySynth
{
    internal sealed class Generator
    {
        private Synthesizer synthesizer;

        private short[] data;
        private LoopMode loopMode;
        private int start;
        private int end;
        private int startLoop;
        private int endLoop;

        private double position;

        internal Generator(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;
        }

        internal void Start(short[] data, LoopMode loopMode, int start, int end, int startLoop, int endLoop)
        {
            this.data = data;
            this.loopMode = loopMode;
            this.start = start;
            this.end = end;
            this.startLoop = startLoop;
            this.endLoop = endLoop;

            position = start;
        }

        internal void Release()
        {
            loopMode = LoopMode.NoLoop;
        }

        internal bool Process(float[] block, double pitchRatio)
        {
            switch (loopMode)
            {
                case LoopMode.NoLoop:
                    return Process_NoLoop(block, pitchRatio);
                case LoopMode.Continuous:
                    return Process_Continuous(block, pitchRatio);
                default:
                    throw new InvalidOperationException("Invalid loop mode.");
            }
        }

        private bool Process_NoLoop(float[] block, double pitchRatio)
        {
            var data = synthesizer.SoundFont.WaveData;

            for (var t = 0; t < block.Length; t++)
            {
                var index = (int)position;

                if (index >= end)
                {
                    Array.Clear(block, t, block.Length - t);
                    return false;
                }

                var x1 = data[index];
                var x2 = data[index + 1];
                var a = (float)(position - index);
                block[t] = (x1 + a * (x2 - x1)) / 32768;

                position += pitchRatio;
            }

            return true;
        }

        private bool Process_Continuous(float[] block, double pitchRatio)
        {
            var data = synthesizer.SoundFont.WaveData;

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
