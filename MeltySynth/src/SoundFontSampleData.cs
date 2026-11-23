using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MeltySynth
{
    internal sealed class SoundFontSampleData
    {
        private readonly int bitsPerSample;
        private readonly short[] samples;

        internal SoundFontSampleData(BinaryReader reader, long containerEnd, AssetLoadLimits? limits = null)
        {
            limits = limits ?? AssetLoadLimits.Default;
            var stream = reader.BaseStream;
            if (stream.Length - stream.Position < 12)
            {
                throw new EndOfStreamException("The SoundFont sample LIST header is incomplete.");
            }
            if (containerEnd - stream.Position < 12)
            {
                throw new InvalidDataException("The sample LIST header extends beyond its RIFF container.");
            }
            var chunkId = reader.ReadFourCC();
            if (chunkId != "LIST")
            {
                throw new InvalidDataException("The LIST chunk was not found.");
            }

            var size = reader.ReadUInt32();
            if (size < 4)
            {
                throw new InvalidDataException("The sample LIST is too small to contain its type.");
            }
            var end = stream.Position + size;
            if (end > stream.Length)
            {
                throw new EndOfStreamException("The SoundFont sample LIST is truncated.");
            }
            if (end > containerEnd)
            {
                throw new InvalidDataException("The sample LIST extends beyond its RIFF container.");
            }

            var listType = reader.ReadFourCC();
            if (listType != "sdta")
            {
                throw new InvalidDataException($"The type of the LIST chunk must be 'sdta', but was '{listType}'.");
            }

            short[]? loadedSamples = null;
            while (stream.Position < end)
            {
                if (end - stream.Position < 8)
                {
                    throw new InvalidDataException("The sample LIST contains an incomplete chunk header.");
                }
                var id = reader.ReadFourCC();
                var length = reader.ReadUInt32();
                var paddedLength = (long)length + (length & 1);
                if (paddedLength > end - stream.Position)
                {
                    throw new InvalidDataException($"The '{id}' chunk extends beyond the sample LIST.");
                }

                switch (id)
                {
                    case "smpl":
                        if (loadedSamples != null)
                        {
                            throw new InvalidDataException("The sample LIST contains duplicate smpl chunks.");
                        }
                        if (length == 0 || (length & 1) != 0 || length > limits.MaxDecodedSampleBytes)
                        {
                            throw new InvalidDataException("The smpl chunk must contain a nonempty, supported-length sequence of 16-bit samples.");
                        }
                        loadedSamples = new short[(int)length / 2];
                        var remaining = MemoryMarshal.Cast<short, byte>(loadedSamples.AsSpan());
                        while (!remaining.IsEmpty)
                        {
                            var read = reader.Read(remaining);
                            if (read == 0)
                            {
                                throw new EndOfStreamException("The SoundFont sample data is incomplete.");
                            }
                            remaining = remaining.Slice(read);
                        }
                        break;
                    case "sm24":
                        // 24-bit data is unsupported; its bounded RIFF padding still applies.
                        stream.Position += length;
                        break;
                    default:
                        throw new InvalidDataException($"The sdta list contains an unknown ID '{id}'.");
                }
                if ((length & 1) != 0) reader.ReadByte();
            }

            if (loadedSamples == null)
            {
                throw new InvalidDataException("No valid sample data was found.");
            }

            if (loadedSamples.Length >= 2 &&
                Encoding.ASCII.GetString(MemoryMarshal.Cast<short, byte>(loadedSamples.AsSpan()).Slice(0, 4)) == "OggS")
            {
                throw new NotSupportedException("SoundFont3 is not yet supported.");
            }

            if (!BitConverter.IsLittleEndian)
            {
                // TODO: Insert the byte swapping code here.
                throw new NotSupportedException("Big endian architectures are not yet supported.");
            }

            bitsPerSample = 16;
            samples = loadedSamples;
        }

        public int BitsPerSample => bitsPerSample;
        public short[] Samples => samples;
    }
}
