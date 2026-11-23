// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;
using System.Text;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class AssetParsingLimitsTest
    {
        private static readonly byte[] End = { 0, 0xFF, 0x2F, 0 };
        private static byte[] MidiBytes(params byte[][] tracks)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
            writer.Write(Encoding.ASCII.GetBytes("MThd"));
            writer.Write(new byte[] { 0, 0, 0, 6, 0, 1, (byte)(tracks.Length >> 8), (byte)tracks.Length, 1, 224 });
            foreach (var track in tracks)
            {
                writer.Write(Encoding.ASCII.GetBytes("MTrk"));
                var size = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(size, track.Length);
                writer.Write(size); writer.Write(track);
            }
            return stream.ToArray();
        }

        private static int Find(byte[] bytes, string name) => Encoding.ASCII.GetString(bytes).IndexOf(name, StringComparison.Ordinal);
        private static void Little(byte[] bytes, int at, int value) =>
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(at, 4), value);

        [TestCase("file")]
        [TestCase("samples")]
        [TestCase("metadata")]
        [TestCase("records")]
        [TestCase("regions")]
        public void SoundFontBudgetRejectsBeforePromotingAnAsset(string budget)
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes();
            var limits = budget switch
            {
                "file" => new AssetLoadLimits(maxFileBytes: bytes.Length - 1),
                "samples" => new AssetLoadLimits(maxDecodedSampleBytes: 347),
                "metadata" => new AssetLoadLimits(maxMetadataBytes: 11),
                "records" => new AssetLoadLimits(maxSoundFontRecords: 17),
                _ => new AssetLoadLimits(maxSoundFontRegions: 1)
            };
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => new SoundFont(stream, limits));
            Assert.That(stream.CanRead, Is.True);
            if (budget == "file") Assert.That(stream.Position, Is.Zero, "No parsing before the source byte budget.");
            if (budget == "samples") Assert.That(stream.Position, Is.EqualTo(Find(bytes, "smpl") + 8), "No PCM read/allocation before its budget check.");
        }

        [Test]
        public void SoundFontExactBudgetsAcceptOriginalPcm()
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes();
            var limits = new AssetLoadLimits(maxFileBytes: bytes.Length, maxDecodedSampleBytes: 348,
                maxMetadataBytes: 12, maxSoundFontRecords: 18, maxSoundFontRegions: 2);
            using var stream = new MemoryStream(bytes, false);
            var actual = new SoundFont(stream, limits);
            Assert.That(actual.WaveData.ToArray(), Is.EqualTo(MidiLoopTestAssets.SoundFont().WaveData.ToArray()));
        }

        [TestCase("info-child")]
        [TestCase("info-list")]
        [TestCase("version-size")]
        [TestCase("parameter-child")]
        [TestCase("parameter-list")]
        [TestCase("parameter-alignment")]
        [TestCase("modulator-skip")]
        [TestCase("duplicate-table")]
        [TestCase("bag-indices")]
        [TestCase("instrument-zone")]
        [TestCase("sample-rate")]
        [TestCase("riff-trailing")]
        public void SoundFontMetadataAndTableRangesAreValidatedBeforeUse(string scenario)
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes();
            switch (scenario)
            {
                case "info-child": Little(bytes, Find(bytes, "ifil") + 4, int.MaxValue); break;
                case "info-list": Little(bytes, Find(bytes, "INFO") - 4, 4); break;
                case "version-size": Little(bytes, Find(bytes, "ifil") + 4, 2); break;
                case "parameter-child": Little(bytes, Find(bytes, "phdr") + 4, int.MaxValue); break;
                case "parameter-list": Little(bytes, Find(bytes, "pdta") - 4, 4); break;
                case "parameter-alignment": Little(bytes, Find(bytes, "phdr") + 4, 75); break;
                case "modulator-skip": Little(bytes, Find(bytes, "pmod") + 4, int.MaxValue); break;
                case "duplicate-table": Encoding.ASCII.GetBytes("pbag").CopyTo(bytes, Find(bytes, "pmod")); break;
                case "bag-indices": bytes[Find(bytes, "pbag") + 8] = 255; break;
                case "instrument-zone": bytes[Find(bytes, "inst") + 8 + 20] = 1; break;
                case "sample-rate": Little(bytes, Find(bytes, "shdr") + 8 + 36, 0); break;
                case "riff-trailing": Array.Resize(ref bytes, bytes.Length + 1); break;
            }
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => new SoundFont(stream));
            Assert.That(stream.CanRead, Is.True);
        }

        private static byte[] BudgetTrack() => new byte[] { 0, 0xFF, 1, 1, 65, 0, 0x90, 60, 100, 96, 0xFF, 0x2F, 0 };

        [TestCase("file")]
        [TestCase("tracks")]
        [TestCase("track-bytes")]
        [TestCase("events")]
        [TestCase("duration")]
        public void MidiBudgetsIncludeAllTracksAndIgnoredEvents(string budget)
        {
            var track = BudgetTrack();
            var bytes = MidiBytes(track, track);
            var limits = budget switch
            {
                "file" => new AssetLoadLimits(maxFileBytes: bytes.Length - 1),
                "tracks" => new AssetLoadLimits(maxMidiTracks: 1),
                "track-bytes" => new AssetLoadLimits(maxMidiTrackBytes: track.Length - 1),
                "events" => new AssetLoadLimits(maxMidiEvents: 5),
                _ => new AssetLoadLimits(maxMidiDuration: TimeSpan.FromMilliseconds(1))
            };
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => new MidiFile(stream, limits));
            Assert.That(stream.CanRead, Is.True);
            if (budget == "file") Assert.That(stream.Position, Is.Zero);
            if (budget == "tracks") Assert.That(stream.Position, Is.EqualTo(14));
            if (budget == "track-bytes") Assert.That(stream.Position, Is.EqualTo(22));
        }

        [Test]
        public void MidiExactBudgetsAcceptAndPreserveTiming()
        {
            var track = BudgetTrack();
            var bytes = MidiBytes(track, track);
            var limits = new AssetLoadLimits(maxFileBytes: bytes.Length, maxMidiTrackBytes: track.Length,
                maxMidiTracks: 2, maxMidiEvents: 6, maxMidiDuration: TimeSpan.FromSeconds(.1));
            using var stream = new MemoryStream(bytes, false);
            var actual = new MidiFile(stream, limits);
            Assert.That(actual.Messages.Length, Is.EqualTo(4));
            Assert.That(actual.Length.TotalSeconds, Is.EqualTo(.1).Within(1E-7));
        }

        [Test]
        public void DefaultEotPolicyRejectsOpaqueTrailingData()
        {
            using var stream = new MemoryStream(MidiBytes(new byte[] { 0, 0xFF, 0x2F, 0, 255, 222, 0, 128 }, End), false);
            Assert.Throws<InvalidDataException>(() => new MidiFile(stream));
        }

        [Test]
        public void ExplicitZeroPaddingDoesNotConsumeTheNextTrack()
        {
            using var stream = new MemoryStream(MidiBytes(new byte[] { 0, 0xFF, 0x2F, 0, 0, 0, 0 }, End), false);
            var actual = new MidiFile(stream, new AssetLoadLimits(allowMidiEndOfTrackPadding: true));
            Assert.That(actual.Messages.Length, Is.EqualTo(2));
            Assert.That(stream.Position, Is.EqualTo(stream.Length));
        }

        [Test]
        public void PaddingOptionStillRejectsTrailingEvents()
        {
            using var stream = new MemoryStream(MidiBytes(new byte[] { 0, 0xFF, 0x2F, 0, 0, 0x90, 60, 100 }), false);
            Assert.Throws<InvalidDataException>(() => new MidiFile(stream, new AssetLoadLimits(allowMidiEndOfTrackPadding: true)));
        }

        [Test]
        public void StrictEotPolicyRejectsPaddingInsideTheDeclaredTrack()
        {
            using var stream = new MemoryStream(MidiBytes(new byte[] { 0, 0xFF, 0x2F, 0, 0 }), false);
            Assert.Throws<InvalidDataException>(() => new MidiFile(stream, new AssetLoadLimits(allowMidiEndOfTrackPadding: false)));
        }

        [Test]
        public void ValidChannelRunningStatusPreservesMessages()
        {
            using var stream = new MemoryStream(MidiBytes(new byte[] { 0, 0x90, 60, 100, 0, 61, 90, 1, 0xFF, 0x2F, 0 }), false);
            var actual = new MidiFile(stream);
            Assert.That(actual.Messages[0].Data1, Is.EqualTo(60));
            Assert.That(actual.Messages[1].Data1, Is.EqualTo(61));
            Assert.That(actual.Messages[1].Command, Is.EqualTo(0x90));
        }

        [TestCase("missing-eot")]
        [TestCase("overlong-vlv")]
        [TestCase("tick-overflow")]
        [TestCase("max-tick-duration")]
        [TestCase("truncated-channel")]
        [TestCase("reserved-status")]
        [TestCase("running-after-meta")]
        [TestCase("trailing-file")]
        public void MidiMalformedEventsCannotEscapeBoundsOrTimingBudgets(string scenario)
        {
            byte[] track = End;
            switch (scenario)
            {
                case "missing-eot": track = new byte[] { 0, 0x90, 60, 100 }; break;
                case "overlong-vlv": track = new byte[] { 128, 128, 128, 128, 0, 0xFF, 0x2F, 0 }; break;
                case "truncated-channel": track = new byte[] { 0, 0x90, 60 }; break;
                case "reserved-status": track = new byte[] { 0, 0xF1, 0 }; break;
                case "running-after-meta": track = new byte[] { 0, 0x90, 60, 100, 0, 0xFF, 1, 0, 0, 61, 90, 0, 0xFF, 0x2F, 0 }; break;
                case "tick-overflow": case "max-tick-duration":
                    using (var data = new MemoryStream())
                    {
                        for (var i = 0; i < 8; i++) data.Write(new byte[] { 255, 255, 255, 127, 0x90, 60, 100 });
                        data.Write(new byte[] { (byte)(scenario == "tick-overflow" ? 8 : 7), 0xFF, 0x2F, 0 });
                        track = data.ToArray();
                    }
                    break;
            }
            var bytes = MidiBytes(track);
            if (scenario == "trailing-file") Array.Resize(ref bytes, bytes.Length + 1);
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => new MidiFile(stream));
        }

        [Test]
        public void LoopOptionsAndDurationAreValidatedAtTheBoundary()
        {
            using var stream = new MemoryStream(MidiBytes(End), false);
            Assert.Throws<ArgumentOutOfRangeException>(() => new MidiFile(stream, (MidiFileLoopType)99));
            stream.Position = 0;
            Assert.Throws<ArgumentException>(() => new MidiFile(stream, 1));
        }

        [Test]
        public void UnsupportedSmpteTimingIsExplicit()
        {
            var bytes = MidiBytes(End); bytes[12] = 0xE7;
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<NotSupportedException>(() => new MidiFile(stream));
        }

        [TestCase("file")]
        [TestCase("samples")]
        [TestCase("track-bytes")]
        [TestCase("tracks")]
        [TestCase("events")]
        [TestCase("records")]
        [TestCase("regions")]
        [TestCase("metadata")]
        [TestCase("duration")]
        public void NonpositiveBudgetsAreRejected(string budget)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = budget switch
            {
                "file" => new AssetLoadLimits(maxFileBytes: 0),
                "samples" => new AssetLoadLimits(maxDecodedSampleBytes: 0),
                "track-bytes" => new AssetLoadLimits(maxMidiTrackBytes: 0),
                "tracks" => new AssetLoadLimits(maxMidiTracks: 0),
                "events" => new AssetLoadLimits(maxMidiEvents: 0),
                "records" => new AssetLoadLimits(maxSoundFontRecords: 0),
                "regions" => new AssetLoadLimits(maxSoundFontRegions: 0),
                "metadata" => new AssetLoadLimits(maxMetadataBytes: 0),
                _ => new AssetLoadLimits(maxMidiDuration: TimeSpan.Zero)
            });
        }

        [TestCase("zero-track")]
        [TestCase("zero-track-count")]
        [TestCase("cross-track-meta")]
        [TestCase("cross-track-sysex")]
        [TestCase("eot-length")]
        [TestCase("zero-resolution")]
        [TestCase("zero-tempo")]
        [TestCase("running-without-status")]
        [TestCase("status-in-data")]
        public void InvalidMidiCannotReadOutsideItsTrackOrPromoteBadTiming(string scenario)
        {
            var bytes = MidiBytes(End);
            switch (scenario)
            {
                case "zero-track": bytes[21] = 0; break;
                case "zero-track-count": bytes = MidiBytes(); break;
                case "cross-track-meta": bytes = MidiBytes(new byte[] { 0, 0xFF, 1, 5 }, End); break;
                case "cross-track-sysex": bytes = MidiBytes(new byte[] { 0, 0xF0, 5 }, End); break;
                case "eot-length": bytes = MidiBytes(new byte[] { 0, 0xFF, 0x2F, 1, 0 }); break;
                case "zero-resolution": bytes[12] = 0; bytes[13] = 0; break;
                case "zero-tempo": bytes = MidiBytes(new byte[] { 0, 0xFF, 0x51, 3, 0, 0, 0, 0, 0xFF, 0x2F, 0 }); break;
                case "running-without-status": bytes = MidiBytes(new byte[] { 0, 60, 100, 0, 0xFF, 0x2F, 0 }); break;
                case "status-in-data": bytes = MidiBytes(new byte[] { 0, 0x90, 128, 100, 0, 0xFF, 0x2F, 0 }); break;
            }
            using var stream = new MemoryStream(bytes, false);
            Assert.Throws<InvalidDataException>(() => new MidiFile(stream));
            Assert.That(stream.CanRead, Is.True);
        }
    }
}
