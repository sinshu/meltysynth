using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MeltySynth
{
    /// <summary>
    /// Represents a standard MIDI file.
    /// </summary>
    public sealed class MidiFile
    {
        private Message[] messages;
        private TimeSpan[] times;

        /// <summary>
        /// Loads a MIDI file from the stream.
        /// </summary>
        /// <param name="stream">The data stream used to load the MIDI file.</param>
        public MidiFile(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Load(stream, 0, MidiFileLoopType.None);

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>
        /// Loads a MIDI file from the stream.
        /// </summary>
        /// <param name="stream">The data stream used to load the MIDI file.</param>
        /// <param name="loopPoint">The loop start point in ticks.</param>
        public MidiFile(Stream stream, int loopPoint)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (loopPoint < 0)
            {
                throw new ArgumentException("The loop point must be a non-negative value.", nameof(loopPoint));
            }

            Load(stream, loopPoint, MidiFileLoopType.None);

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>
        /// Loads a MIDI file from the stream.
        /// </summary>
        /// <param name="stream">The data stream used to load the MIDI file.</param>
        /// <param name="loopType">The type of loop extension to use.</param>
        public MidiFile(Stream stream, MidiFileLoopType loopType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Load(stream, 0, loopType);

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>
        /// Loads a MIDI file from the file.
        /// </summary>
        /// <param name="path">The MIDI file name and path.</param>
        public MidiFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, 0, MidiFileLoopType.None);
            }

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>
        /// Loads a MIDI file from the file.
        /// </summary>
        /// <param name="path">The MIDI file name and path.</param>
        /// <param name="loopPoint">The loop start point in ticks.</param>
        public MidiFile(string path, int loopPoint)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (loopPoint < 0)
            {
                throw new ArgumentException("The loop point must be a non-negative value.", nameof(loopPoint));
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, loopPoint, MidiFileLoopType.None);
            }

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>
        /// Loads a MIDI file from the file.
        /// </summary>
        /// <param name="path">The MIDI file name and path.</param>
        /// <param name="loopType">The type of loop extension to use.</param>
        public MidiFile(string path, MidiFileLoopType loopType)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, 0, loopType);
            }

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        // Some .NET implementations round TimeSpan to the nearest millisecond,
        // and the timing of MIDI messages will be wrong.
        // This method makes TimeSpan without rounding.
        internal static TimeSpan GetTimeSpanFromSeconds(double value)
        {
            return new TimeSpan((long)(TimeSpan.TicksPerSecond * value));
        }

        private void Load(Stream stream, int loopPoint, MidiFileLoopType loopType)
        {
            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var chunkType = reader.ReadFourCC();
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

                var trackCount = reader.ReadInt16BigEndian();
                var resolution = reader.ReadInt16BigEndian();

                var messageLists = new List<Message>[trackCount];
                var tickLists = new List<int>[trackCount];
                for (var i = 0; i < trackCount; i++)
                {
                    (messageLists[i], tickLists[i]) = ReadTrack(reader, loopType);
                }

                if (loopPoint != 0)
                {
                    var tickList = tickLists[0];
                    var messageList = messageLists[0];
                    if (loopPoint <= tickList.Last())
                    {
                        for (var i = 0; i < tickList.Count; i++)
                        {
                            if (tickList[i] >= loopPoint)
                            {
                                tickList.Insert(i, loopPoint);
                                messageList.Insert(i, Message.LoopStart());
                                break;
                            }
                        }
                    }
                    else
                    {
                        tickList.Add(loopPoint);
                        messageList.Add(Message.LoopStart());
                    }
                }

                (messages, times) = MergeTracks(messageLists, tickLists, resolution);
            }
        }

        private static (List<Message>, List<int>) ReadTrack(BinaryReader reader, MidiFileLoopType loopType)
        {
            var chunkType = reader.ReadFourCC();
            if (chunkType != "MTrk")
            {
                throw new InvalidDataException($"The chunk type must be 'MTrk', but was '{chunkType}'.");
            }

            var end = (long)reader.ReadInt32BigEndian();
            end += reader.BaseStream.Position;

            var messages = new List<Message>();
            var ticks = new List<int>();

            int tick = 0;
            byte lastStatus = 0;

            while (true)
            {
                var delta = reader.ReadIntVariableLength();
                var first = reader.ReadByte();

                try
                {
                    tick = checked(tick + delta);
                }
                catch (OverflowException)
                {
                    throw new NotSupportedException("Long MIDI file is not supported.");
                }

                if ((first & 128) == 0)
                {
                    var command = lastStatus & 0xF0;
                    if (command == 0xC0 || command == 0xD0)
                    {
                        messages.Add(Message.Common(lastStatus, first));
                        ticks.Add(tick);
                    }
                    else
                    {
                        var data2 = reader.ReadByte();
                        messages.Add(Message.Common(lastStatus, first, data2, loopType));
                        ticks.Add(tick);
                    }

                    continue;
                }

                switch (first)
                {
                    case 0xF0: // System Exclusive
                        DiscardData(reader);
                        break;

                    case 0xF7: // System Exclusive
                        DiscardData(reader);
                        break;

                    case 0xFF: // Meta Event
                        switch (reader.ReadByte())
                        {
                            case 0x2F: // End of Track
                                reader.ReadByte();
                                messages.Add(Message.EndOfTrack());
                                ticks.Add(tick);

                                // Some MIDI files may have events inserted after the EOT.
                                // Such events should be ignored.
                                if (reader.BaseStream.Position < end)
                                {
                                    reader.BaseStream.Position = end;
                                }

                                return (messages, ticks);

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
                            messages.Add(Message.Common(first, data1));
                            ticks.Add(tick);
                        }
                        else
                        {
                            var data1 = reader.ReadByte();
                            var data2 = reader.ReadByte();
                            messages.Add(Message.Common(first, data1, data2, loopType));
                            ticks.Add(tick);
                        }
                        break;
                }

                lastStatus = first;
            }
        }

        private static (Message[], TimeSpan[]) MergeTracks(List<Message>[] messageLists, List<int>[] tickLists, int resolution)
        {
            var mergedMessages = new List<Message>();
            var mergedTimes = new List<TimeSpan>();

            var indices = new int[messageLists.Length];

            var currentTick = 0;
            var currentTime = TimeSpan.Zero;

            var tempo = 120.0;

            while (true)
            {
                var minTick = int.MaxValue;
                var minIndex = -1;
                for (var ch = 0; ch < tickLists.Length; ch++)
                {
                    if (indices[ch] < tickLists[ch].Count)
                    {
                        var tick = tickLists[ch][indices[ch]];
                        if (tick < minTick)
                        {
                            minTick = tick;
                            minIndex = ch;
                        }
                    }
                }

                if (minIndex == -1)
                {
                    break;
                }

                var nextTick = tickLists[minIndex][indices[minIndex]];
                var deltaTick = nextTick - currentTick;
                var deltaTime = GetTimeSpanFromSeconds(60.0 / (resolution * tempo) * deltaTick);

                currentTick += deltaTick;
                currentTime += deltaTime;

                var message = messageLists[minIndex][indices[minIndex]];
                if (message.Type == MessageType.TempoChange)
                {
                    tempo = message.Tempo;
                }
                else
                {
                    mergedMessages.Add(message);
                    mergedTimes.Add(currentTime);
                }

                indices[minIndex]++;
            }

            return (mergedMessages.ToArray(), mergedTimes.ToArray());
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

        /// <summary>
        /// The length of the MIDI file.
        /// </summary>
        public TimeSpan Length => times.Last();

        internal Message[] Messages => messages;
        internal TimeSpan[] Times => times;



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

            public static Message Common(byte status, byte data1)
            {
                byte channel = (byte)(status & 0x0F);
                byte command = (byte)(status & 0xF0);
                byte data2 = 0;
                return new Message(channel, command, data1, data2);
            }

            public static Message Common(byte status, byte data1, byte data2, MidiFileLoopType loopType)
            {
                byte channel = (byte)(status & 0x0F);
                byte command = (byte)(status & 0xF0);

                if (command == 0xB0)
                {
                    switch (loopType)
                    {
                        case MidiFileLoopType.RpgMaker:
                            if (data1 == 111)
                            {
                                return LoopStart();
                            }
                            break;

                        case MidiFileLoopType.IncredibleMachine:
                            if (data1 == 110)
                            {
                                return LoopStart();
                            }
                            if (data1 == 111)
                            {
                                return LoopEnd();
                            }
                            break;

                        case MidiFileLoopType.FinalFantasy:
                            if (data1 == 116)
                            {
                                return LoopStart();
                            }
                            if (data1 == 117)
                            {
                                return LoopEnd();
                            }
                            break;
                    }
                }

                return new Message(channel, command, data1, data2);
            }

            public static Message TempoChange(int tempo)
            {
                byte command = (byte)(tempo >> 16);
                byte data1 = (byte)(tempo >> 8);
                byte data2 = (byte)(tempo);
                return new Message((int)MessageType.TempoChange, command, data1, data2);
            }

            public static Message LoopStart()
            {
                return new Message((int)MessageType.LoopStart, 0, 0, 0);
            }

            public static Message LoopEnd()
            {
                return new Message((int)MessageType.LoopEnd, 0, 0, 0);
            }

            public static Message EndOfTrack()
            {
                return new Message((int)MessageType.EndOfTrack, 0, 0, 0);
            }

            public override string ToString()
            {
                switch (channel)
                {
                    case (int)MessageType.TempoChange:
                        return "Tempo: " + Tempo;

                    case (int)MessageType.LoopStart:
                        return "LoopStart";

                    case (int)MessageType.LoopEnd:
                        return "LoopEnd";

                    case (int)MessageType.EndOfTrack:
                        return "EndOfTrack";

                    default:
                        return "CH" + channel + ": " + command.ToString("X2") + ", " + data1.ToString("X2") + ", " + data2.ToString("X2");
                }
            }

            public MessageType Type
            {
                get
                {
                    switch (channel)
                    {
                        case (int)MessageType.TempoChange:
                            return MessageType.TempoChange;

                        case (int)MessageType.LoopStart:
                            return MessageType.LoopStart;

                        case (int)MessageType.LoopEnd:
                            return MessageType.LoopEnd;

                        case (int)MessageType.EndOfTrack:
                            return MessageType.EndOfTrack;

                        default:
                            return MessageType.Normal;
                    }
                }
            }

            public byte Channel => channel;
            public byte Command => command;
            public byte Data1 => data1;
            public byte Data2 => data2;

            public double Tempo => 60000000.0 / ((command << 16) | (data1 << 8) | data2);
        }



        internal enum MessageType
        {
            Normal = 0,
            TempoChange = 252,
            LoopStart = 253,
            LoopEnd = 254,
            EndOfTrack = 255
        }
    }
}
