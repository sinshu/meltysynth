using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    internal sealed class SoundFontParameters
    {
        private IReadOnlyList<SampleHeader> sampleHeaders;
        private IReadOnlyList<Preset> presets;
        private IReadOnlyList<Instrument> instruments;

        internal SoundFontParameters(BinaryReader reader)
        {
            var chunkId = reader.ReadAsciiString(4);
            if (chunkId != "LIST")
            {
                throw new InvalidDataException("The LIST chunk was not found.");
            }

            var end = reader.BaseStream.Position + reader.ReadInt32();

            var listType = reader.ReadAsciiString(4);
            if (listType != "pdta")
            {
                throw new InvalidDataException($"The type of the LIST chunk must be 'pdta', but was '{listType}'.");
            }

            IReadOnlyList<PresetInfo> presetInfos = null;
            IReadOnlyList<ZoneInfo> presetBag = null;
            IReadOnlyList<ModulatorParameter> presetModulators = null;
            IReadOnlyList<GeneratorParameter> presetGenerators = null;

            IReadOnlyList<InstrumentInfo> instrumentInfos = null;
            IReadOnlyList<ZoneInfo> instrumentBag = null;
            IReadOnlyList<ModulatorParameter> instrumentModulators = null;
            IReadOnlyList<GeneratorParameter> instrumentGenerators = null;

            while (reader.BaseStream.Position < end)
            {
                var id = reader.ReadAsciiString(4);
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

            var presetZones = Zone.Create(presetBag, presetModulators, presetGenerators);
            presets = Preset.Create(presetInfos, presetZones);

            var instrumentZones = Zone.Create(instrumentBag, instrumentModulators, instrumentGenerators);
            instruments = Instrument.Create(instrumentInfos, instrumentZones);
        }

        public IReadOnlyList<SampleHeader> SampleHeaders => sampleHeaders;
        public IReadOnlyList<Preset> Presets => presets;
        public IReadOnlyList<Instrument> Instruments => instruments;
    }
}
