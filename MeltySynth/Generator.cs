using System;

namespace MeltySynth
{
    internal sealed class Generator
    {
        private Synthesizer synthesizer;

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

        internal void Start(InstrumentRegion region)
        {
            Start(new RegionPair(PresetRegion.Default, region));
        }

        internal void Start(RegionPair region)
        {
            loopMode = region.SampleModes;
            start = region.Instrument.Sample.Start + region.StartAddressOffset;
            end = region.Instrument.Sample.End + region.EndAddressOffset;
            startLoop = region.Instrument.Sample.StartLoop + region.StartLoopAddressOffset;
            endLoop = region.Instrument.Sample.EndLoop + region.EndLoopAddressOffset;

            position = start;
        }

        internal bool Process(float[] block, double speed)
        {
            switch (loopMode)
            {
                case LoopMode.NoLoop:
                    return Process_NoLoop(block, speed);
                default:
                    return true;
            }
        }

        private bool Process_NoLoop(float[] block, double speed)
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
                var a = position - index;
                block[t] = (float)((x1 + a * (x2 - x1)) / 32768);

                position += speed;
            }

            return true;
        }
    }
}
