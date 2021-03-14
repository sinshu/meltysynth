using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeltySynth.SoundFont;
using MeltySynth.Synthesis;

public static class DumpInfo
{
    public static void DumpInstruments(SoundFont sf, string directory)
    {
        foreach (var instrument in sf.Instruments)
        {
            Console.WriteLine(instrument.Name);

            var regions = Patch
                .GetRegions(instrument)
                .OrderBy(x => x.KeyRangeStart)
                .ThenBy(x => sf.SampleHeaders[x.SampleID].Name)
                .ToArray();

            var path = Path.Combine(directory, instrument.Name.Replace('/', ' ') + ".csv");
            using (var writer = new StreamWriter(path))
            {
                foreach (var region in regions)
                {
                    var sample = sf.SampleHeaders[region.SampleID];
                    writer.Write("," + sample.Name);
                }
                writer.WriteLine();

                writer.Write("Key range");
                foreach (var region in regions)
                {
                    writer.Write("," + region.KeyRangeStart + "-" + region.KeyRangeEnd);
                }
                writer.WriteLine();

                writer.Write("Velocity range");
                foreach (var region in regions)
                {
                    writer.Write("," + region.VelocityRangeStart + "-" + region.VelocityRangeEnd);
                }
                writer.WriteLine();

                writer.Write("Attenuation (dB)");
                foreach (var region in regions)
                {
                    writer.Write("," + (0.4 * region.InitialAttenuation).ToString("0.00")); // Where did the 0.4 come from?
                }
                writer.WriteLine();

                writer.Write("Pan [-50;50]");
                foreach (var region in regions)
                {
                    writer.Write("," + region.Pan);
                }
                writer.WriteLine();

                writer.Write("Loop playback");
                foreach (var region in regions)
                {
                    writer.Write("," + region.SampleMode);
                }
                writer.WriteLine();

                writer.Write("Sample root key");
                foreach (var region in regions)
                {
                    writer.Write("," + sf.SampleHeaders[region.SampleID].OriginalPitch);
                }
                writer.WriteLine();

                writer.Write("Root key");
                foreach (var region in regions)
                {
                    writer.Write("," + region.OverridingRootKey);
                }
                writer.WriteLine();

                writer.Write("Tuing (semi-tones)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.CoarseTune);
                }
                writer.WriteLine();

                writer.Write("Tuing (cents)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.FineTune);
                }
                writer.WriteLine();

                writer.Write("\"Filter, cutoff (Hz)\"");
                foreach (var region in regions)
                {
                    writer.Write("," + Math.Round(region.InitialFilterCutoffFrequency));
                }
                writer.WriteLine();

                writer.Write("\"Filter, resonance (dB)\"");
                foreach (var region in regions)
                {
                    writer.Write("," + region.InitialFilterQ.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Vol env delay (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vol env attack (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.AttackVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vol env hold (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.HoldVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vol env decay (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DecayVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vol env sustain (dB)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.SustainVolumeEnvelope.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Vol env release (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ReleaseVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Key -> Vol env hold (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.KeyNumberToVolumeEnvelopeHold);
                }
                writer.WriteLine();

                writer.Write("Key -> Vol env decay (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.KeyNumberToVolumeEnvelopeDecay);
                }
                writer.WriteLine();
            }
        }
    }
}
