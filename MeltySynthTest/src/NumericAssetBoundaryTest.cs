// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;
using System.Buffers.Binary;
using System.Text;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    [TestFixture]
    public class NumericAssetBoundaryTest
    {
        internal static int Chunk(byte[] bytes, string name) => bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes(name)) + 8;
        internal static byte[] Font(int type, int value)
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes(); var at = Chunk(bytes, "igen");
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at), (ushort)type);
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(at + 2), (short)value);
            return bytes;
        }
        [TestCase(5, -12000)] [TestCase(5, 12000)]
        [TestCase(6, -12000)] [TestCase(6, 12000)]
        [TestCase(7, -12000)] [TestCase(7, 12000)]
        [TestCase(51, -120)] [TestCase(51, 120)]
        [TestCase(52, -99)] [TestCase(52, 99)]
        [TestCase(17, -500)] [TestCase(17, 500)]
        [TestCase(8, 1500)] [TestCase(8, 13500)]
        [TestCase(9, 0)] [TestCase(9, 960)]
        [TestCase(46, -1)] [TestCase(46, 0)] [TestCase(46, 127)]
        [TestCase(47, -1)] [TestCase(47, 0)] [TestCase(47, 127)]
        [TestCase(58, -1)] [TestCase(58, 0)] [TestCase(58, 127)]
        [TestCase(54, 0)] [TestCase(54, 1)] [TestCase(54, 2)] [TestCase(54, 3)]
        [TestCase(56, 0)] [TestCase(56, 100)]
        [TestCase(21, -32768)] [TestCase(23, -32768)] [TestCase(25, -32768)]
        [TestCase(26, -32768)] [TestCase(27, -32768)] [TestCase(28, -32768)]
        [TestCase(30, -32768)] [TestCase(33, -32768)] [TestCase(34, -32768)]
        [TestCase(35, -32768)] [TestCase(36, -32768)] [TestCase(38, -32768)]
        public void SupportedGeneratorExtremaAndSentinelsRenderFiniteAudio(int type, int value)
        {
            var font = new SoundFont(new MemoryStream(Font(type, value)));
            foreach (var rate in new[] { 16000, 192000 })
            {
                var synth = new Synthesizer(font, new SynthesizerSettings(rate) { EnableReverbAndChorus = false });
                var left = new float[512]; var right = new float[512];
                foreach (var key in new[] { 0, 60, 127 })
                {
                    synth.NoteOn(0, key, 127); synth.Render(left, right);
                    Assert.That(Array.TrueForAll(left, float.IsFinite), Is.True);
                    Assert.That(Array.TrueForAll(right, float.IsFinite), Is.True); synth.NoteOffAll(true);
                }
            }
        }

        [TestCase(51, -121)] [TestCase(51, 121)] [TestCase(52, 100)]
        [TestCase(17, 501)] [TestCase(8, 1499)] [TestCase(9, -1)]
        [TestCase(46, 128)] [TestCase(47, -2)] [TestCase(58, 128)]
        [TestCase(54, 4)] [TestCase(43, 0x7F80)] [TestCase(44, 0x807F)]
        [TestCase(56, -1)] [TestCase(56, 1201)]
        [TestCase(38, 32767)] [TestCase(48, -1)] [TestCase(15, 1001)]
        public void InvalidGeneratorSemanticsAreRejected(int type, int value)
            => Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(Font(type, value))));

        [Test]
        public void LegalRawValuesWithUnrepresentableEffectivePitchAreRejected()
            => Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(Font(56, 1200))));

        [TestCase(0)] [TestCase(127)] [TestCase(255)]
        public void OriginalPitchSentinelsAndExtremaRemainSupported(int pitch)
        {
            var bytes = Font(51, 0); bytes[Chunk(bytes, "shdr") + 40] = (byte)pitch;
            Assert.DoesNotThrow(() => new SoundFont(new MemoryStream(bytes)));
        }

        [TestCase(-1, 0x90, 60, 100)] [TestCase(16, 0x90, 60, 100)]
        [TestCase(0, 0x91, 60, 100)] [TestCase(0, 0xF0, 0, 0)]
        [TestCase(0, 0x90, -1, 100)] [TestCase(0, 0x90, 60, 128)]
        [TestCase(0, 0xB0, 6, -1)] [TestCase(0, 0xE0, 128, 0)]
        [TestCase(0, 0xC0, 128, 0)]
        public void MalformedLiveMessagesAreAtomic(int channel, int command, int data1, int data2)
        {
            var synth = SynthesisBaselineScenario.Synth(false); synth.NoteOn(0, 60, 100);
            var count = synth.ActiveVoiceCount;
            Assert.Throws<ArgumentOutOfRangeException>(() => synth.ProcessMidiMessage(channel, command, data1, data2));
            Assert.That(synth.ActiveVoiceCount, Is.EqualTo(count));
        }

        [TestCase(-1, 60, 100)] [TestCase(0, 128, 100)] [TestCase(0, 60, -1)]
        public void DirectNoteBoundaryRejectsInvalidData(int channel, int key, int velocity)
        {
            var synth = SynthesisBaselineScenario.Synth(false);
            Assert.Throws<ArgumentOutOfRangeException>(() => synth.NoteOn(channel, key, velocity));
            Assert.That(synth.ActiveVoiceCount, Is.Zero);
        }

        [TestCase(0)] [TestCase(127)]
        public void LegalLiveRpnAndWheelExtremaRemainFinite(int value)
        {
            var synth = SynthesisBaselineScenario.Synth(false);
            foreach (var rpn in new[] { 0, 1, 2 })
            {
                synth.ProcessMidiMessage(0, 0xB0, 101, 0); synth.ProcessMidiMessage(0, 0xB0, 100, rpn);
                synth.ProcessMidiMessage(0, 0xB0, 6, value); synth.ProcessMidiMessage(0, 0xB0, 38, value);
            }
            synth.ProcessMidiMessage(0, 0xE0, value, value); synth.NoteOn(0, 60, 127);
            var left = new float[512]; var right = new float[512]; synth.Render(left, right);
            Assert.That(Array.TrueForAll(left, float.IsFinite), Is.True);
        }

        [Test]
        public void PreparedPitchDomainRejectsControllerBeforeMutation()
        {
            var bytes = Font(56, 1200);
            // Restrict this high-scale instrument to one central key.
            var at = Chunk(bytes, "igen") + 4;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at), 43);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + 2), 0x3C3C);
            var font = new SoundFont(new MemoryStream(bytes));
            var actual = new Synthesizer(font, 32000); var expected = new Synthesizer(font, 32000);
            foreach (var synth in new[] { actual, expected })
            {
                synth.ProcessMidiMessage(0, 0xB0, 101, 0); synth.ProcessMidiMessage(0, 0xB0, 100, 2);
                synth.NoteOn(0, 60, 100);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => actual.ProcessMidiMessage(0, 0xB0, 6, 127));
            var a = new float[512]; var b = new float[512]; var c = new float[512]; var d = new float[512];
            actual.Render(a, b); expected.Render(c, d); Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
            actual.ResetAllControllers(); Assert.DoesNotThrow(() => actual.Render(a, b));
        }

        private static byte[] WithPresetGenerator(byte[] bytes, ushort type, short value)
        {
            var at = Chunk(bytes, "pgen"); var result = new byte[bytes.Length + 4];
            bytes.AsSpan(0, at).CopyTo(result); bytes.AsSpan(at).CopyTo(result.AsSpan(at + 4));
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(at), type);
            BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(at + 2), value);
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(at - 4), BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(at - 4)) + 4);
            var pdta = bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("pdta"));
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(pdta - 4), BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(pdta - 4)) + 4);
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), result.Length - 8);
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(Chunk(result, "pbag") + 4), 2);
            return result;
        }

        [TestCase(17, -500)] [TestCase(8, -12000)] [TestCase(48, -100)]
        public void PresetOffsetsAreRelativeRatherThanAbsolute(int type, int value)
        {
            var bytes = WithPresetGenerator(Font(51, 0), (ushort)type, (short)value);
            Assert.DoesNotThrow(() => new SoundFont(new MemoryStream(bytes)));
        }

        [Test]
        public void UnsafePresetGeneratorIsRejected()
            => Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(WithPresetGenerator(Font(51, 0), 51, 32767))));

        [Test]
        public void CombinedLegalModulationDepthsMustRemainRepresentable()
        {
            // Each +/-120-semitone generator is individually legal, but this combination
            // reaches below the Q24 minimum even before any channel tuning is applied.
            var bytes = WithPresetGenerator(Font(5, 12000), 6, 12000);
            var at = Chunk(bytes, "igen") + 4;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at), 7);
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(at + 2), -12000);
            Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(bytes)));
        }

        [TestCase("wheel")] [TestCase("range-coarse")] [TestCase("fine-coarse")] [TestCase("fine-low")]
        public void EveryUnsafePitchControllerUpdateIsAtomic(string scenario)
        {
            var bytes = Font(56, 1200); var at = Chunk(bytes, "igen") + 4;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at), 43);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + 2), 0x3C3C);
            var font = new SoundFont(new MemoryStream(bytes));
            var actual = new Synthesizer(font, 32000); var expected = new Synthesizer(font, 32000);
            void Rpn(Synthesizer synth, int id) { synth.ProcessMidiMessage(0, 0xB0, 101, 0); synth.ProcessMidiMessage(0, 0xB0, 100, id); }
            foreach (var synth in new[] { actual, expected })
            {
                if (scenario == "wheel") { Rpn(synth, 0); synth.ProcessMidiMessage(0, 0xB0, 6, 127); }
                else if (scenario == "range-coarse") { Rpn(synth, 0); synth.ProcessMidiMessage(0, 0xE0, 127, 127); }
                else
                {
                    Rpn(synth, 2); synth.ProcessMidiMessage(0, 0xB0, 6, 100); // +36 semitones
                    Rpn(synth, 1);
                    if (scenario == "fine-low") synth.ProcessMidiMessage(0, 0xB0, 6, 95);
                }
                synth.NoteOn(0, 60, 100);
            }
            if (scenario == "wheel") Assert.Throws<ArgumentOutOfRangeException>(() => actual.ProcessMidiMessage(0, 0xE0, 127, 127));
            else Assert.Throws<ArgumentOutOfRangeException>(() => actual.ProcessMidiMessage(0, 0xB0, scenario == "fine-low" ? 38 : 6, 127));
            var a = new float[512]; var b = new float[512]; var c = new float[512]; var d = new float[512];
            actual.Render(a, b); expected.Render(c, d); Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
        }

        [Test]
        public void RejectedReplacementAssetCannotDisturbCurrentPlayback()
        {
            var actual = SynthesisBaselineScenario.Synth(false); var expected = SynthesisBaselineScenario.Synth(false);
            actual.NoteOn(0, 60, 100); expected.NoteOn(0, 60, 100);
            Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(Font(51, 32767))));
            var a = new float[512]; var b = new float[512]; var c = new float[512]; var d = new float[512];
            actual.Render(a, b); expected.Render(c, d); Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
        }

        [Test]
        public void NullRpnAndNoteOnZeroRetainSupportedMidiSemantics()
        {
            var actual = SynthesisBaselineScenario.Synth(false); var expected = SynthesisBaselineScenario.Synth(false);
            actual.ProcessMidiMessage(0, 0xB0, 101, 127); actual.ProcessMidiMessage(0, 0xB0, 100, 127);
            actual.ProcessMidiMessage(0, 0xB0, 6, 127); actual.ProcessMidiMessage(0, 0xB0, 38, 127);
            actual.NoteOn(0, 60, 100); expected.NoteOn(0, 60, 100);
            actual.NoteOn(0, 60, 0); expected.NoteOff(0, 60);
            var a = new float[512]; var b = new float[512]; var c = new float[512]; var d = new float[512];
            actual.Render(a, b); expected.Render(c, d); Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
        }

        [Test]
        public void UnsafeCoarseTuneIsRejectedBeforePromotion()
            => Assert.Throws<InvalidDataException>(() => new SoundFont(new MemoryStream(Font(51, 32767))));

        [Test]
        public void InvalidLiveRpnCannotChangePlayback()
        {
            var actual = SynthesisBaselineScenario.Synth(false); var expected = SynthesisBaselineScenario.Synth(false);
            foreach (var synth in new[] { actual, expected })
            {
                synth.ProcessMidiMessage(0, 0xB0, 101, 0); synth.ProcessMidiMessage(0, 0xB0, 100, 2);
                synth.NoteOn(0, 60, 100);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => actual.ProcessMidiMessage(0, 0xB0, 6, 32831));
            var left = new float[512]; var right = new float[512]; var a = new float[512]; var b = new float[512];
            actual.Render(left, right); expected.Render(a, b);
            Assert.That(left, Is.EqualTo(a)); Assert.That(right, Is.EqualTo(b));
        }
    }
}
