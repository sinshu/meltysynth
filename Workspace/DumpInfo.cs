using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeltySynth;

public static class DumpInfo
{
    public static void DumpInstruments(SoundFont soundFont, string directory)
    {
        foreach (var instrument in soundFont.Instruments)
        {
            Console.WriteLine(instrument.Name);

            var regions = instrument.Regions
                .OrderBy(x => x.KeyRangeStart)
                .ThenBy(x => x.VelocityRangeStart)
                .ThenBy(x => x.Sample.Name);

            var filename = instrument.Name.Replace('/', ' ').Replace(':', ' ');
            var path = Path.Combine(directory, filename + ".csv");
            using (var writer = new StreamWriter(path))
            {
                foreach (var region in regions)
                {
                    writer.Write("," + region.Sample.Name);
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
                    writer.Write("," + Math.Round(region.Pan));
                }
                writer.WriteLine();

                writer.Write("Loop playback");
                foreach (var region in regions)
                {
                    writer.Write("," + region.SampleModes);
                }
                writer.WriteLine();

                writer.Write("Sample root key");
                foreach (var region in regions)
                {
                    writer.Write("," + region.Sample.OriginalPitch);
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

                writer.Write("Scale tuing");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ScaleTuning);
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

                writer.Write("Mod env delay (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env attack (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.AttackModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env hold (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.HoldModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env decay (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DecayModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env sustain (%)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.SustainModulationEnvelope.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Mod env release (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ReleaseModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env -> pitch (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationEnvelopeToPitch);
                }
                writer.WriteLine();

                writer.Write("Mod env -> filter (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationEnvelopeToFilterCutoffFrequency);
                }
                writer.WriteLine();

                writer.Write("Key -> Mod env hold (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.KeyNumberToModulationEnvelopeHold);
                }
                writer.WriteLine();

                writer.Write("Key -> Mod env decay (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.KeyNumberToModulationEnvelopeDecay);
                }
                writer.WriteLine();

                writer.Write("Mod LFO delay (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayModulationLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod LFO freq (Hz)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.FrequencyModulationLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod LFO -> pitch (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationLfoToPitch);
                }
                writer.WriteLine();

                writer.Write("Mod LFO -> filter (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationLfoToFilterCutoffFrequency);
                }
                writer.WriteLine();

                writer.Write("Mod LFO -> volume (dB)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationLfoToVolume.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Vib LFO delay (s)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayVibratoLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vib LFO freq (Hz)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.FrequencyVibratoLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vib LFO -> pitch (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.VibratoLfoToPitch);
                }
                writer.WriteLine();

                writer.Write("Exclusive class");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ExclusiveClass);
                }
                writer.WriteLine();

                writer.Write("Chorus (%)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ChorusEffectsSend.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Reverb (%)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ReverbEffectsSend.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Fixed key");
                foreach (var region in regions)
                {
                    writer.Write(",???");
                }
                writer.WriteLine();

                writer.Write("Fixed velocity");
                foreach (var region in regions)
                {
                    writer.Write(",???");
                }
                writer.WriteLine();

                writer.Write("Sample length");
                foreach (var region in regions)
                {
                    writer.Write("," + (region.Sample.End - region.Sample.Start));
                }
                writer.WriteLine();

                writer.Write("Sample start offset");
                foreach (var region in regions)
                {
                    writer.Write("," + region.StartAddressOffset);
                }
                writer.WriteLine();

                writer.Write("Sample end offset");
                foreach (var region in regions)
                {
                    writer.Write("," + region.EndAddressOffset);
                }
                writer.WriteLine();

                writer.Write("Loop start");
                foreach (var region in regions)
                {
                    writer.Write("," + (region.Sample.StartLoop - region.Sample.Start) + "-" + (region.Sample.EndLoop - region.Sample.Start));
                }
                writer.WriteLine();

                writer.Write("Loop start offset");
                foreach (var region in regions)
                {
                    writer.Write("," + region.StartLoopAddressOffset);
                }
                writer.WriteLine();

                writer.Write("Loop end offset");
                foreach (var region in regions)
                {
                    writer.Write("," + region.EndLoopAddressOffset);
                }
                writer.WriteLine();
            }
        }
    }

    public static void DumpPresets(SoundFont soundFont, string directory)
    {
        foreach (var preset in soundFont.Presets)
        {
            Console.WriteLine(preset.Name);

            var regions = preset.Regions
                .OrderBy(x => x.KeyRangeStart)
                .ThenBy(x => x.VelocityRangeStart)
                .ThenBy(x => x.Instrument.Name);

            var filename = preset.Name.Replace('/', ' ').Replace(':', ' ');
            var path = Path.Combine(directory, filename + ".csv");
            using (var writer = new StreamWriter(path))
            {
                foreach (var region in regions)
                {
                    writer.Write("," + region.Instrument.Name);
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

                writer.Write("Pan [-100;100]");
                foreach (var region in regions)
                {
                    writer.Write("," + Math.Round(region.Pan));
                }
                writer.WriteLine();

                writer.Write("Loop playback");
                foreach (var region in regions)
                {
                    writer.Write("," + region.SampleModes);
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

                writer.Write("Scale tuing");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ScaleTuning);
                }
                writer.WriteLine();

                writer.Write("\"Filter, cutoff (x)\"");
                foreach (var region in regions)
                {
                    writer.Write("," + region.InitialFilterCutoffFrequency.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("\"Filter, resonance (dB)\"");
                foreach (var region in regions)
                {
                    writer.Write("," + region.InitialFilterQ.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Vol env delay (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vol env attack (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.AttackVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vol env hold (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.HoldVolumeEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vol env decay (x)");
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

                writer.Write("Vol env release (x)");
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

                writer.Write("Mod env delay (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env attack (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.AttackModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env hold (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.HoldModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env decay (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DecayModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env sustain (%)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.SustainModulationEnvelope.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Mod env release (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ReleaseModulationEnvelope.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod env -> pitch (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationEnvelopeToPitch);
                }
                writer.WriteLine();

                writer.Write("Mod env -> filter (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationEnvelopeToFilterCutoffFrequency);
                }
                writer.WriteLine();

                writer.Write("Key -> Mod env hold (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.KeyNumberToModulationEnvelopeHold);
                }
                writer.WriteLine();

                writer.Write("Key -> Mod env decay (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.KeyNumberToModulationEnvelopeDecay);
                }
                writer.WriteLine();

                writer.Write("Mod LFO delay (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayModulationLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod LFO freq (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.FrequencyModulationLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Mod LFO -> pitch (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationLfoToPitch);
                }
                writer.WriteLine();

                writer.Write("Mod LFO -> filter (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationLfoToFilterCutoffFrequency);
                }
                writer.WriteLine();

                writer.Write("Mod LFO -> volume (dB)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ModulationLfoToVolume.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Vib LFO delay (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.DelayVibratoLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vib LFO freq (x)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.FrequencyVibratoLfo.ToString("0.000"));
                }
                writer.WriteLine();

                writer.Write("Vib LFO -> pitch (c)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.VibratoLfoToPitch);
                }
                writer.WriteLine();

                writer.Write("Chorus (%)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ChorusEffectsSend.ToString("0.0"));
                }
                writer.WriteLine();

                writer.Write("Reverb (%)");
                foreach (var region in regions)
                {
                    writer.Write("," + region.ReverbEffectsSend.ToString("0.0"));
                }
                writer.WriteLine();
            }
        }
    }
}
