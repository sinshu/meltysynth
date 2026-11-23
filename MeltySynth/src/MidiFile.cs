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
        private bool canLoop;

        /// <summary>Loads a MIDI asset using default loading budgets.</summary>
        /// <param name="stream">The MIDI asset stream.</param>
        public MidiFile(Stream stream) : this(stream, AssetLoadLimits.Default) { }

        /// <summary>Loads a MIDI asset using application-specific loading budgets.</summary>
        /// <param name="stream">The MIDI asset stream, which remains open.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public MidiFile(Stream stream, AssetLoadLimits limits)
        {
            Load(stream, 0, MidiFileLoopType.None, limits);
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>Loads a MIDI asset using default loading budgets.</summary>
        /// <param name="stream">The MIDI asset stream.</param>
        /// <param name="loopPoint">The loop start tick.</param>
        public MidiFile(Stream stream, int loopPoint) : this(stream, loopPoint, AssetLoadLimits.Default) { }

        /// <summary>Loads a MIDI asset using application-specific loading budgets.</summary>
        /// <param name="stream">The MIDI asset stream, which remains open.</param>
        /// <param name="loopPoint">The loop start tick.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public MidiFile(Stream stream, int loopPoint, AssetLoadLimits limits)
        {
            Load(stream, loopPoint, MidiFileLoopType.None, limits);
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>Loads a MIDI asset using default loading budgets.</summary>
        /// <param name="stream">The MIDI asset stream.</param>
        /// <param name="loopType">The loop marker interpretation.</param>
        public MidiFile(Stream stream, MidiFileLoopType loopType) : this(stream, loopType, AssetLoadLimits.Default) { }

        /// <summary>Loads a MIDI asset using application-specific loading budgets.</summary>
        /// <param name="stream">The MIDI asset stream, which remains open.</param>
        /// <param name="loopType">The loop marker interpretation.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public MidiFile(Stream stream, MidiFileLoopType loopType, AssetLoadLimits limits)
        {
            Load(stream, 0, loopType, limits);
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>Loads a MIDI asset using default loading budgets.</summary>
        /// <param name="path">The MIDI asset path.</param>
        public MidiFile(string path) : this(path, AssetLoadLimits.Default) { }

        /// <summary>Loads a MIDI asset using application-specific loading budgets.</summary>
        /// <param name="path">The MIDI asset path.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public MidiFile(string path, AssetLoadLimits limits)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, 0, MidiFileLoopType.None, limits);
            }
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>Loads a MIDI asset using default loading budgets.</summary>
        /// <param name="path">The MIDI asset path.</param>
        /// <param name="loopPoint">The loop start tick.</param>
        public MidiFile(string path, int loopPoint) : this(path, loopPoint, AssetLoadLimits.Default) { }

        /// <summary>Loads a MIDI asset using application-specific loading budgets.</summary>
        /// <param name="path">The MIDI asset path.</param>
        /// <param name="loopPoint">The loop start tick.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public MidiFile(string path, int loopPoint, AssetLoadLimits limits)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, loopPoint, MidiFileLoopType.None, limits);
            }
            Debug.Assert(messages != null);
            Debug.Assert(times != null);
        }

        /// <summary>Loads a MIDI asset using default loading budgets.</summary>
        /// <param name="path">The MIDI asset path.</param>
        /// <param name="loopType">The loop marker interpretation.</param>
        public MidiFile(string path, MidiFileLoopType loopType) : this(path, loopType, AssetLoadLimits.Default) { }

        /// <summary>Loads a MIDI asset using application-specific loading budgets.</summary>
        /// <param name="path">The MIDI asset path.</param>
        /// <param name="loopType">The loop marker interpretation.</param>
        /// <param name="limits">Immutable loading budgets.</param>
        public MidiFile(string path, MidiFileLoopType loopType, AssetLoadLimits limits)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, 0, loopType, limits);
            }
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

        private void Load(Stream stream, int loopPoint, MidiFileLoopType loopType, AssetLoadLimits limits)
        {
            AssetReader.Validate(stream, limits);
            if (loopPoint < 0) throw new ArgumentException("The loop point must be non-negative.", nameof(loopPoint));
            if (loopType < MidiFileLoopType.None || loopType > MidiFileLoopType.FinalFantasy)
                throw new ArgumentOutOfRangeException(nameof(loopType));
            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                AssetReader.Require(reader, 14, stream.Length);
                if (reader.ReadFourCC() != "MThd" || reader.ReadInt32BigEndian() != 6)
                    throw new InvalidDataException("The MThd chunk is invalid.");
                var format = reader.ReadInt16BigEndian();
                if (format != 0 && format != 1) throw new NotSupportedException("Only MIDI formats 0 and 1 are supported.");
                var trackCount = (ushort)reader.ReadInt16BigEndian();
                var resolution = reader.ReadInt16BigEndian();
                if (trackCount == 0 || trackCount > limits.MaxMidiTracks || (format == 0 && trackCount != 1))
                    throw new InvalidDataException("The MIDI track count exceeds its format or budget.");
                if (resolution < 0) throw new NotSupportedException("SMPTE MIDI timing is not supported.");
                if (resolution == 0) throw new InvalidDataException("MIDI tick resolution must be positive.");

                var messageLists = new List<Message>[trackCount];
                var tickLists = new List<int>[trackCount];
                var events = 0;
                for (var i = 0; i < trackCount; i++)
                    (messageLists[i], tickLists[i]) = ReadTrack(reader, loopType, limits, ref events);
                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Unexpected bytes follow the declared MIDI tracks.");

                if (loopPoint != 0)
                {
                    if (trackCount == 0 || loopPoint > tickLists.Max(track => track[track.Count - 1]))
                        throw new ArgumentException("The loop point exceeds the MIDI duration.", nameof(loopPoint));
                    if (events >= limits.MaxMidiEvents) throw new InvalidDataException("The loop marker exceeds the MIDI event budget.");
                    var tickList = tickLists[0];
                    var messageList = messageLists[0];
                    var index = tickList.FindIndex(tick => tick >= loopPoint);
                    if (index < 0) index = tickList.Count;
                    tickList.Insert(index, loopPoint);
                    messageList.Insert(index, Message.LoopStart());
                }

                (messages, times) = MergeTracks(messageLists, tickLists, resolution, limits);
                canLoop = HasProgressingLoop(messages, times);
            }
        }

        private static bool HasProgressingLoop(Message[] messages, TimeSpan[] times)
        {
            if (messages.Length == 0)
            {
                return false;
            }

            var loopIndex = 0;
            for (var i = 0; i < messages.Length; i++)
            {
                if (messages[i].Type == MessageType.LoopStart)
                {
                    loopIndex = i;
                }
                else if (messages[i].Type == MessageType.LoopEnd)
                {
                    // The first explicit end always rewinds; its tail is unreachable.
                    return times[i] > times[loopIndex];
                }
            }

            return times[times.Length - 1] > times[loopIndex];
        }

        private static byte TrackByte(BinaryReader reader, long end)
        {
            AssetReader.Require(reader, 1, end);
            return reader.ReadByte();
        }

        private static byte DataByte(BinaryReader reader, long end)
        {
            var value = TrackByte(reader, end);
            if (value > 127) throw new InvalidDataException("A MIDI data byte contains a status bit.");
            return value;
        }

        private static int VariableLength(BinaryReader reader, long end)
        {
            var value = 0;
            for (var i = 0; i < 4; i++)
            {
                var next = TrackByte(reader, end);
                value = (value << 7) | (next & 127);
                if (next < 128) return value;
            }
            throw new InvalidDataException("MIDI variable-length values may contain at most four bytes.");
        }

        private static void ReadZeroPadding(BinaryReader reader, long end)
        {
            Span<byte> buffer = stackalloc byte[256];
            while (reader.BaseStream.Position < end)
            {
                var count = (int)Math.Min(buffer.Length, end - reader.BaseStream.Position);
                var read = reader.Read(buffer.Slice(0, count));
                if (read == 0) throw new EndOfStreamException("MIDI end-of-track padding is truncated.");
                for (var i = 0; i < read; i++)
                    if (buffer[i] != 0) throw new InvalidDataException("Only zero-byte padding may follow MIDI end-of-track.");
            }
        }

        private static (List<Message>, List<int>) ReadTrack(BinaryReader reader, MidiFileLoopType loopType,
            AssetLoadLimits limits, ref int eventCount)
        {
            var stream = reader.BaseStream;
            AssetReader.Require(reader, 8, stream.Length);
            if (reader.ReadFourCC() != "MTrk") throw new InvalidDataException("The MTrk chunk was not found.");
            var size = (uint)reader.ReadInt32BigEndian();
            if (size > limits.MaxMidiTrackBytes) throw new InvalidDataException("The MIDI track exceeds its byte budget.");
            AssetReader.Require(reader, size, stream.Length);
            var end = stream.Position + size;
            var messages = new List<Message>();
            var ticks = new List<int>();
            var tick = 0;
            byte lastStatus = 0;
            while (stream.Position < end)
            {
                if (eventCount >= limits.MaxMidiEvents) throw new InvalidDataException("MIDI events exceed the aggregate event budget.");
                eventCount++;
                var delta = VariableLength(reader, end);
                if (delta > int.MaxValue - tick) throw new InvalidDataException("MIDI tick accumulation exceeds the supported range.");
                tick += delta;
                var first = TrackByte(reader, end);
                if (first < 0xF0)
                {
                    byte status, data1;
                    if (first < 128)
                    {
                        if (lastStatus == 0) throw new InvalidDataException("MIDI running status has no preceding channel status.");
                        status = lastStatus; data1 = first;
                    }
                    else { status = first; data1 = DataByte(reader, end); lastStatus = status; }
                    var command = status & 0xF0;
                    var message = command == 0xC0 || command == 0xD0
                        ? Message.Common(status, data1)
                        : Message.Common(status, data1, DataByte(reader, end), loopType);
                    messages.Add(message); ticks.Add(tick);
                    continue;
                }

                lastStatus = 0;
                if (first == 0xF0 || first == 0xF7)
                {
                    var length = VariableLength(reader, end);
                    AssetReader.Require(reader, length, end);
                    stream.Position += length;
                }
                else if (first == 0xFF)
                {
                    var type = DataByte(reader, end);
                    var length = VariableLength(reader, end);
                    AssetReader.Require(reader, length, end);
                    if (type == 0x2F)
                    {
                        if (length != 0) throw new InvalidDataException("MIDI end-of-track must have zero length.");
                        if (!limits.AllowMidiEndOfTrackPadding && stream.Position != end)
                            throw new InvalidDataException("Bytes after MIDI end-of-track are disabled by policy.");
                        if (stream.Position != end) ReadZeroPadding(reader, end);
                        messages.Add(Message.EndOfTrack()); ticks.Add(tick);
                        return (messages, ticks);
                    }
                    if (type == 0x51)
                    {
                        if (length != 3) throw new InvalidDataException("MIDI tempo must contain three bytes.");
                        var tempo = (TrackByte(reader, end) << 16) | (TrackByte(reader, end) << 8) | TrackByte(reader, end);
                        if (tempo == 0) throw new InvalidDataException("MIDI tempo must be positive.");
                        messages.Add(Message.TempoChange(tempo)); ticks.Add(tick);
                    }
                    else stream.Position += length;
                }
                else throw new InvalidDataException("Unsupported MIDI system status inside a track.");
            }
            throw new InvalidDataException("The MIDI track ended without an end-of-track event.");
        }

        private static (Message[], TimeSpan[]) MergeTracks(List<Message>[] messageLists, List<int>[] tickLists, int resolution, AssetLoadLimits limits)
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
                        if (minIndex == -1 || tick < minTick)
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

                if (deltaTime > limits.MaxMidiDuration - currentTime)
                    throw new InvalidDataException("The MIDI duration exceeds its budget.");
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

        /// <summary>
        /// The length of the MIDI file.
        /// </summary>
        public TimeSpan Length => times.Length == 0 ? TimeSpan.Zero : times[times.Length - 1];

        internal bool CanLoop => canLoop;
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
