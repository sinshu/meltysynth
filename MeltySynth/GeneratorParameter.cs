﻿using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class GeneratorParameter
    {
        private GeneratorParameterType type;
        private ushort value;

        private GeneratorParameter(BinaryReader reader)
        {
            type = (GeneratorParameterType)reader.ReadUInt16();
            value = reader.ReadUInt16();
        }

        internal static GeneratorParameter[] ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 4 != 0)
            {
                throw new InvalidDataException("The generator list is invalid.");
            }

            var generators = new GeneratorParameter[size / 4 - 1];

            for (var i = 0; i < generators.Length; i++)
            {
                generators[i] = new GeneratorParameter(reader);
            }

            // The last one is the terminator.
            new GeneratorParameter(reader);

            return generators;
        }

        internal GeneratorParameterType Type => type;
        internal ushort Value => value;
    }
}
