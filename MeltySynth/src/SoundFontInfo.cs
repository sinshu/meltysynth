using System;
using System.IO;

namespace MeltySynth
{
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
            var chunkId = reader.ReadFixedLengthString(4);
            if (chunkId != "LIST")
            {
                throw new InvalidDataException("The LIST chunk was not found.");
            }

            var end = reader.BaseStream.Position + reader.ReadInt32();

            var listType = reader.ReadFixedLengthString(4);
            if (listType != "INFO")
            {
                throw new InvalidDataException($"The type of the LIST chunk must be 'INFO', but was '{listType}'.");
            }

            while (reader.BaseStream.Position < end)
            {
                var id = reader.ReadFixedLengthString(4);
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

        public override string ToString()
        {
            return bankName;
        }

        public SoundFontVersion Version => version;
        public string TargetSoundEngine => targetSoundEngine;
        public string BankName => bankName;
        public string RomName => romName;
        public SoundFontVersion RomVersion => romVersion;
        public string CeationDate => creationDate;
        public string Author => author;
        public string TargetProduct => targetProduct;
        public string Copyright => copyright;
        public string Comments => comments;
        public string Tools => tools;
    }
}
