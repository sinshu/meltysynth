using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MeltySynth
{
    public sealed class SoundFont
    {
        private SoundFontInfo info;
        private SoundFontSampleData sampleData;
        private SoundFontParameters parameters;

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
                sampleData = new SoundFontSampleData(reader);
                parameters = new SoundFontParameters(reader);
            }

            var sampleCount = sampleData.Samples.Length;
            foreach (var sample in parameters.SampleHeaders)
            {
                if (!(0 <= sample.Start && sample.Start < sampleCount))
                {
                    throw new InvalidDataException($"The start position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.StartLoop && sample.StartLoop < sampleCount))
                {
                    throw new InvalidDataException($"The loop start position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.End && sample.End < sampleCount))
                {
                    throw new InvalidDataException($"The end position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.EndLoop && sample.EndLoop < sampleCount))
                {
                    throw new InvalidDataException($"The loop end position of the sample '{sample.Name}' is out of range.");
                }
            }
        }

        public override string ToString()
        {
            return info.BankName;
        }

        public SoundFontInfo Info => info;
        public int BitsPerSample => sampleData.BitsPerSample;
        public short[] SampleData => sampleData.Samples;
        public IReadOnlyList<SampleHeader> SampleHeaders => parameters.SampleHeaders;
        public IReadOnlyList<Preset> Presets => parameters.Presets;
        public IReadOnlyList<Instrument> Instruments => parameters.Instruments;
    }
}
