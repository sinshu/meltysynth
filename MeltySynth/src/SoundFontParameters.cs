using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class SoundFontParameters
    {
        private readonly SampleHeader[] sampleHeaders;
        private readonly Preset[] presets;
        private readonly Instrument[] instruments;

        internal SoundFontParameters(BinaryReader reader)
        {
            var chunkId = reader.ReadFourCC();
            if (chunkId != "LIST")
            {
                throw new InvalidDataException("The LIST chunk was not found.");
            }

            var end = reader.BaseStream.Position + reader.ReadInt32();

            var listType = reader.ReadFourCC();
            if (listType != "pdta")
            {
                throw new InvalidDataException($"The type of the LIST chunk must be 'pdta', but was '{listType}'.");
            }

            PresetInfo[]? presetInfos = null;
            ZoneInfo[]? presetBag = null;
            Generator[]? presetGenerators = null;
            InstrumentInfo[]? instrumentInfos = null;
            ZoneInfo[]? instrumentBag = null;
            Generator[]? instrumentGenerators = null;

            while (reader.BaseStream.Position < end)
            {
                var id = reader.ReadFourCC();
                var size = reader.ReadInt32();

                switch (id)
                {
                    case "phdr":
                        presetInfos = PresetInfo.ReadFromChunk(reader, size);
                        break;
                    case "pbag":
                        presetBag = ZoneInfo.ReadFromChunk(reader, size);
                        break;
                    case "pmod":
                        Modulator.DiscardData(reader, size);
                        break;
                    case "pgen":
                        presetGenerators = Generator.ReadFromChunk(reader, size);
                        break;
                    case "inst":
                        instrumentInfos = InstrumentInfo.ReadFromChunk(reader, size);
                        break;
                    case "ibag":
                        instrumentBag = ZoneInfo.ReadFromChunk(reader, size);
                        break;
                    case "imod":
                        Modulator.DiscardData(reader, size);
                        break;
                    case "igen":
                        instrumentGenerators = Generator.ReadFromChunk(reader, size);
                        break;
                    case "shdr":
                        sampleHeaders = SampleHeader.ReadFromChunk(reader, size);
                        break;
                    default:
                        throw new InvalidDataException($"The INFO list contains an unknown ID '{id}'.");
                }
            }

            if (presetInfos == null) throw new InvalidDataException("The PHDR sub-chunk was not found.");
            if (presetBag == null) throw new InvalidDataException("The PBAG sub-chunk was not found.");
            if (presetGenerators == null) throw new InvalidDataException("The PGEN sub-chunk was not found.");
            if (instrumentInfos == null) throw new InvalidDataException("The INST sub-chunk was not found.");
            if (instrumentBag == null) throw new InvalidDataException("The IBAG sub-chunk was not found.");
            if (instrumentGenerators == null) throw new InvalidDataException("The IGEN sub-chunk was not found.");
            if (sampleHeaders == null) throw new InvalidDataException("The SHDR sub-chunk was not found.");

            var instrumentZones = Zone.Create(instrumentBag, instrumentGenerators);
            instruments = Instrument.Create(instrumentInfos, instrumentZones, sampleHeaders);

            var presetZones = Zone.Create(presetBag, presetGenerators);
            presets = Preset.Create(presetInfos, presetZones, instruments);
        }

        public SampleHeader[] SampleHeaders => sampleHeaders;
        public Preset[] Presets => presets;
        public Instrument[] Instruments => instruments;
    }
}
