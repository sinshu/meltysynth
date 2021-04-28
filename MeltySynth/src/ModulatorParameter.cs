using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class ModulatorParameter
    {
        private readonly ModulatorParameterType sourceModulationData;
        private readonly GeneratorParameterType destinationGenerator;
        private readonly short amount;
        private readonly ModulatorParameterType sourceModulationAmount;
        private readonly TransformType sourceTransform;

        private ModulatorParameter(BinaryReader reader)
        {
            sourceModulationData = new ModulatorParameterType(reader);
            destinationGenerator = (GeneratorParameterType)reader.ReadUInt16();
            amount = reader.ReadInt16();
            sourceModulationAmount = new ModulatorParameterType(reader);
            sourceTransform = (TransformType)reader.ReadUInt16();
        }

        internal static ModulatorParameter[] ReadFromChunk(BinaryReader reader, int size)
        {
            if (size % 10 != 0)
            {
                throw new InvalidDataException("The modulator list is invalid.");
            }

            var modulators = new ModulatorParameter[size / 10 - 1];

            for (var i = 0; i < modulators.Length; i++)
            {
                modulators[i] = new ModulatorParameter(reader);
            }

            // The last one is the terminator.
            new ModulatorParameter(reader);

            return modulators;
        }

        public ModulatorParameterType SourceModulationData => sourceModulationData;
        public GeneratorParameterType DestinationGenerator => destinationGenerator;
        public short Amount => amount;
        public ModulatorParameterType SourceModulationAmount => sourceModulationAmount;
        public TransformType SourceTransform => sourceTransform;
    }
}
