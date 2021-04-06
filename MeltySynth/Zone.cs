using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class Zone
    {
        private ModulatorParameter[] modulatorParameters;
        private GeneratorParameter[] generatorParameters;

        private Zone()
        {
        }

        internal static Zone[] Create(ZoneInfo[] infos, GeneratorParameter[] gps, ModulatorParameter[] mps)
        {
            if (infos.Length <= 1)
            {
                throw new InvalidDataException("No valid zone was found.");
            }

            // The last one is the terminator.
            var zones = new Zone[infos.Length - 1];

            for (var i = 0; i < zones.Length; i++)
            {
                var info = infos[i];

                var zone = new Zone();
                zone.generatorParameters = new GeneratorParameter[info.GeneratorCount];
                for (var j = 0; j < zone.generatorParameters.Length; j++)
                {
                    zone.generatorParameters[j] = gps[info.GeneratorIndex + j];
                }
                zone.modulatorParameters = new ModulatorParameter[info.ModulatorCount];
                for (var j = 0; j < zone.modulatorParameters.Length; j++)
                {
                    zone.modulatorParameters[j] = mps[info.ModulatorIndex + j];
                }

                zones[i] = zone;
            }

            return zones;
        }

        public ModulatorParameter[] ModulatorParameters => modulatorParameters;
        public GeneratorParameter[] GeneratorParameters => generatorParameters;
    }
}
