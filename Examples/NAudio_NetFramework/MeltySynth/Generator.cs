using System;
using System.IO;

namespace MeltySynth
{
    internal struct Generator
    {
        private readonly GeneratorType type;
        private readonly ushort value;

        private Generator(BinaryReader reader)
        {
            type = (GeneratorType)reader.ReadUInt16();
            value = reader.ReadUInt16();
        }

        internal static Generator[] ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 4 != 0)
            {
                throw new InvalidDataException("The generator list is invalid.");
            }

            var generators = new Generator[size / 4 - 1];

            for (var i = 0; i < generators.Length; i++)
            {
                generators[i] = new Generator(reader);
            }

            // The last one is the terminator.
            new Generator(reader);

            return generators;
        }

        public GeneratorType Type => type;
        public ushort Value => value;
    }
}
