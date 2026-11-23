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
    public class SampleLoopValidationTest
    {
        private static int Chunk(byte[] data, string name)
        {
            var id = Encoding.ASCII.GetBytes(name);
            for (var i = 0; i <= data.Length - 8; i++)
                if (data[i] == id[0] && data[i + 1] == id[1] && data[i + 2] == id[2] && data[i + 3] == id[3]) return i + 8;
            throw new InvalidOperationException("Missing fixture chunk " + name);
        }
        private static void WriteInt(byte[] data, int at, int value)
        {
            data[at] = (byte)value; data[at + 1] = (byte)(value >> 8);
            data[at + 2] = (byte)(value >> 16); data[at + 3] = (byte)(value >> 24);
        }
        private static void Generator(byte[] data, int index, ushort type, short value)
        {
            var at = Chunk(data, "igen") + index * 4;
            data[at] = (byte)type; data[at + 1] = (byte)(type >> 8);
            data[at + 2] = (byte)value; data[at + 3] = (byte)(value >> 8);
        }

        [TestCase(0, 0)]
        [TestCase(100, 80)]
        public void EmptyOrReversedSampleExtentIsRejected(int start, int end)
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes(); var at = Chunk(bytes, "shdr");
            WriteInt(bytes, at + 20, start); WriteInt(bytes, at + 24, end);
            Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(bytes)));
        }

        [TestCase(1, 20, 20)]
        [TestCase(1, 50, 20)]
        [TestCase(1, 0, 129)]
        [TestCase(3, 20, 20)]
        [TestCase(3, 50, 20)]
        public void ActiveLoopMustHavePositiveExtentInsideItsSample(int mode, int start, int end)
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes(); var at = Chunk(bytes, "shdr");
            Generator(bytes, 1, 54, (short)mode);
            WriteInt(bytes, at + 28, start); WriteInt(bytes, at + 32, end);
            Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(bytes)));
        }

        [TestCase(0, 128)]
        [TestCase(0, 4)]
        [TestCase(2, 128)]
        [TestCase(3, -128)]
        [TestCase(3, 1)]
        public void GeneratorAdjustedRegionMustRemainOrderedAndInBounds(int type, int value)
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes(); Generator(bytes, 0, (ushort)type, (short)value);
            Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(bytes)));
        }

        [Test]
        public void NonLoopingRegionMayHaveUnusedZeroLengthLoopMarkers()
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes(); var at = Chunk(bytes, "shdr");
            Generator(bytes, 1, 54, 0); Generator(bytes, 0, 0, 4);
            WriteInt(bytes, at + 28, 0); WriteInt(bytes, at + 32, 0);
            Assert.DoesNotThrow(() => new SoundFont(new MemoryStream(bytes)));
        }
    }
}
