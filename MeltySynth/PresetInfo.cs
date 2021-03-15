using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class PresetInfo
    {
        private string name;
        private int patchNumber;
        private int bankNumber;
        private int zoneStartIndex;
        private int zoneEndIndex;
        private int library;
        private int genre;
        private int morphology;

        private PresetInfo()
        {
        }

        internal static PresetInfo[] ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 38 != 0)
            {
                throw new InvalidDataException("The preset list is invalid.");
            }

            var count = size / 38;

            var presets = new PresetInfo[count];

            for (var i = 0; i < count; i++)
            {
                var preset = new PresetInfo();
                preset.name = reader.ReadFixedLengthString(20);
                preset.patchNumber = reader.ReadUInt16();
                preset.bankNumber = reader.ReadUInt16();
                preset.zoneStartIndex = reader.ReadUInt16();
                preset.library = reader.ReadInt32();
                preset.genre = reader.ReadInt32();
                preset.morphology = reader.ReadInt32();

                presets[i] = preset;
            }

            for (var i = 0; i < count - 1; i++)
            {
                presets[i].zoneEndIndex = presets[i + 1].zoneStartIndex - 1;
            }

            return presets;
        }

        internal string Name => name;
        internal int PatchNumber => patchNumber;
        internal int BankNumber => bankNumber;
        internal int ZoneStartIndex => zoneStartIndex;
        internal int ZoneEndIndex => zoneEndIndex;
        internal int Library => library;
        internal int Genre => genre;
        internal int Morphology => morphology;
    }
}
