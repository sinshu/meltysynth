using System;

namespace MeltySynth
{
    internal sealed class Voice
    {
        private Synthesizer synthesizer;

        private VolumeEnvelope volumeEnvelope;
        private ModulationEnvelope modulationEnvelope;

        private Generator generator;

        private float[] block;

        private RegionPair region;
        private int channel;
        private int key;
        private int velocity;

        internal Voice(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;

            volumeEnvelope = new VolumeEnvelope(synthesizer);
            modulationEnvelope = new ModulationEnvelope(synthesizer);

            generator = new Generator(synthesizer);

            block = new float[synthesizer.BlockSize];
        }

        public void Start(RegionPair region, int channel, int key, int velocity)
        {
            volumeEnvelope.Start(region, key, velocity);
            modulationEnvelope.Start(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);

            this.region = region;
            this.channel = channel;
            this.key = key;
            this.velocity = velocity;
        }

        public void End()
        {
            volumeEnvelope.Release();
            modulationEnvelope.Release();
            generator.Release();
        }

        public bool Process()
        {
            if (!volumeEnvelope.Process())
            {
                return false;
            }

            if (!generator.Process(block, key))
            {
                return false;
            }

            modulationEnvelope.Process();

            var test = volumeEnvelope.Value;
            for (var t = 0; t < block.Length; t++)
            {
                block[t] *= test;
            }

            return true;
        }

        public float[] Block => block;
        public int Channel => channel;
        public int Key => key;
        public int Velocity => velocity;
    }
}
