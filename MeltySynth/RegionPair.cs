using System;

namespace MeltySynth
{
    internal struct RegionPair
    {
        private PresetRegion preset;
        private InstrumentRegion instrument;

        internal RegionPair(PresetRegion preset, InstrumentRegion instrument)
        {
            this.preset = preset;
            this.instrument = instrument;
        }

        internal PresetRegion Preset => preset;
        internal InstrumentRegion Instrument => instrument;
    }
}
