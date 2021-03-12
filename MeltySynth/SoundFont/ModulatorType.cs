using System;
using System.IO;

namespace MeltySynth.SoundFont
{
    public sealed class ModulatorType
    {
        private CurveType curveType;
        private CurvePolarity polarity;
        private CurveDirection direction;
        private bool isMidiContinuousController;
        private ushort controllerSource;

        internal ModulatorType(BinaryReader reader)
        {
            var value = reader.ReadUInt16();

            curveType = (CurveType)((value & (0xFC00)) >> 10);

            if ((value & 0x0200) != 0)
            {
                polarity = CurvePolarity.Bipolar;
            }
            else
            {
                polarity = CurvePolarity.Unipolar;
            }

            if ((value & 0x0100) != 0)
            {
                direction = CurveDirection.MaxToMin;
            }
            else
            {
                direction = CurveDirection.MinToMax;
            }

            isMidiContinuousController = ((value & 0x0080) != 0);

            controllerSource = (ushort)(value & 0x007F);
        }

        public override string ToString()
        {
            return "(" + curveType + ", " + polarity + ", " + direction + ", " + controllerSource + ")";
        }

        public CurveType CurveType => curveType;
        public CurvePolarity CurvePolarity => polarity;
        public CurveDirection CurveDirection => direction;
        public bool IsMidiContinuousController => isMidiContinuousController;
        public ushort ControllerSource => controllerSource;
    }
}
