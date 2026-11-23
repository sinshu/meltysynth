// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using MeltySynth;
using NUnit.Framework;
using static MeltySynthTest.MidiLoopTestAssets;

namespace MeltySynthTest
{
    public class SequencerClockTest
    {
        private static MidiFile LongScore() => Midi(MidiFileLoopType.None, Track(
            (0, new byte[] { 0xC0, 0 }), (20000000, new byte[] { 0xFF, 0x2F, 0 })));
        private static MidiFileSequencer Sequence(int rate, int block)
        {
            var sequence = new MidiFileSequencer(new Synthesizer(SoundFont(), new SynthesizerSettings(rate)
            { BlockSize = block, MaximumPolyphony = 8, EnableReverbAndChorus = false }));
            sequence.Play(LongScore(), false); return sequence;
        }

        // Independent rational oracle: decode IEEE binary32 rather than sharing the
        // production representation or adding rounded TimeSpan/double increments.
        private sealed class ClockReference
        {
            private BigInteger numerator;
            private BigInteger denominator = BigInteger.One;
            internal long Ticks => (long)(numerator / denominator);
            internal void Advance(long frames, int rate, float speed)
            {
                var bits = BitConverter.SingleToUInt32Bits(speed);
                if ((bits & 0x7FFFFFFF) == 0) return;
                var exponent = (int)((bits >> 23) & 255) - 150;
                BigInteger scaledFrames = frames * (BigInteger)TimeSpan.TicksPerSecond * ((bits & 0x7FFFFF) | 0x800000);
                BigInteger divisor = rate;
                if (exponent < 0) divisor <<= -exponent; else scaledFrames <<= exponent;
                numerator = numerator * divisor + scaledFrames * denominator;
                denominator *= divisor;
                var gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);
                numerator /= gcd; denominator /= gcd;
            }
            internal void Rewind(long ticks) => numerator = ticks * denominator + numerator % denominator;
        }

        [TestCase(32000)] [TestCase(44100)] [TestCase(48000)]
        public void HundredThousandActiveBlocksHaveNoCumulativeTickLoss(int rate)
        {
            var sequence = Sequence(rate, 64); var left = new float[64]; var right = new float[64];
            for (var i = 0; i < 100000; i++) sequence.Render(left, right);
            var oracle = new ClockReference(); oracle.Advance(6400000, rate, 1F);
            TestContext.WriteLine($"rate={rate}; frames=6400000; expectedTicks={oracle.Ticks}; actualTicks={sequence.Position.Ticks}; lagTicks={oracle.Ticks - sequence.Position.Ticks}");
            Assert.That(sequence.EndOfSequence, Is.False);
            Assert.That(sequence.Position.Ticks, Is.EqualTo(oracle.Ticks));
        }

        public static IEnumerable<TestCaseData> ClockProfiles()
        {
            foreach (var rate in new[] { 16000, 22050, 32000, 44100, 48000, 88200, 96000, 192000 })
            foreach (var block in new[] { 8, 64, 127, 1024 })
            foreach (var speed in new[] { 0F, MidiFileSequencer.MinimumSpeed, .1F, 1F, 1.1F, MidiFileSequencer.MaximumSpeed })
                yield return new TestCaseData(rate, block, speed);
        }

        [TestCaseSource(nameof(ClockProfiles))]
        public void SupportedClockProfilesMatchExactRationalFrames(int rate, int block, float speed)
        {
            var sequence = Sequence(rate, block); sequence.Speed = speed;
            var left = new float[block]; var right = new float[block];
            for (var i = 0; i < 2048; i++) sequence.Render(left, right);
            var reference = new ClockReference(); reference.Advance(2048L * block, rate, speed);
            Assert.That(sequence.Position.Ticks, Is.EqualTo(reference.Ticks));
            Assert.That(sequence.EndOfSequence, Is.False);
        }

        [TestCase(16000, 8)] [TestCase(16000, 1024)] [TestCase(44100, 8)] [TestCase(44100, 64)]
        [TestCase(44100, 1024)] [TestCase(48000, 64)] [TestCase(192000, 8)] [TestCase(192000, 1024)]
        public void FractionSurvivesSpeedChangesPauseAndFragmentedOutput(int rate, int block)
        {
            var sequence = Sequence(rate, block); var reference = new ClockReference(); long frames = 0;
            var speeds = new[] { .1F, 0F, 16F, MidiFileSequencer.MinimumSpeed, 1.1F,
                MathF.BitIncrement(MidiFileSequencer.MinimumSpeed), MathF.BitDecrement(16F), .33333334F, 1F };
            var sizes = new[] { 1, 0, 7, 65, 511, 1025, 13 };
            var left = new float[1025]; var right = new float[1025];
            for (var i = 0; i < 300; i++)
            {
                var speed = speeds[i % speeds.Length]; var count = sizes[i % sizes.Length];
                sequence.Speed = speed;
                var newBlocks = (frames + count + block - 1) / block - (frames + block - 1) / block;
                reference.Advance(newBlocks * block, rate, speed);
                sequence.Render(left.AsSpan(0, count), right.AsSpan(0, count)); frames += count;
                Assert.That(sequence.Position.Ticks, Is.EqualTo(reference.Ticks), "Fragment/speed change " + i);
            }
        }

        [TestCase(32000, false)] [TestCase(32000, true)]
        [TestCase(44100, false)] [TestCase(44100, true)]
        [TestCase(48000, false)] [TestCase(48000, true)]
        [TestCase(192000, false)] [TestCase(192000, true)]
        public void FragmentationPreservesStereoEventsAndRelease(int rate, bool effects)
        {
            MidiFileSequencer Create()
            {
                var synth = new Synthesizer(SoundFont(), new SynthesizerSettings(rate) { BlockSize = 64, MaximumPolyphony = 8, EnableReverbAndChorus = effects });
                var sequence = new MidiFileSequencer(synth);
                sequence.Play(new MidiFile(new MemoryStream(SynthesisBaselineScenario.MidiBytes())), true);
                sequence.Speed = 1.1F; return sequence;
            }
            var actual = Create(); var expected = Create();
            var a = new float[32768]; var b = new float[32768]; var c = new float[32768]; var d = new float[32768];
            expected.Render(c, d);
            var sizes = new[] { 1, 7, 65, 1023, 511 };
            for (int at = 0, i = 0; at < a.Length; i++)
            {
                var count = Math.Min(sizes[i % sizes.Length], a.Length - at);
                actual.Render(a.AsSpan(at, count), b.AsSpan(at, count)); at += count;
            }
            Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
            Assert.That(actual.Position, Is.EqualTo(expected.Position));
        }

        [TestCase(44100, 16)] [TestCase(48000, 1)] [TestCase(32000, 1)] [TestCase(192000, 1)]
        public void ExactFrameBoundaryEventsAreNotDelayedByTickTruncation(int rate, int seconds)
        {
            var midi = Midi(MidiFileLoopType.None, Track((0, new byte[] { 0x90, 60, 100 }),
                (seconds * 960, new byte[] { 0x80, 60, 0 }), (0, new byte[] { 0x90, 67, 80 }),
                (96, new byte[] { 0xFF, 0x2F, 0 })));
            var actual = Sequence(rate, 64); actual.Play(midi, false);
            var reference = new Synthesizer(SoundFont(), new SynthesizerSettings(rate) { BlockSize = 64, MaximumPolyphony = 8, EnableReverbAndChorus = false });
            var delivered = new List<(int Command, int Key, long Time)>();
            actual.OnSendMessage = (synth, channel, command, a, b) =>
            { delivered.Add((command, a, actual.Position.Ticks)); synth.ProcessMidiMessage(channel, command, a, b); };
            var a = new float[rate * seconds]; var b = new float[a.Length]; var c = new float[a.Length]; var d = new float[a.Length];
            reference.NoteOn(0, 60, 100); reference.Render(c, d); actual.Render(a, b);
            Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
            var expectedTicks = (long)seconds * TimeSpan.TicksPerSecond;
            Assert.That(actual.Position.Ticks, Is.EqualTo(expectedTicks));
            Assert.That(delivered, Has.Count.EqualTo(1));
            reference.NoteOff(0, 60); reference.NoteOn(0, 67, 80);
            reference.Render(c.AsSpan(0, 64), d.AsSpan(0, 64)); actual.Render(a.AsSpan(0, 64), b.AsSpan(0, 64));
            Assert.That(a.AsSpan(0, 64).ToArray(), Is.EqualTo(c.AsSpan(0, 64).ToArray()));
            Assert.That(b.AsSpan(0, 64).ToArray(), Is.EqualTo(d.AsSpan(0, 64).ToArray()));
            Assert.That(delivered, Is.EqualTo(new[] { (0x90, 60, 0L), (0x80, 60, expectedTicks), (0x90, 67, expectedTicks) }));
        }

        [TestCase("play")] [TestCase("stop-play")] [TestCase("synth-reset")]
        [TestCase("stopped-render")] [TestCase("rejected-play")]
        public void LifecycleChangesResetOrRetainTheClockAtItsOwnerBoundary(string action)
        {
            const int rate = 44100, block = 64;
            var sequence = Sequence(rate, block); sequence.Speed = 1.1F;
            var reference = new ClockReference(); reference.Advance(block, rate, sequence.Speed);
            sequence.Render(new float[1], new float[1]); var file = sequence.MidiFile!;
            var position = sequence.Position;
            if (action == "play" || action == "stop-play")
            {
                if (action == "stop-play") { sequence.Stop(); Assert.That(sequence.Position, Is.EqualTo(position)); }
                sequence.Play(file, false); reference = new ClockReference();
                Assert.That(sequence.Position, Is.EqualTo(TimeSpan.Zero));
                sequence.Render(new float[block], new float[block]); reference.Advance(block, rate, sequence.Speed);
            }
            else
            {
                if (action == "synth-reset") sequence.Synthesizer.Reset();
                if (action == "stopped-render") sequence.Stop();
                if (action == "rejected-play")
                {
                    Assert.Throws<ArgumentException>(() => sequence.Play(Midi(MidiFileLoopType.None, Track((0, new byte[] { 0xFF, 0x2F, 0 }))), true));
                    Assert.That(sequence.MidiFile, Is.SameAs(file));
                }
                sequence.Render(new float[block - 1], new float[block - 1]);
                Assert.That(sequence.Position, Is.EqualTo(position));
                sequence.Render(new float[block], new float[block]); reference.Advance(block, rate, sequence.Speed);
            }
            Assert.That(sequence.Position.Ticks, Is.EqualTo(reference.Ticks));
        }

        [TestCase(false)] [TestCase(true)]
        public void HookSpeedChangeAndExceptionPreserveUncommittedClockFraction(bool fail)
        {
            var sequence = Sequence(44100, 64); var calls = 0;
            sequence.OnSendMessage = (_, _, _, _, _) =>
            { calls++; sequence.Speed = .1F; if (fail) throw new ApplicationException("Before clock commit"); };
            var left = new float[64]; var right = new float[64];
            if (fail)
            {
                Assert.Throws<ApplicationException>(() => sequence.Render(left, right));
                Assert.That(sequence.Position, Is.EqualTo(TimeSpan.Zero));
            }
            sequence.Render(left, right);
            var reference = new ClockReference(); reference.Advance(64, 44100, .1F);
            Assert.That(sequence.Position.Ticks, Is.EqualTo(reference.Ticks)); Assert.That(calls, Is.EqualTo(1));
        }

        [TestCase(MidiFileLoopType.None, -1, -1)]
        [TestCase(MidiFileLoopType.RpgMaker, 111, -1)]
        [TestCase(MidiFileLoopType.FinalFantasy, 116, 117)]
        [TestCase(MidiFileLoopType.IncredibleMachine, 110, 111)]
        public void LoopRewindsCarryFractionAndPreserveReleaseAndRestartOrder(MidiFileLoopType type, int start, int end)
        {
            const int rate = 44100, block = 64;
            var note = new byte[] { 0x90, 60, 100 }; var eot = new byte[] { 0xFF, 0x2F, 0 };
            var track = start < 0 ? Track((0, note), (73, eot)) : end < 0
                ? Track((0, new byte[] { 0xB0, (byte)start, 0 }), (0, note), (73, eot))
                : Track((0, new byte[] { 0xB0, (byte)start, 0 }), (0, note), (73, new byte[] { 0xB0, (byte)end, 0 }), (0, eot));
            var midi = Midi(type, track); var actual = Sequence(rate, block); actual.Play(midi, true);
            var reference = new Synthesizer(SoundFont(), new SynthesizerSettings(rate) { BlockSize = block, MaximumPolyphony = 8, EnableReverbAndChorus = false });
            var clock = new ClockReference(); var pending = true; var actualNotes = 0; var expectedNotes = 0;
            actual.OnSendMessage = (synth, channel, command, a, b) =>
            { if (command == 0x90) actualNotes++; synth.ProcessMidiMessage(channel, command, a, b); };
            var a = new float[block]; var b = new float[block]; var c = new float[block]; var d = new float[block];
            for (var i = 0; i < 3000; i++)
            {
                var speed = i % 211 == 0 ? 0F : i % 13 == 0 ? .1F : 1.1F;
                actual.Speed = speed;
                if (clock.Ticks >= midi.Length.Ticks)
                {
                    reference.NoteOffAll(false); clock.Rewind(0); pending = true;
                    if (end >= 0) { reference.NoteOn(0, 60, 100); expectedNotes++; pending = false; }
                }
                else if (pending) { reference.NoteOn(0, 60, 100); expectedNotes++; pending = false; }
                reference.Render(c, d); clock.Advance(block, rate, speed); actual.Render(a, b);
                Assert.That(actual.Position.Ticks, Is.EqualTo(clock.Ticks));
                Assert.That(actualNotes, Is.EqualTo(expectedNotes));
                Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
            }
            Assert.That(actualNotes, Is.GreaterThan(20));
        }
    }
}
