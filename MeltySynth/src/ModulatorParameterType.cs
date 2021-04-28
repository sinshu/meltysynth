using System;
using System.IO;

namespace MeltySynth
{
    internal sealed class ModulatorParameterType
    {
        private readonly CurveType curveType;
        private readonly CurvePolarity polarity;
        private readonly CurveDirection direction;
        private readonly bool isMidiContinuousController;
        private readonly ushort controllerSource;

        internal ModulatorParameterType(BinaryReader reader)
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

        public CurveType CurveType => curveType;
        public CurvePolarity CurvePolarity => polarity;
        public CurveDirection CurveDirection => direction;
        public bool IsMidiContinuousController => isMidiContinuousController;
        public ushort ControllerSource => controllerSource;
    }
}
