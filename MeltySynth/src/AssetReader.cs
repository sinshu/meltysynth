using System;
using System.IO;

namespace MeltySynth
{
    // Load-time structural checks only; no buffers, wrappers or per-render validation.
    internal static class AssetReader
    {
        internal static void Validate(Stream stream, AssetLoadLimits limits)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            if (!stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("Asset loading requires a readable, seekable stream.", nameof(stream));
            var remaining = stream.Length - stream.Position;
            if (remaining < 0 || remaining > limits.MaxFileBytes)
                throw new InvalidDataException("The asset exceeds the file byte budget or has an invalid position.");
        }

        internal static void Require(BinaryReader reader, long count, long end)
        {
            if (count < 0 || count > end - reader.BaseStream.Position)
                throw new InvalidDataException("The asset field extends beyond its containing chunk.");
        }

        internal static (string Id, int Size, long End) Chunk(BinaryReader reader, long end)
        {
            Require(reader, 8, end);
            var id = reader.ReadFourCC();
            var size = reader.ReadUInt32();
            Require(reader, (long)size + (size & 1), end);
            if (size > int.MaxValue) throw new InvalidDataException("The chunk is too large for this loader.");
            return (id, (int)size, reader.BaseStream.Position + size);
        }

        internal static long List(BinaryReader reader, long end, string type)
        {
            var chunk = Chunk(reader, end);
            if (chunk.Id != "LIST" || chunk.Size < 4 || reader.ReadFourCC() != type)
                throw new InvalidDataException("The expected " + type + " LIST was not found.");
            return chunk.End;
        }
    }
}
