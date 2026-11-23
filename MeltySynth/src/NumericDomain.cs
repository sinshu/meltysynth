using System;
using System.IO;

namespace MeltySynth
{
    // Asset/command boundaries only: never called from the sample loop.
    internal static class NumericDomain
    {
        internal static void Generator(Generator generator, bool relative)
        {
            var id = (int)generator.Type; var value = (short)generator.Value;
            if (id == 43 || id == 44)
            {
                var low = generator.Value & 255; var high = generator.Value >> 8;
                if (low > high || high > 127) Invalid(id, value);
                return;
            }
            var range = id switch
            {
                5 or 6 or 7 or 10 or 11 => (-12000, 12000),
                8 => (1500, 13500), 9 => (0, 960), 13 => (-960, 960),
                15 or 16 => (0, 1000), 17 => (-500, 500),
                21 or 23 or 25 or 27 or 33 or 35 => (-12000, 5000),
                22 or 24 => (-16000, 4500),
                26 or 28 or 30 or 34 or 36 or 38 => (-12000, 8000),
                29 => (0, 1000), 37 or 48 => (0, 1440),
                31 or 32 or 39 or 40 => (-1200, 1200),
                46 or 47 or 58 => (-1, 127), 51 => (-120, 120), 52 => (-99, 99),
                54 => (0, 3), 56 => (0, 1200), 57 => (0, 127),
                _ => ((int)short.MinValue, (int)short.MaxValue)
            };
            // SF2 absolute-zero timecents sentinel; retain the existing DSP interpretation.
            if (value == short.MinValue && (id is 21 or 23 or 25 or 26 or 27 or 28 or 30 or 33 or 34 or 35 or 36 or 38)) return;
            if (relative && id is not (46 or 47 or 54 or 57 or 58))
            {
                var extent = Math.Max(Math.Abs(range.Item1), Math.Abs(range.Item2));
                range = (-extent, extent);
            }
            if (value < range.Item1 || value > range.Item2) Invalid(id, value);
        }

        private static void Invalid(int id, int value)
            => throw new InvalidDataException($"SoundFont generator {id} has an unsupported numeric value {value}.");

        internal static void Midi(int channel, int command, int data1, int data2)
        {
            if ((uint)channel >= 16) throw new ArgumentOutOfRangeException(nameof(channel));
            if (command < 0x80 || command > 0xE0 || (command & 15) != 0)
                throw new ArgumentOutOfRangeException(nameof(command));
            Data(data1, nameof(data1)); Data(data2, nameof(data2));
        }
        internal static void Data(int value, string name)
        {
            if ((uint)value > 127) throw new ArgumentOutOfRangeException(name, "MIDI data must be in 0..127.");
        }
    }
}
