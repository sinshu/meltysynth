using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace MeltySynth
{
    public sealed class SoundFont
    {
        private SoundFontInfo info;
        private int bitsPerSample;
        private short[] waveData;
        private ImmutableArray<SampleHeader> sampleHeaders;
        private ImmutableArray<Preset> presets;
        private ImmutableArray<Instrument> instruments;

        public SoundFont(Stream stream)
        {
            Load(stream);
        }

        public SoundFont(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream);
            }
        }

        private void Load(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var chunkId = reader.ReadFixedLengthString(4);
                if (chunkId != "RIFF")
                {
                    throw new InvalidDataException("The RIFF chunk was not found.");
                }

                var size = reader.ReadInt32();

                var formType = reader.ReadFixedLengthString(4);
                if (formType != "sfbk")
                {
                    throw new InvalidDataException($"The type of the RIFF chunk must be 'sfbk', but was '{formType}'.");
                }

                info = new SoundFontInfo(reader);

                var sampleData = new SoundFontSampleData(reader);
                bitsPerSample = sampleData.BitsPerSample;
                waveData = sampleData.Samples;

                var parameters = new SoundFontParameters(reader);
                sampleHeaders = ImmutableArray.Create(parameters.SampleHeaders);
                presets = ImmutableArray.Create(parameters.Presets);
                instruments = ImmutableArray.Create(parameters.Instruments);
            }

            CheckSamples();
            CheckRegions();
        }

        public override string ToString()
        {
            return info.BankName;
        }

        private void CheckSamples()
        {
            var sampleCount = waveData.Length - 4; // This small offset is to ensure that out of range access is safe in the DSP.

            foreach (var sample in sampleHeaders)
            {
                if (!(0 <= sample.Start && sample.Start < sampleCount))
                {
                    throw new InvalidDataException($"The start position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.StartLoop && sample.StartLoop < sampleCount))
                {
                    throw new InvalidDataException($"The loop start position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.End && sample.End + 4 < sampleCount))
                {
                    throw new InvalidDataException($"The end position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.EndLoop && sample.EndLoop < sampleCount))
                {
                    throw new InvalidDataException($"The loop end position of the sample '{sample.Name}' is out of range.");
                }
            }
        }

        private void CheckRegions()
        {
            var sampleCount = waveData.Length - 4; // This small offset is to ensure that out of range access is safe in the DSP.

            foreach (var instrument in instruments)
            {
                foreach (var region in instrument.Regions)
                {
                    if (!(0 <= region.SampleStart && region.SampleStart < sampleCount))
                    {
                        throw new InvalidDataException($"The start position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }
                    if (!(0 <= region.SampleStartLoop && region.SampleStartLoop < sampleCount))
                    {
                        throw new InvalidDataException($"The loop start position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }
                    if (!(0 <= region.SampleEnd && region.SampleEnd + 4 < sampleCount))
                    {
                        throw new InvalidDataException($"The end position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }
                    if (!(0 <= region.SampleEndLoop && region.SampleEndLoop < sampleCount))
                    {
                        throw new InvalidDataException($"The loop end position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }

                    switch (region.SampleModes)
                    {
                        case LoopMode.NoLoop:
                        case LoopMode.Continuous:
                        case LoopMode.LoopUntilNoteOff:
                            break;
                        default:
                            throw new InvalidDataException($"The sample '{region.Sample.Name}' in the instrument '{instrument.Name}' has an invalid loop mode.");
                    }
                }
            }
        }

        public SoundFontInfo Info => info;
        public int BitsPerSample => bitsPerSample;
        public short[] WaveData => waveData;
        public ImmutableArray<SampleHeader> SampleHeaders => sampleHeaders;
        public ImmutableArray<Preset> Presets => presets;
        public ImmutableArray<Instrument> Instruments => instruments;
    }
}
