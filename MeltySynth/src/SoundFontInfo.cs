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

        internal SoundFontInfo(BinaryReader reader)
        {
            var chunkId = reader.ReadFourCC();
            if (chunkId != "LIST")
            {
                throw new InvalidDataException("The LIST chunk was not found.");
            }

            var end = reader.BaseStream.Position + reader.ReadInt32();

            var listType = reader.ReadFourCC();
            if (listType != "INFO")
            {
                throw new InvalidDataException($"The type of the LIST chunk must be 'INFO', but was '{listType}'.");
            }

            while (reader.BaseStream.Position < end)
            {
                var id = reader.ReadFourCC();
                var size = reader.ReadInt32();

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
