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

        private bool isActive;

        internal Voice(Synthesizer synthesizer)
        {
            this.synthesizer = synthesizer;

            volumeEnvelope = new VolumeEnvelope(synthesizer);
            modulationEnvelope = new ModulationEnvelope(synthesizer);
            generator = new Generator(synthesizer);

            block = new float[synthesizer.BlockSize];

            isActive = false;
        }

        internal void Start(RegionPair region, int channel, int key, int velocity)
        {
            volumeEnvelope.Start(region, key, velocity);
            modulationEnvelope.Start(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);

            this.region = region;
            this.channel = channel;
            this.key = key;
            this.velocity = velocity;

            isActive = true;
        }

        internal void End()
        {
            volumeEnvelope.Release();

            if (region.SampleModes == LoopMode.LoopUntilNoteOff)
            {
                generator.Release();
            }
        }

        internal void Process()
        {
            var volumeEnvelopeIsActive = volumeEnvelope.Process(synthesizer.BlockSize);
            var modulationEnvelopeIsActive = modulationEnvelope.Process(synthesizer.BlockSize);

            if (!volumeEnvelopeIsActive)
            {
                isActive = false;
                return;
            }

            var generatorIsActive = generator.Process(block, key);

            if (!generatorIsActive)
            {
                isActive = false;
                return;
            }

            var test = volumeEnvelope.Value;
            for (var t = 0; t < block.Length; t++)
            {
                block[t] *= test;
            }
        }

        internal float[] Block => block;
        internal bool IsActive => isActive;
    }
}
