using System;
using System.IO;

namespace MeltySynth
{
    /// <summary>
    /// The information of a SoundFont.
    /// </summary>
    public sealed class SoundFontInfo
    {
        private readonly SoundFontVersion version = default;
        private readonly string targetSoundEngine = string.Empty;
        private readonly string bankName = string.Empty;
        private readonly string romName = string.Empty;
        private readonly SoundFontVersion romVersion = default;
        private readonly string creationDate = string.Empty;
        private readonly string author = string.Empty;
        private readonly string targetProduct = string.Empty;
        private readonly string copyright = string.Empty;
        private readonly string comments = string.Empty;
        private readonly string tools = string.Empty;

        internal SoundFontInfo(BinaryReader reader, long containerEnd, AssetLoadLimits limits)
        {
            var end = AssetReader.List(reader, containerEnd, "INFO");
            if (end - reader.BaseStream.Position > limits.MaxMetadataBytes)
                throw new InvalidDataException("SoundFont INFO exceeds the metadata byte budget.");
            var seen = new System.Collections.Generic.HashSet<string>();
            while (reader.BaseStream.Position < end)
            {
                var chunk = AssetReader.Chunk(reader, end);
                var id = chunk.Id;
                var size = chunk.Size;
                if (!seen.Add(id)) throw new InvalidDataException("Duplicate SoundFont INFO chunk: " + id);
                if ((id == "ifil" || id == "iver") && size != 4)
                    throw new InvalidDataException("SoundFont version chunks must contain four bytes.");
                switch (id)
                {
                    case "ifil":
                        version = new SoundFontVersion(reader.ReadInt16(), reader.ReadInt16());
                        break;
                    case "isng":
                        targetSoundEngine = reader.ReadFixedLengthString(size);
                        break;
                    case "INAM":
                        bankName = reader.ReadFixedLengthString(size);
                        break;
                    case "irom":
                        romName = reader.ReadFixedLengthString(size);
                        break;
                    case "iver":
                        romVersion = new SoundFontVersion(reader.ReadInt16(), reader.ReadInt16());
                        break;
                    case "ICRD":
                        creationDate = reader.ReadFixedLengthString(size);
                        break;
                    case "IENG":
                        author = reader.ReadFixedLengthString(size);
                        break;
                    case "IPRD":
                        targetProduct = reader.ReadFixedLengthString(size);
                        break;
                    case "ICOP":
                        copyright = reader.ReadFixedLengthString(size);
                        break;
                    case "ICMT":
                        comments = reader.ReadFixedLengthString(size);
                        break;
                    case "ISFT":
                        tools = reader.ReadFixedLengthString(size);
                        break;
                    default:
                        throw new InvalidDataException($"The INFO list contains an unknown ID '{id}'.");
                }
                if (reader.BaseStream.Position != chunk.End)
                    throw new InvalidDataException("The INFO chunk was not consumed exactly.");
                if ((size & 1) != 0) reader.ReadByte();
            }
        }

        /// <summary>
        /// Gets the name of the SoundFont.
        /// </summary>
        /// <returns>
        /// The name of the SoundFont.
        /// </returns>
        public override string ToString()
        {
            return bankName;
        }

        /// <summary>
        /// The version of the SoundFont.
        /// </summary>
        public SoundFontVersion Version => version;

        /// <summary>
        /// The target sound engine of the SoundFont.
        /// </summary>
        public string TargetSoundEngine => targetSoundEngine;

        /// <summary>
        /// The bank name of the SoundFont.
        /// </summary>
        public string BankName => bankName;

        /// <summary>
        /// The ROM name of the SoundFont.
        /// </summary>
        public string RomName => romName;

        /// <summary>
        /// The ROM version of the SoundFont.
        /// </summary>
        public SoundFontVersion RomVersion => romVersion;

        /// <summary>
        /// The creation date of the SoundFont.
        /// </summary>
        public string CeationDate => creationDate;

        /// <summary>
        /// The auther of the SoundFont.
        /// </summary>
        public string Author => author;

        /// <summary>
        /// The target product of the SoundFont.
        /// </summary>
        public string TargetProduct => targetProduct;

        /// <summary>
        /// The copyright message for the SoundFont.
        /// </summary>
        public string Copyright => copyright;

        /// <summary>
        /// The comments for the SoundFont.
        /// </summary>
        public string Comments => comments;

        /// <summary>
        /// The tools used to create the SoundFont.
        /// </summary>
        public string Tools => tools;
    }
}
