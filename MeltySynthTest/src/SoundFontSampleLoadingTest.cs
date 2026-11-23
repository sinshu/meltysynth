using System;
using System.IO;
using System.Text;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class SoundFontSampleLoadingTest
    {
        private static byte[] Chunk(string id, byte[] bytes)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
            writer.Write(Encoding.ASCII.GetBytes(id)); writer.Write(bytes.Length); writer.Write(bytes);
            if ((bytes.Length & 1) != 0) writer.Write((byte)0);
            return stream.ToArray();
        }

        private static byte[] List(params byte[][] chunks)
        {
            using var stream = new MemoryStream();
            stream.Write(Encoding.ASCII.GetBytes("sdta"));
            foreach (var chunk in chunks) stream.Write(chunk);
            return Chunk("LIST", stream.ToArray());
        }

        private static void Size(byte[] bytes, int offset, int size) =>
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, 4), size);

        private static SoundFontSampleData LoadList(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.ASCII, true);
            return new SoundFontSampleData(reader, stream.Length);
        }

        private sealed class ShortReadStream : MemoryStream
        {
            private readonly int maximum;
            private readonly long stopAt;
            internal ShortReadStream(byte[] bytes, int maximum, long stopAt = long.MaxValue) : base(bytes, false)
            { this.maximum = maximum; this.stopAt = stopAt; }
            public override int Read(byte[] buffer, int offset, int count) =>
                base.Read(buffer, offset, (int)Math.Min(Math.Min(count, maximum), Math.Max(0, stopAt - Position)));
            public override int Read(Span<byte> buffer) =>
                base.Read(buffer.Slice(0, (int)Math.Min(Math.Min(buffer.Length, maximum), Math.Max(0, stopAt - Position))));
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(17)]
        [TestCase(1024)]
        public void SeekableShortReadsLoadIdenticalPcmAndLeaveCallerStreamOpen(int maximum)
        {
            using var stream = new ShortReadStream(MidiLoopTestAssets.SoundFontBytes(), maximum);
            var loaded = new SoundFont(stream);
            Assert.That(loaded.WaveData.ToArray(), Is.EqualTo(MidiLoopTestAssets.SoundFont().WaveData.ToArray()));
            Assert.That(stream.CanRead, Is.True);
            stream.Position = 0;
            Assert.That(stream.ReadByte(), Is.EqualTo((int)'R'));
        }

        [TestCase("list-too-small")]
        [TestCase("child-crosses-list")]
        [TestCase("unsigned-child-size")]
        [TestCase("odd-pcm")]
        [TestCase("empty-pcm")]
        [TestCase("incomplete-child-header")]
        [TestCase("duplicate-pcm")]
        [TestCase("missing-pcm")]
        [TestCase("missing-padding")]
        [TestCase("sm24-crosses-list")]
        public void InvalidListBoundariesAndSampleAlignmentAreRejected(string scenario)
        {
            var bytes = List(Chunk("smpl", new byte[] { 1, 2, 3, 4 }));
            switch (scenario)
            {
                case "list-too-small": Size(bytes, 4, 3); break;
                case "child-crosses-list": Size(bytes, 16, 8); break;
                case "unsigned-child-size": Size(bytes, 16, -1); break;
                case "odd-pcm": bytes = List(Chunk("smpl", new byte[] { 1, 2, 3 })); break;
                case "empty-pcm": bytes = List(Chunk("smpl", Array.Empty<byte>())); break;
                case "incomplete-child-header": bytes = List(new byte[] { 115, 109, 112, 108 }); break;
                case "duplicate-pcm": bytes = List(Chunk("smpl", new byte[4]), Chunk("smpl", new byte[4])); break;
                case "missing-pcm": bytes = List(Chunk("sm24", new byte[2])); break;
                case "missing-padding": bytes = List(Chunk("smpl", new byte[4]), Chunk("sm24", new byte[1])); Array.Resize(ref bytes, bytes.Length - 1); Size(bytes, 4, bytes.Length - 8); break;
                case "sm24-crosses-list": bytes = List(Chunk("smpl", new byte[4]), Chunk("sm24", new byte[2])); Size(bytes, 28, 20); break;
            }
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => LoadList(stream));
            Assert.That(stream.CanRead, Is.True);
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(15)]
        public void PhysicallyTruncatedListFailsBeforePromotion(int removed)
        {
            var bytes = List(Chunk("smpl", new byte[16]));
            Array.Resize(ref bytes, bytes.Length - removed);
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<EndOfStreamException>(() => LoadList(stream));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void ZeroReadBeforeDeclaredEndIsNotPromotedAsZeroFilledPcm()
        {
            var bytes = List(Chunk("smpl", new byte[16]));
            using var stream = new ShortReadStream(bytes, 3, 25);
            Assert.That(Assert.Throws<EndOfStreamException>(() => LoadList(stream))?.Message, Does.Contain("sample data is incomplete"));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void OddSm24PaddingDoesNotConsumeTheNextChunkHeader()
        {
            using var stream = new ShortReadStream(List(Chunk("sm24", new byte[3]), Chunk("smpl", new byte[] { 1, 2, 3, 4 })), 1);
            var loaded = LoadList(stream);
            Assert.That(loaded.Samples, Is.EqualTo(new short[] { 513, 1027 }));
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
        }

        [Test]
        public void MinimalAlignedPcmDoesNotRequireFourBytesForFormatDetection()
        {
            using var stream = new MemoryStream(List(Chunk("smpl", new byte[] { 1, 2 })), false);
            Assert.That(LoadList(stream).Samples, Is.EqualTo(new short[] { 513 }));
        }

        [Test]
        public void SoundFont3IsStillRejectedWithoutClosingCallerStream()
        {
            using var stream = new MemoryStream(List(Chunk("smpl", Encoding.ASCII.GetBytes("OggS"))), false);
            Assert.Throws<NotSupportedException>(() => LoadList(stream));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void SampleListCannotBorrowBytesOutsideDeclaredRiff()
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes();
            var sdta = Encoding.ASCII.GetString(bytes).IndexOf("sdta", StringComparison.Ordinal);
            Size(bytes, 4, sdta + 4 - 8);
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => new SoundFont(stream));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void ChildChunkCannotConsumeBytesFollowingItsList()
        {
            var bytes = List(Chunk("smpl", new byte[4]));
            Size(bytes, 16, 8);
            Array.Resize(ref bytes, bytes.Length + 4);
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => LoadList(stream));
            Assert.That(stream.Position, Is.EqualTo(20), "Reject before reading any PCM outside its LIST.");
        }

        [Test]
        public void PrematurePcmEofLeavesPublicCallerStreamOpen()
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes();
            var smpl = Encoding.ASCII.GetString(bytes).IndexOf("smpl", StringComparison.Ordinal);
            using var stream = new ShortReadStream(bytes, 3, smpl + 8 + 5);
            Assert.That(Assert.Throws<EndOfStreamException>(() => new SoundFont(stream))?.Message,
                Does.Contain("sample data is incomplete"));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void TruncatedRiffLeavesPublicCallerStreamOpen()
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes();
            Array.Resize(ref bytes, bytes.Length - 1);
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<EndOfStreamException>(() => new SoundFont(stream));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void LoadedPcmMatchesOriginalFixtureIncludingSignAndGuardSamples()
        {
            var soundFont = MidiLoopTestAssets.SoundFont();
            Assert.That(soundFont.WaveData.Length, Is.EqualTo(174));
            for (var i = 0; i < 128; i++)
            {
                var expected = (short)(12000 * Math.Sin(i * Math.PI / 16));
                Assert.That(soundFont.WaveData[i], Is.EqualTo(expected), "PCM sample " + i);
            }
            for (var i = 128; i < soundFont.WaveData.Length; i++)
                Assert.That(soundFont.WaveData[i], Is.Zero, "Guard sample " + i);
        }
    }
}
