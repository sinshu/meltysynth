#nullable enable
using System;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class OscillatorLoopRegressionTest
    {
        private static short[] Samples()
        {
            var data = new short[256];
            for (var i = 0; i < data.Length; i++) data[i] = (short)((i * 7919 % 60001) - 30000);
            return data;
        }

        private static Oscillator Create(short[] data, LoopMode mode, int start, int end, int loopStart, int loopEnd)
        {
            var synth = new Synthesizer(MidiLoopTestAssets.SoundFont(), 32000);
            var oscillator = new Oscillator(synth);
            oscillator.Start(data, mode, 32000, start, end, loopStart, loopEnd, 60, 0, 0, 100);
            return oscillator;
        }

        [TestCase(1, 0.5)]
        [TestCase(1, 63.25)]
        [TestCase(32, 0.5)]
        [TestCase(32, 1.0)]
        [TestCase(32, 32.0)]
        [TestCase(32, 47.875)]
        [TestCase(32, 256.75)]
        [TestCase(32, 65536.25)]
        public void ContinuousLoopMatchesIndependentReferenceAcrossBlocks(int length, double ratio)
        {
            var data = Samples();
            var oscillator = Create(data, LoopMode.Continuous, 3, 160, 9, 9 + length);
            var reference = new Reference(data, 3, 160, 9, 9 + length, true, ratio);
            foreach (var size in new[] { 1, 63, 64, 65, 257 })
            {
                var block = new float[size];
                Assert.That(oscillator.FillBlock(block, ratio), Is.True);
                Assert.That(block, Is.EqualTo(reference.Read(size)).Within(1E-6));
            }
        }

        [Test]
        public void HighestMidiNoteDoesNotEscapeShortLoop()
        {
            var data = Samples();
            var oscillator = Create(data, LoopMode.Continuous, 0, 128, 0, 32);
            var ratio = MathF.Pow(2, 67 / 12F);
            var reference = new Reference(data, 0, 128, 0, 32, true, ratio);
            var block = new float[512];
            for (var i = 0; i < 8; i++)
            {
                Assert.That(oscillator.Process(block, 127), Is.True);
                Assert.That(block, Is.EqualTo(reference.Read(block.Length)).Within(1E-6));
            }
        }

        [Test]
        public void FractionalSeamInterpolatesToLoopStartInsteadOfTail()
        {
            var data = Samples();
            var oscillator = Create(data, LoopMode.Continuous, 5, 20, 5, 9);
            var block = new float[10];
            oscillator.FillBlock(block, .5);
            Assert.That(block[7], Is.EqualTo((data[8] + data[5]) / 65536F).Within(1E-6));
            Assert.That(block, Is.EqualTo(new Reference(data, 5, 20, 5, 9, true, .5).Read(10)).Within(1E-6));
        }

        [TestCase(0.5)]
        [TestCase(47.875)]
        [TestCase(256.75)]
        public void NoteOffNormalizesPendingAdvanceAndContinuesIntoTail(double ratio)
        {
            var data = Samples();
            var oscillator = Create(data, LoopMode.LoopUntilNoteOff, 9, 160, 9, 41);
            var reference = new Reference(data, 9, 160, 9, 41, true, ratio);
            var before = new float[7];
            oscillator.FillBlock(before, ratio);
            Assert.That(before, Is.EqualTo(reference.Read(7)).Within(1E-6));
            oscillator.Release(); reference.Release();
            AssertFinishes(oscillator, reference, ratio);
        }

        [Test]
        public void RepeatedNoteOffDoesNotRewindAlreadyReleasedTail()
        {
            var data = Samples();
            var oscillator = Create(data, LoopMode.LoopUntilNoteOff, 9, 160, 9, 41);
            var reference = new Reference(data, 9, 160, 9, 41, true, .5);
            oscillator.FillBlock(new float[7], .5); reference.Read(7);
            oscillator.Release(); reference.Release();
            var tail = new float[80];
            oscillator.FillBlock(tail, .5);
            Assert.That(tail, Is.EqualTo(reference.Read(80)).Within(1E-6));
            oscillator.Release();
            AssertFinishes(oscillator, reference, .5);
        }

        [Test]
        public void ReleaseKeepsContinuousModeLooping()
        {
            var data = Samples();
            var oscillator = Create(data, LoopMode.Continuous, 3, 160, 9, 41);
            var reference = new Reference(data, 3, 160, 9, 41, true, 47.875);
            oscillator.FillBlock(new float[7], 47.875); reference.Read(7);
            oscillator.Release();
            var block = new float[257];
            Assert.That(oscillator.FillBlock(block, 47.875), Is.True);
            Assert.That(block, Is.EqualTo(reference.Read(block.Length)).Within(1E-6));
        }

        [TestCase(0.5)]
        [TestCase(1.0)]
        [TestCase(47.875)]
        public void NonLoopingPlaybackAndZeroPaddedTailAreUnchanged(double ratio)
        {
            var data = Samples();
            var oscillator = Create(data, LoopMode.NoLoop, 3, 160, 0, 0);
            oscillator.Release();
            AssertFinishes(oscillator, new Reference(data, 3, 160, 0, 0, false, ratio), ratio);
        }

        private static void AssertFinishes(Oscillator oscillator, Reference reference, double ratio)
        {
            for (var i = 0; i < 64; i++)
            {
                var block = new float[17];
                var expected = reference.Read(block.Length);
                var active = oscillator.FillBlock(block, ratio);
                Assert.That(active, Is.EqualTo(expected != null));
                if (!active) return;
                Assert.That(block, Is.EqualTo(expected).Within(1E-6));
            }
            Assert.Fail("Non-looping playback did not finish within the fixture bound.");
        }

        // Double-precision phase/interpolation oracle. The production path uses fixed-point
        // arithmetic; only the ABI's 24-bit step quantization is shared here.
        private sealed class Reference
        {
            private readonly short[] data;
            private readonly int end, loopStart, loopEnd;
            private readonly double step;
            private double position;
            private bool looping;
            internal Reference(short[] data, int start, int end, int loopStart, int loopEnd, bool looping, double ratio)
            {
                this.data = data; this.end = end; this.loopStart = loopStart; this.loopEnd = loopEnd;
                this.looping = looping; position = start;
                step = Math.Floor(ratio * 16777216) / 16777216;
            }
            private void Normalize()
            {
                if (position >= loopEnd)
                {
                    var length = loopEnd - loopStart;
                    var cycles = Math.Floor((position - loopStart) / length);
                    position -= cycles * length;
                }
            }
            internal void Release() { Normalize(); looping = false; }
            internal float[]? Read(int count)
            {
                var output = new float[count];
                for (var i = 0; i < count; i++)
                {
                    if (looping) Normalize();
                    if (!looping && position >= end) return i == 0 ? null : output;
                    var at = (int)Math.Floor(position);
                    var next = looping && at + 1 >= loopEnd ? loopStart : at + 1;
                    var fraction = position - at;
                    output[i] = (float)((data[at] * (1 - fraction) + data[next] * fraction) / 32768);
                    position += step;
                }
                return output;
            }
        }
    }
}
