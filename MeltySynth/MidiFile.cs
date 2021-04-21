using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MeltySynth
{
    public sealed class MidiFile
    {
        private int trackCount;
        private int resolution;

        private Message[] messages;
        private long[] ticks;

        public MidiFile(Stream stream)
        {
            Load(stream);
        }

        public MidiFile(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream);
            }
        }

        private void Load(Stream stream)
        {
            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var chunkType = reader.ReadFixedLengthString(4);
                if (chunkType != "MThd")
                {
                    throw new InvalidDataException($"The chunk type must be 'MThd', but was '{chunkType}'.");
                }

                var size = reader.ReadInt32BigEndian();
                if (size != 6)
                {
                    throw new InvalidDataException($"The MThd chunk has invalid data.");
                }

                var format = reader.ReadInt16BigEndian();
                if (!(format == 0 || format == 1))
                {
                    throw new NotSupportedException($"The format {format} is not supported.");
                }

                trackCount = reader.ReadInt16BigEndian();
                resolution = reader.ReadInt16BigEndian();

                var messageLists = new List<Message>[trackCount];
                var tickLists = new List<long>[trackCount];
                for (var i = 0; i < trackCount; i++)
                {
                    List<Message> messageList;
                    List<long> tickList;
                    ReadTrack(reader, out messageList, out tickList);
                    messageLists[i] = messageList;
                    tickLists[i] = tickList;
                }

                CombineTracks(messageLists, tickLists, out messages, out ticks);
            }
        }

        private static void ReadTrack(BinaryReader reader, out List<Message> messages, out List<long> ticks)
        {
            var chunkType = reader.ReadFixedLengthString(4);
            if (chunkType != "MTrk")
            {
                throw new InvalidDataException($"The chunk type must be 'MTrk', but was '{chunkType}'.");
            }

            reader.ReadInt32BigEndian();

            messages = new List<Message>();
            ticks = new List<long>();

            long tick = 0;
            byte lastStatus = 0;

            while (true)
            {
                var delta = reader.ReadIntVariableLength();
                var first = reader.ReadByte();

                tick += delta;

                if ((first & 128) == 0)
                {
                    var command = lastStatus & 0xF0;
                    if (command == 0xC0 || command == 0xD0)
                    {
                        messages.Add(Message.Normal(lastStatus, first));
                        ticks.Add(tick);
                    }
                    else
                    {
                        var data2 = reader.ReadByte();
                        messages.Add(Message.Normal(lastStatus, first, data2));
                        ticks.Add(tick);
                    }

                    continue;
                }

                switch (first)
                {
                    case 0xF0: // System exclusive
                        DiscardData(reader);
                        break;

                    case 0xF7: // System exclusive
                        DiscardData(reader);
                        break;

                    case 0xFF: // Meta event
                        switch (reader.ReadByte())
                        {
                            case 0x2F: // End of track
                                reader.ReadByte();
                                ticks.Add(tick);
                                return;

                            case 0x51: // Tempo
                                messages.Add(Message.TempoChange(ReadTempo(reader)));
                                ticks.Add(tick);
                                break;

                            default:
                                DiscardData(reader);
                                break;
                        }
                        break;

                    default:
                        var command = first & 0xF0;
                        if (command == 0xC0 || command == 0xD0)
                        {
                            var data1 = reader.ReadByte();
                            messages.Add(Message.Normal(first, data1));
                            ticks.Add(tick);
                        }
                        else
                        {
                            var data1 = reader.ReadByte();
                            var data2 = reader.ReadByte();
                            messages.Add(Message.Normal(first, data1, data2));
                            ticks.Add(tick);
                        }
                        break;
                }

                lastStatus = first;
            }
        }

        private static void CombineTracks(List<Message>[] messageLists, List<long>[] tickLists, out Message[] messages, out long[] ticks)
        {
            var messageCount = 0;
            foreach (var list in messageLists)
            {
                messageCount += list.Count;
            }

            messages = new Message[messageCount];
            ticks = new long[messageCount + 1];

            var indices = new int[messageLists.Length];

            for (var i = 0; i < messages.Length; i++)
            {
                long minTick = long.MaxValue;
                int minIndex = -1;
                for (var j = 0; j < tickLists.Length; j++)
                {
                    if (indices[j] < messageLists[j].Count)
                    {
                        var tick = tickLists[j][indices[j]];
                        if (tick < minTick)
                        {
                            minTick = tick;
                            minIndex = j;
                        }
                    }
                }

                messages[i] = messageLists[minIndex][indices[minIndex]];
                ticks[i] = tickLists[minIndex][indices[minIndex]];
                indices[minIndex]++;
            }

            var lastTick = long.MinValue;
            for (var j = 0; j < tickLists.Length; j++)
            {
                if (tickLists[j].Last() > lastTick)
                {
                    lastTick = tickLists[j].Last();
                }
            }

            ticks[messageCount] = lastTick;
        }

        private static int ReadTempo(BinaryReader reader)
        {
            var size = reader.ReadIntVariableLength();
            if (size != 3)
            {
                throw new InvalidDataException("Failed to read the tempo value.");
            }

            var b1 = reader.ReadByte();
            var b2 = reader.ReadByte();
            var b3 = reader.ReadByte();
            return (b1 << 16) | (b2 << 8) | b3;
        }

        private static void DiscardData(BinaryReader reader)
        {
            var size = reader.ReadIntVariableLength();
            reader.BaseStream.Position += size;
        }

        internal int TrackCount => trackCount;
        internal int Resolution => resolution;
        internal Message[] Messages => messages;
        internal long[] Ticks => ticks;



        internal struct Message
        {
            private byte channel;
            private byte command;
            private byte data1;
            private byte data2;

            private Message(byte channel, byte command, byte data1, byte data2)
            {
                this.channel = channel;
                this.command = command;
                this.data1 = data1;
                this.data2 = data2;
            }

            public static Message Normal(byte status, byte data1)
            {
                byte channel = (byte)(status & 0x0F);
                byte command = (byte)(status & 0xF0);
                byte data2 = 0;
                return new Message(channel, command, data1, data2);
            }

            public static Message Normal(byte status, byte data1, byte data2)
            {
                byte channel = (byte)(status & 0x0F);
                byte command = (byte)(status & 0xF0);
                return new Message(channel, command, data1, data2);
            }

            public static Message TempoChange(int tempo)
            {
                byte channel = 0xFF;
                byte command = (byte)(tempo >> 16);
                byte data1 = (byte)(tempo >> 8);
                byte data2 = (byte)(tempo);
                return new Message(channel, command, data1, data2);
            }

            public override string ToString()
            {
                if (channel != 0xFF)
                {
                    return "CH" + channel + ": " + command.ToString("X2") + ", " + data1.ToString("X2") + ", " + data2.ToString("X2");
                }
                else
                {
                    return "Tempo: " + Tempo;
                }
            }

            public MessageType Type => channel != 0xFF ? MessageType.Normal : MessageType.TempoChange;

            public byte Channel => channel;
            public byte Command => command;
            public byte Data1 => data1;
            public byte Data2 => data2;

            public int Tempo => (command << 16) | (data1 << 8) | data2;
        }



        internal enum MessageType
        {
            Normal,
            TempoChange
        }
    }
}
