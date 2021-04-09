using System;

namespace MeltySynth
{
    internal sealed class Voice
    {
        private Synthesizer synthesizer;

        private VolumeEnvelope volumeEnvelope;
        private ModulationEnvelope modulationEnvelope;

        private Generator generator;
        private BiQuadFilter filter;

        private float[] block;

        private float mixFactor;

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
            filter = new BiQuadFilter(synthesizer);

            block = new float[synthesizer.BlockSize];
        }

        public void Start(RegionPair region, int channel, int key, int velocity)
        {
            volumeEnvelope.Start(region, key, velocity);
            modulationEnvelope.Start(region, key, velocity);
            generator.Start(synthesizer.SoundFont.WaveData, region);
            filter.ClearBuffer();
            filter.SetLowPassFilter(region.InitialFilterCutoffFrequency, 1);

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
            float noteGain;
            if (velocity > 0)
            {
                noteGain = 2 * SoundFontMath.LinearToDecibels(velocity / 127F) - region.InitialAttenuation;
            }
            else
            {
                return false;
            }

            if (!volumeEnvelope.Process())
            {
                return false;
            }

            if (!generator.Process(block, key))
            {
                return false;
            }

            modulationEnvelope.Process();

            var cutoffFrequency = region.InitialFilterCutoffFrequency;
            cutoffFrequency *= SoundFontMath.CentsToMultiplyingFactor(modulationEnvelope.Value * region.ModulationEnvelopeToFilterCutoffFrequency);
            filter.SetLowPassFilter(cutoffFrequency, 1);
            filter.Process(block);

            var noteFactor = SoundFontMath.DecibelsToLinear(noteGain);

            var channelInfo = synthesizer.Channels[channel];
            var channelFactor = channelInfo.Volume * channelInfo.Expression;

            mixFactor = noteFactor * channelFactor * volumeEnvelope.Value;

            return true;
        }

        public float[] Block => block;
        public float MixFactor => mixFactor;

        public int Channel => channel;
        public int Key => key;
        public int Velocity => velocity;
    }
}
