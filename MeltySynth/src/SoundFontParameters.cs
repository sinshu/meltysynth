using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class SoundFontParameters
    {
        private readonly SampleHeader[] sampleHeaders;
        private readonly Preset[] presets;
        private readonly Instrument[] instruments;

        internal SoundFontParameters(BinaryReader reader, long containerEnd, AssetLoadLimits limits)
        {
            var end = AssetReader.List(reader, containerEnd, "pdta");
            PresetInfo[]? presetInfos = null;
            ZoneInfo[]? presetBag = null;
            Generator[]? presetGenerators = null;
            InstrumentInfo[]? instrumentInfos = null;
            ZoneInfo[]? instrumentBag = null;
            Generator[]? instrumentGenerators = null;

            var seen = new System.Collections.Generic.HashSet<string>();
            var records = 0;
            while (reader.BaseStream.Position < end)
            {
                var chunk = AssetReader.Chunk(reader, end);
                var id = chunk.Id;
                var size = chunk.Size;
                if (!seen.Add(id)) throw new InvalidDataException("Duplicate SoundFont parameter chunk: " + id);
                int stride;
                switch (id)
                {
                    case "phdr": stride = 38; break;
                    case "inst": stride = 22; break;
                    case "shdr": stride = 46; break;
                    case "pmod": case "imod": stride = 10; break;
                    case "pbag": case "ibag": case "pgen": case "igen": stride = 4; break;
                    default: throw new InvalidDataException("Unknown SoundFont parameter chunk: " + id);
                }
                if (size % stride != 0 || size / stride > limits.MaxSoundFontRecords - records)
                    throw new InvalidDataException("SoundFont parameter records exceed their alignment or aggregate budget.");
                records += size / stride;
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
                if (reader.BaseStream.Position != chunk.End)
                    throw new InvalidDataException("The parameter chunk was not consumed exactly.");
                if ((size & 1) != 0) reader.ReadByte();
            }

            if (presetInfos == null) throw new InvalidDataException("The PHDR sub-chunk was not found.");
            if (presetBag == null) throw new InvalidDataException("The PBAG sub-chunk was not found.");
            if (presetGenerators == null) throw new InvalidDataException("The PGEN sub-chunk was not found.");
            if (instrumentInfos == null) throw new InvalidDataException("The INST sub-chunk was not found.");
            if (instrumentBag == null) throw new InvalidDataException("The IBAG sub-chunk was not found.");
            if (instrumentGenerators == null) throw new InvalidDataException("The IGEN sub-chunk was not found.");
            if (sampleHeaders == null) throw new InvalidDataException("The SHDR sub-chunk was not found.");

            var regions = 0;
            foreach (var info in instrumentInfos.AsSpan(0, instrumentInfos.Length - 1))
                CountRegions(info.ZoneStartIndex, info.ZoneEndIndex, instrumentBag.Length - 1, limits, ref regions);
            foreach (var info in presetInfos.AsSpan(0, presetInfos.Length - 1))
                CountRegions(info.ZoneStartIndex, info.ZoneEndIndex, presetBag.Length - 1, limits, ref regions);

            var instrumentZones = Zone.Create(instrumentBag, instrumentGenerators);
            instruments = Instrument.Create(instrumentInfos, instrumentZones, sampleHeaders);

            var presetZones = Zone.Create(presetBag, presetGenerators);
            presets = Preset.Create(presetInfos, presetZones, instruments);
        }

        private static void CountRegions(int start, int end, int zoneCount, AssetLoadLimits limits, ref int total)
        {
            if (start < 0 || end < start || end >= zoneCount || end - start + 1 > limits.MaxSoundFontRegions - total)
                throw new InvalidDataException("SoundFont zones exceed their references or aggregate region budget.");
            total += end - start + 1;
        }

        public SampleHeader[] SampleHeaders => sampleHeaders;
        public Preset[] Presets => presets;
        public Instrument[] Instruments => instruments;
    }
}
