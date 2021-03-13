using System;
using System.Collections.Generic;
using System.IO;

namespace MeltySynth.SoundFont
{
    public sealed class Zone
    {
        private ModulatorParameter[] modulatorParameters;
        private GeneratorParameter[] generatorParameters;

        private Zone()
        {
        }

        internal static IReadOnlyList<Zone> Create(IReadOnlyList<ZoneInfo> infos, IReadOnlyList<ModulatorParameter> mps, IReadOnlyList<GeneratorParameter> gps)
        {
            var zones = new Zone[infos.Count];

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

        public override string ToString()
        {
            return $"Modulator: {modulatorParameters.Length}, Generator: {generatorParameters.Length}";
        }

        public IReadOnlyList<ModulatorParameter> ModulatorParameters => modulatorParameters;
        public IReadOnlyList<GeneratorParameter> GeneratorParameters => generatorParameters;
    }
}
