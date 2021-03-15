using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class SoundFontParameters
    {
        private SampleHeader[] sampleHeaders;
        private Preset[] presets;
        private Instrument[] instruments;

        internal SoundFontParameters(BinaryReader reader)
        {
            var chunkId = reader.ReadFixedLengthString(4);
            if (chunkId != "LIST")
            {
                throw new InvalidDataException("The LIST chunk was not found.");
            }

            var end = reader.BaseStream.Position + reader.ReadInt32();

            var listType = reader.ReadFixedLengthString(4);
            if (listType != "pdta")
            {
                throw new InvalidDataException($"The type of the LIST chunk must be 'pdta', but was '{listType}'.");
            }

            PresetInfo[] presetInfos = null;
            ZoneInfo[] presetBag = null;
            ModulatorParameter[] presetModulators = null;
            GeneratorParameter[] presetGenerators = null;
            InstrumentInfo[] instrumentInfos = null;
            ZoneInfo[] instrumentBag = null;
            ModulatorParameter[] instrumentModulators = null;
            GeneratorParameter[] instrumentGenerators = null;

            while (reader.BaseStream.Position < end)
            {
                var id = reader.ReadFixedLengthString(4);
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
                        presetModulators = ModulatorParameter.ReadFromChunk(reader, size);
                        break;
                    case "pgen":
                        presetGenerators = GeneratorParameter.ReadFromChunk(reader, size);
                        break;
                    case "inst":
                        instrumentInfos = InstrumentInfo.ReadFromChunk(reader, size);
                        break;
                    case "ibag":
                        instrumentBag = ZoneInfo.ReadFromChunk(reader, size);
                        break;
                    case "imod":
                        instrumentModulators = ModulatorParameter.ReadFromChunk(reader, size);
                        break;
                    case "igen":
                        instrumentGenerators = GeneratorParameter.ReadFromChunk(reader, size);
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
            if (presetModulators == null) throw new InvalidDataException("The PMOD sub-chunk was not found.");
            if (presetGenerators == null) throw new InvalidDataException("The PGEN sub-chunk was not found.");
            if (instrumentInfos == null) throw new InvalidDataException("The INST sub-chunk was not found.");
            if (instrumentBag == null) throw new InvalidDataException("The IBAG sub-chunk was not found.");
            if (instrumentModulators == null) throw new InvalidDataException("The IMOD sub-chunk was not found.");
            if (instrumentGenerators == null) throw new InvalidDataException("The IGEN sub-chunk was not found.");
            if (sampleHeaders == null) throw new InvalidDataException("The SHDR sub-chunk was not found.");

            var instrumentZones = Zone.Create(instrumentBag, instrumentGenerators, instrumentModulators);
            instruments = Instrument.Create(instrumentInfos, instrumentZones, sampleHeaders);

            var presetZones = Zone.Create(presetBag, presetGenerators, presetModulators);
            presets = Preset.Create(presetInfos, presetZones, instruments);
        }

        internal SampleHeader[] SampleHeaders => sampleHeaders;
        internal Preset[] Presets => presets;
        internal Instrument[] Instruments => instruments;
    }
}
