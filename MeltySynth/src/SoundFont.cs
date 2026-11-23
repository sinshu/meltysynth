using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MeltySynth
{
    /// <summary>
    /// Reperesents a SoundFont.
    /// </summary>
    public sealed class SoundFont
    {
        private SoundFontInfo info;
        private int bitsPerSample;
        private short[] waveData;
        private SampleHeader[] sampleHeaders;
        private Preset[] presets;
        private Instrument[] instruments;

        /// <summary>
        /// Loads a SoundFont from the stream.
        /// </summary>
        /// <param name="stream">
        /// The data stream used to load the SoundFont.
        /// </param>
        public SoundFont(Stream stream) : this(stream, AssetLoadLimits.Default) { }

        /// <summary>Loads a SoundFont with application-specific asset budgets, leaving the stream open.</summary>
        /// <param name="stream">Readable, seekable asset stream.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public SoundFont(Stream stream, AssetLoadLimits limits)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Load(stream, limits);

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(info != null);
            Debug.Assert(waveData != null);
            Debug.Assert(sampleHeaders != null);
            Debug.Assert(presets != null);
            Debug.Assert(instruments != null);
        }

        /// <summary>
        /// Loads a SoundFont from the file.
        /// </summary>
        /// <param name="path">
        /// The SoundFont file name and path.
        /// </param>
        public SoundFont(string path) : this(path, AssetLoadLimits.Default) { }

        /// <summary>Loads a SoundFont file with application-specific asset budgets.</summary>
        /// <param name="path">SoundFont file path.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public SoundFont(string path, AssetLoadLimits limits)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, limits);
            }

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(info != null);
            Debug.Assert(waveData != null);
            Debug.Assert(sampleHeaders != null);
            Debug.Assert(presets != null);
            Debug.Assert(instruments != null);
        }

        private void Load(Stream stream, AssetLoadLimits limits)
        {
            AssetReader.Validate(stream, limits);
            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var chunkId = reader.ReadFourCC();
                if (chunkId != "RIFF")
                {
                    throw new InvalidDataException("The RIFF chunk was not found.");
                }

                var size = reader.ReadUInt32();
                if (size < 4)
                {
                    throw new InvalidDataException("The RIFF chunk is too small to contain its form type.");
                }
                var riffEnd = stream.Position + size;
                if (riffEnd > stream.Length)
                {
                    throw new EndOfStreamException("The SoundFont RIFF chunk is truncated.");
                }

                if (riffEnd != stream.Length)
                    throw new InvalidDataException("Unexpected bytes follow the SoundFont RIFF container.");

                var formType = reader.ReadFourCC();
                if (formType != "sfbk")
                {
                    throw new InvalidDataException($"The type of the RIFF chunk must be 'sfbk', but was '{formType}'.");
                }

                info = new SoundFontInfo(reader, riffEnd, limits);

                var sampleData = new SoundFontSampleData(reader, riffEnd, limits);
                bitsPerSample = sampleData.BitsPerSample;
                waveData = sampleData.Samples;

                var parameters = new SoundFontParameters(reader, riffEnd, limits);
                sampleHeaders = parameters.SampleHeaders;
                presets = parameters.Presets;
                instruments = parameters.Instruments;
                if (stream.Position != riffEnd)
                    throw new InvalidDataException("Unexpected data remains inside the SoundFont RIFF container.");
            }

            CheckSamples();
            CheckRegions();
        }

        /// <summary>
        /// Gets the name of the SoundFont.
        /// </summary>
        /// <returns>
        /// The name of the SoundFont.
        /// </returns>
        public override string ToString()
        {
            return info.BankName;
        }

        private void CheckSamples()
        {
            var sampleCount = waveData.Length - 4; // This offset is to ensure that out of range access is safe.

            foreach (var sample in sampleHeaders)
            {
                if (!(0 <= sample.Start && sample.Start < sampleCount))
                {
                    throw new InvalidDataException($"The start position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.StartLoop && sample.StartLoop < sampleCount))
                {
                    throw new InvalidDataException($"The loop start position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 < sample.End && sample.End <= sampleCount))
                {
                    throw new InvalidDataException($"The end position of the sample '{sample.Name}' is out of range.");
                }
                if (!(0 <= sample.EndLoop && sample.EndLoop <= sampleCount))
                {
                    throw new InvalidDataException($"The loop end position of the sample '{sample.Name}' is out of range.");
                }
                if (sample.SampleRate <= 0 || (sample.OriginalPitch > 127 && sample.OriginalPitch != 255))
                    throw new InvalidDataException($"The sample '{sample.Name}' has invalid pitch/rate metadata.");
                if (sample.Start >= sample.End)
                {
                    throw new InvalidDataException($"The sample '{sample.Name}' must have a positive length.");
                }
            }
        }

        private void CheckRegions()
        {
            var sampleCount = waveData.Length - 4; // This offset is to ensure that out of range access is safe.

            foreach (var instrument in instruments)
            {
                foreach (var region in instrument.RegionArray)
                {
                    if (!(0 <= region.SampleStart && region.SampleStart < sampleCount))
                    {
                        throw new InvalidDataException($"The start position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }
                    if (!(0 <= region.SampleStartLoop && region.SampleStartLoop < sampleCount))
                    {
                        throw new InvalidDataException($"The loop start position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }
                    if (!(0 < region.SampleEnd && region.SampleEnd <= sampleCount))
                    {
                        throw new InvalidDataException($"The end position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }
                    if (!(0 <= region.SampleEndLoop && region.SampleEndLoop <= sampleCount))
                    {
                        throw new InvalidDataException($"The loop end position of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' is out of range.");
                    }

                    if (region.SampleStart >= region.SampleEnd)
                    {
                        throw new InvalidDataException($"The sample '{region.Sample.Name}' in the instrument '{instrument.Name}' must have a positive length.");
                    }

                    switch (region.SampleModes)
                    {
                        case LoopMode.NoLoop:
                            break;
                        case LoopMode.Continuous:
                        case LoopMode.LoopUntilNoteOff:
                            if (!(region.SampleStart <= region.SampleStartLoop &&
                                  region.SampleStartLoop < region.SampleEndLoop &&
                                  region.SampleEndLoop <= region.SampleEnd))
                            {
                                throw new InvalidDataException($"The loop of the sample '{region.Sample.Name}' in the instrument '{instrument.Name}' must have a positive length within the sample.");
                            }
                            break;
                        default:
                            throw new InvalidDataException($"The sample '{region.Sample.Name}' in the instrument '{instrument.Name}' has an invalid loop mode.");
                    }
                }
            }
        }

        /// <summary>
        /// The information of the SoundFont.
        /// </summary>
        public SoundFontInfo Info => info;

        /// <summary>
        /// The bits per sample of the sample data.
        /// </summary>
        /// <remarks>
        /// This value is always 16.
        /// </remarks>
        public int BitsPerSample => bitsPerSample;

        /// <summary>
        /// The sample data.
        /// </summary>
        /// <remarks>
        /// This single array contains all the waveform data in the SoundFont.
        /// An instance of <see cref="SampleHeader"/> indicates a range of the array corresponding to a sample.
        /// </remarks>
        public ReadOnlySpan<short> WaveData => waveData;

        /// <summary>
        /// The samples of the SoundFont.
        /// </summary>
        public IReadOnlyList<SampleHeader> SampleHeaders => sampleHeaders;

        /// <summary>
        /// The presets of the SoundFont.
        /// </summary>
        public IReadOnlyList<Preset> Presets => presets;

        /// <summary>
        /// The instruments of the SoundFont.
        /// </summary>
        public IReadOnlyList<Instrument> Instruments => instruments;

        // Internally exposes the raw arrays for fast enumeration.
        internal short[] WaveDataArray => waveData;
        internal SampleHeader[] SampleHeaderArray => sampleHeaders;
        internal Preset[] PresetArray => presets;
        internal Instrument[] InstrumentArray => instruments;
    }
}
