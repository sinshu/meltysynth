using System;
using System.IO;
using System.Linq;

namespace MeltySynth
{
    public sealed class PresetRegion
    {
        private Preset preset;
        private Instrument instrument;
        private short[] gps;

        private PresetRegion(Preset preset, GeneratorParameter[] global, GeneratorParameter[] local, Instrument[] instruments)
        {
            this.preset = preset;

            gps = new short[61];
            gps[(int)GeneratorParameterType.KeyRange] = 0x7F00;
            gps[(int)GeneratorParameterType.VelocityRange] = 0x7F00;

            if (global != null)
            {
                foreach (var parameter in global)
                {
                    SetParameter(parameter);
                }
            }

            foreach (var parameter in local)
            {
                SetParameter(parameter);
            }

            var id = gps[(int)GeneratorParameterType.Instrument];
            if (!(0 <= id && id < instruments.Length))
            {
                throw new InvalidDataException($"The preset {preset.Name} contains an invalid instrument ID.");
            }
            instrument = instruments[id];
        }

        internal static PresetRegion[] Create(Preset preset, Zone[] zones, Instrument[] instruments)
        {
            Zone global = null;

            // Is the first one the global zone?
            if (zones[0].GeneratorParameters.Length == 0 || zones[0].GeneratorParameters.Last().Type != GeneratorParameterType.Instrument)
            {
                // The first one is the global zone.
                global = zones[0];
            }

            if (global != null)
            {
                // The global zone is regarded as the base setting of subsequent zones.
                var regions = new PresetRegion[zones.Length - 1];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new PresetRegion(preset, global.GeneratorParameters, zones[i + 1].GeneratorParameters, instruments);
                }
                return regions;
            }
            else
            {
                // No global zone.
                var regions = new PresetRegion[zones.Length];
                for (var i = 0; i < regions.Length; i++)
                {
                    regions[i] = new PresetRegion(preset, null, zones[i].GeneratorParameters, instruments);
                }
                return regions;
            }
        }

        private void SetParameter(GeneratorParameter parameter)
        {
            gps[(int)parameter.Type] = (short)parameter.Value;
        }

        public override string ToString()
        {
            return $"{preset.Name} (Key: {KeyRangeStart}-{KeyRangeEnd}, Velocity: {VelocityRangeStart}-{VelocityRangeEnd})";
        }

        internal short this[GeneratorParameterType generatortType] => gps[(int)generatortType];

        public Instrument Instrument => instrument;

        public int KeyRangeStart => gps[(int)GeneratorParameterType.KeyRange] & 0xFF;
        public int KeyRangeEnd => (gps[(int)GeneratorParameterType.KeyRange] >> 8) & 0xFF;
        public int VelocityRangeStart => gps[(int)GeneratorParameterType.VelocityRange] & 0xFF;
        public int VelocityRangeEnd => (gps[(int)GeneratorParameterType.VelocityRange] >> 8) & 0xFF;
    }
}
