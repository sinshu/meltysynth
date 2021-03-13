using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeltySynth.SoundFont;

namespace MeltySynth.Synthesis
{
    public sealed class Patch
    {
        public static Region[] GetRegions(Instrument instrument)
        {
            var zones = instrument.Zones;

            Zone global = null;

            // Is the first one the global zone?
            if (zones[0].GeneratorParameters.Count == 0 || zones[0].GeneratorParameters.Last().Type != GeneratorParameterType.SampleID)
            {
                // The first one is the global zone.
                global = zones[0];
            }

            if (global != null)
            {
                // The global zone is regarded as the base setting of subsequent zones.
                var regions = new Region[zones.Count - 1];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = GetRegion(global.GeneratorParameters, zones[i + 1].GeneratorParameters);
                }
                return regions;
            }
            else
            {
                // No global zone.
                var regions = new Region[zones.Count];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = GetRegion(null, zones[i].GeneratorParameters);
                }
                return regions;
            }
        }

        private static Region GetRegion(IEnumerable<GeneratorParameter> global, IEnumerable<GeneratorParameter> local)
        {
            var region = new Region();

            if (global != null)
            {
                foreach (var parameter in global)
                {
                    region.SetParameter(parameter);
                }
            }

            foreach (var parameter in local)
            {
                region.SetParameter(parameter);
            }

            return region;
        }
    }
}
