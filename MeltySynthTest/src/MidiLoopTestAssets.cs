using System;
using System.IO;
using System.Text;

namespace MeltySynthTest
{
    // Original in-memory fixtures: no downloaded SoundFont, sound device or host path.
    internal static class MidiLoopTestAssets
    {
        internal static byte[] Track(params (int Delta, byte[] Message)[] events)
        {
            using var stream = new MemoryStream();
            foreach (var e in events)
            {
                var value = (uint)e.Delta;
                var packed = value & 0x7F;
                while ((value >>= 7) != 0) packed = (packed << 8) | 0x80 | (value & 0x7F);
                while (true)
                {
                    stream.WriteByte((byte)packed);
                    if ((packed & 0x80) == 0) break;
                    packed >>= 8;
                }
                stream.Write(e.Message, 0, e.Message.Length);
            }
            return stream.ToArray();
        }

        internal static MeltySynth.MidiFile Midi(MeltySynth.MidiFileLoopType type, params byte[][] tracks)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("MThd"));
                writer.Write(new byte[] { 0, 0, 0, 6, 0, (byte)(tracks.Length == 1 ? 0 : 1), 0, (byte)tracks.Length, 1, 224 });
                foreach (var track in tracks)
                {
                    writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                    writer.Write(new byte[] { (byte)(track.Length >> 24), (byte)(track.Length >> 16), (byte)(track.Length >> 8), (byte)track.Length });
                    writer.Write(track);
                }
            }
            stream.Position = 0;
            return new MeltySynth.MidiFile(stream, type);
        }

        internal static MeltySynth.SoundFont SoundFont() =>
            new MeltySynth.SoundFont(new MemoryStream(SoundFontBytes(), false));

        internal static byte[] SoundFontBytes()
        {
            byte[] Data(Action<BinaryWriter> write)
            {
                using var stream = new MemoryStream();
                using (var writer = new BinaryWriter(stream, Encoding.ASCII, true)) write(writer);
                return stream.ToArray();
            }
            byte[] Chunk(string id, byte[] data) => Data(w =>
            {
                w.Write(Encoding.ASCII.GetBytes(id)); w.Write(data.Length); w.Write(data);
                if ((data.Length & 1) != 0) w.Write((byte)0);
            });
            byte[] List(string id, params byte[][] chunks) => Chunk("LIST", Data(w =>
            {
                w.Write(Encoding.ASCII.GetBytes(id)); foreach (var chunk in chunks) w.Write(chunk);
            }));
            void Name(BinaryWriter w, string name) { var bytes = new byte[20]; Encoding.ASCII.GetBytes(name).CopyTo(bytes, 0); w.Write(bytes); }
            var info = List("INFO", Chunk("ifil", new byte[] { 2, 0, 1, 0 }));
            var samples = List("sdta", Chunk("smpl", Data(w =>
            {
                for (var i = 0; i < 128; i++) w.Write((short)(12000 * Math.Sin(i * Math.PI / 16)));
                w.Write(new byte[92]);
            })));
            var parameters = List("pdta",
                Chunk("phdr", Data(w => { Name(w, "Loop test"); w.Write(new byte[18]); Name(w, "EOP"); w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)1); w.Write(new byte[12]); })),
                Chunk("pbag", new byte[] { 0, 0, 0, 0, 1, 0, 0, 0 }), Chunk("pmod", new byte[10]),
                Chunk("pgen", new byte[] { 41, 0, 0, 0, 0, 0, 0, 0 }),
                Chunk("inst", Data(w => { Name(w, "Sine"); w.Write((ushort)0); Name(w, "EOI"); w.Write((ushort)1); })),
                Chunk("ibag", new byte[] { 0, 0, 0, 0, 3, 0, 0, 0 }), Chunk("imod", new byte[10]),
                Chunk("igen", Data(w => { w.Write((ushort)38); w.Write((short)-3600); w.Write((ushort)54); w.Write((short)1); w.Write((ushort)53); w.Write((short)0); w.Write(0); })),
                Chunk("shdr", Data(w => { Name(w, "Sine"); w.Write(0); w.Write(128); w.Write(0); w.Write(128); w.Write(32000); w.Write((byte)60); w.Write((sbyte)0); w.Write((ushort)0); w.Write((ushort)1); Name(w, "EOS"); w.Write(new byte[26]); })));
            var file = Chunk("RIFF", Data(w => { w.Write(Encoding.ASCII.GetBytes("sfbk")); w.Write(info); w.Write(samples); w.Write(parameters); }));
            return file;
        }
    }
}
