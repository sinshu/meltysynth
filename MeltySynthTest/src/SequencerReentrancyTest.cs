using System;
using System.Collections.Generic;
using MeltySynth;
using NUnit.Framework;
using static MeltySynthTest.MidiLoopTestAssets;

namespace MeltySynthTest
{
    public class SequencerReentrancyTest
    {
        private static MidiFile Score() => Midi(MidiFileLoopType.None, Track(
            (0, new byte[] { 0x90, 60, 100 }), (0, new byte[] { 0x90, 67, 80 }),
            (96, new byte[] { 0xB0, 123, 0 }), (96, new byte[] { 0xFF, 0x2F, 0 })));
        private static MidiFileSequencer Sequence()
        {
            var sequence = new MidiFileSequencer(SynthesisBaselineScenario.Synth(false));
            sequence.Play(Score(), false); return sequence;
        }
        private static void SameAudio(MidiFileSequencer actual, MidiFileSequencer expected, int frames = 513)
        {
            var a = new float[frames]; var b = new float[frames]; var c = new float[frames]; var d = new float[frames];
            actual.Render(a, b); expected.Render(c, d);
            Assert.That(a, Is.EqualTo(c)); Assert.That(b, Is.EqualTo(d));
            Assert.That(actual.Position, Is.EqualTo(expected.Position));
            Assert.That(actual.EndOfSequence, Is.EqualTo(expected.EndOfSequence));
        }

        [TestCase("stop")] [TestCase("play")] [TestCase("render")] [TestCase("empty-render")]
        public void ReentrantPlaybackOperationIsRejectedBeforeMutation(string operation)
        {
            var actual = Sequence(); var expected = Sequence(); var file = actual.MidiFile; var calls = 0; var insideHook = false;
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                if (insideHook) throw new ApplicationException("Nested hook reached; missing render guard.");
                insideHook = true;
                try
                {
                    calls++;
                    var position = actual.Position; var voices = s.ActiveVoiceCount;
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        if (operation == "stop") actual.Stop();
                        else if (operation == "play") actual.Play(Score(), true);
                        else if (operation == "render") actual.Render(new float[1], new float[1]);
                        else actual.Render(Span<float>.Empty, Span<float>.Empty);
                    });
                    Assert.That(actual.MidiFile, Is.SameAs(file)); Assert.That(actual.Position, Is.EqualTo(position));
                    Assert.That(s.ActiveVoiceCount, Is.EqualTo(voices));
                    s.ProcessMidiMessage(channel, command, a, b);
                }
                finally { insideHook = false; }
            };
            SameAudio(actual, expected); Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void ThrowAfterForwardingConsumesEventOnceAndReleasesRenderGuard()
        {
            var actual = Sequence(); var expected = Sequence(); var keys = new List<int>();
            var failure = new ApplicationException("Original hook failure");
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                keys.Add(a); s.ProcessMidiMessage(channel, command, a, b);
                if (keys.Count == 1) throw failure;
            };
            Assert.That(Assert.Throws<ApplicationException>(() => actual.Render(new float[64], new float[64])), Is.SameAs(failure));
            Assert.That(actual.Position, Is.EqualTo(TimeSpan.Zero));
            SameAudio(actual, expected); Assert.That(keys, Is.EqualTo(new[] { 60, 67 }));
            actual.Stop(); Assert.That(actual.MidiFile, Is.Null); Assert.That(actual.Synthesizer.ActiveVoiceCount, Is.Zero);
            actual.Play(Score(), false); actual.OnSendMessage = null;
            expected.Play(Score(), false); SameAudio(actual, expected);
        }

        [TestCase("stop")] [TestCase("play")] [TestCase("render")]
        public void UncaughtReentrancyFailureConsumesOnlyItsEventAndAllowsRecovery(string operation)
        {
            var actual = Sequence(); var expected = Sequence(); var calls = 0; var file = actual.MidiFile;
            var entered = false;
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                if (entered) throw new ApplicationException("Unexpected recursive dispatch.");
                entered = true;
                try
                {
                    if (++calls == 1)
                    {
                        if (operation == "stop") actual.Stop();
                        else if (operation == "play") actual.Play(Score(), false);
                        else actual.Render(new float[1], new float[1]);
                    }
                    s.ProcessMidiMessage(channel, command, a, b);
                }
                finally { entered = false; }
            };
            expected.OnSendMessage = (s, channel, command, a, b) => { if (a != 60) s.ProcessMidiMessage(channel, command, a, b); };
            Assert.Throws<InvalidOperationException>(() => actual.Render(new float[64], new float[64]));
            Assert.That(actual.MidiFile, Is.SameAs(file)); Assert.That(actual.Position, Is.EqualTo(TimeSpan.Zero));
            SameAudio(actual, expected); Assert.That(calls, Is.EqualTo(2));
            actual.Stop(); Assert.That(actual.EndOfSequence, Is.True);
        }

        [TestCase("null")] [TestCase("zero-loop")]
        public void InvalidPlayFromHookPreservesArgumentsAndExistingPlayback(string invalid)
        {
            var actual = Sequence(); var expected = Sequence(); var file = actual.MidiFile;
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                var voices = s.ActiveVoiceCount; var time = actual.Position;
                if (invalid == "null") Assert.Throws<ArgumentNullException>(() => actual.Play(null!, false));
                else Assert.Throws<ArgumentException>(() => actual.Play(Midi(MidiFileLoopType.None,
                    Track((0, new byte[] { 0xFF, 0x2F, 0 }))), true));
                Assert.That(actual.MidiFile, Is.SameAs(file)); Assert.That(actual.Position, Is.EqualTo(time));
                Assert.That(s.ActiveVoiceCount, Is.EqualTo(voices));
                s.ProcessMidiMessage(channel, command, a, b);
            };
            SameAudio(actual, expected);
        }

        [TestCase(false)] [TestCase(true)]
        public void HookReplacementAppliesToNextEventIncludingSameTick(bool clear)
        {
            var actual = Sequence(); var expected = Sequence(); var seen = new List<string>();
            MidiFileSequencer.MessageHook replacement = (s, channel, command, a, b) =>
            { seen.Add("new:" + a); s.ProcessMidiMessage(channel, command, a + 12, b); };
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                seen.Add("old:" + a); actual.OnSendMessage = clear ? null : replacement;
                s.ProcessMidiMessage(channel, command, a, b);
            };
            if (!clear) expected.OnSendMessage = (s, channel, command, a, b) => s.ProcessMidiMessage(channel, command, a == 67 ? 79 : a, b);
            SameAudio(actual, expected);
            Assert.That(seen, Is.EqualTo(clear ? new[] { "old:60" } : new[] { "old:60", "new:67" }));
        }

        [TestCase(false)] [TestCase(true)]
        public void ThrowingHookIsNotRetriedWhetherItForwardedOrDroppedTheEvent(bool forward)
        {
            var actual = Sequence(); var expected = Sequence(); var seen = new List<int>();
            var failure = new ApplicationException("Keep original exception identity");
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                seen.Add(a);
                if (a != 60 || forward) s.ProcessMidiMessage(channel, command, a, b);
                if (a == 60) throw failure;
            };
            if (!forward) expected.OnSendMessage = (s, channel, command, a, b) => { if (a != 60) s.ProcessMidiMessage(channel, command, a, b); };
            Assert.That(Assert.Throws<ApplicationException>(() => actual.Render(new float[64], new float[64])), Is.SameAs(failure));
            SameAudio(actual, expected); Assert.That(seen, Is.EqualTo(new[] { 60, 67 }));
        }

        [Test]
        public void ReplacementSurvivesExceptionWithoutReplayingOldHook()
        {
            var actual = Sequence(); var expected = Sequence(); var oldCalls = 0; var newCalls = 0;
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                oldCalls++; s.ProcessMidiMessage(channel, command, a, b);
                actual.OnSendMessage = (next, c, cmd, x, y) => { newCalls++; next.ProcessMidiMessage(c, cmd, x, y); };
                throw new ApplicationException("Switch and fail");
            };
            Assert.Throws<ApplicationException>(() => actual.Render(new float[64], new float[64]));
            SameAudio(actual, expected); Assert.That(oldCalls, Is.EqualTo(1)); Assert.That(newCalls, Is.EqualTo(1));
        }

        [TestCase(false)] [TestCase(true)]
        public void PlaybackChangesAfterHookReturnsStartReplacementAtItsFirstEvent(bool stopFirst)
        {
            var actual = Sequence(); var requested = false;
            actual.OnSendMessage = (s, channel, command, a, b) => { requested = true; s.ProcessMidiMessage(channel, command, a, b); };
            actual.Render(new float[65], new float[65]); Assert.That(requested, Is.True);
            if (stopFirst) { actual.Stop(); Assert.That(actual.Synthesizer.ActiveVoiceCount, Is.Zero); }
            var replacement = Midi(MidiFileLoopType.None, Track((0, new byte[] { 0x90, 45, 100 }),
                (0, new byte[] { 0x90, 72, 80 }), (96, new byte[] { 0xFF, 0x2F, 0 })));
            var expected = Sequence(); expected.Play(replacement, false); actual.Play(replacement, false);
            var keys = new List<int>();
            actual.OnSendMessage = (s, channel, command, a, b) => { keys.Add(a); s.ProcessMidiMessage(channel, command, a, b); };
            SameAudio(actual, expected); Assert.That(keys, Is.EqualTo(new[] { 45, 72 }));
        }

        [Test]
        public void ExceptionAfterRenderedPrefixPreservesClockAndUnsentEvents()
        {
            var midi = Midi(MidiFileLoopType.None, Track((0, new byte[] { 0x90, 60, 100 }),
                (2, new byte[] { 0x90, 67, 80 }), (96, new byte[] { 0xFF, 0x2F, 0 })));
            var actual = Sequence(); var expected = Sequence(); actual.Play(midi, false); expected.Play(midi, false);
            var calls = 0;
            actual.OnSendMessage = (s, channel, command, a, b) =>
            { calls++; s.ProcessMidiMessage(channel, command, a, b); if (a == 67) throw new ApplicationException("Later block"); };
            var left = new float[257]; var right = new float[257];
            Assert.Throws<ApplicationException>(() => actual.Render(left, right));
            var a = new float[128]; var b = new float[128]; expected.Render(a, b);
            Assert.That(left.AsSpan(0, 128).ToArray(), Is.EqualTo(a)); Assert.That(right.AsSpan(0, 128).ToArray(), Is.EqualTo(b));
            Assert.That(actual.Position, Is.EqualTo(expected.Position));
            SameAudio(actual, expected); Assert.That(calls, Is.EqualTo(2));
        }

        [TestCase(0F)] [TestCase(.5F)] [TestCase(2F)]
        public void SpeedAndModifiedMidiRemainSupportedInsideHook(float speed)
        {
            var actual = Sequence(); var expected = Sequence(); expected.Speed = speed;
            actual.OnSendMessage = (s, channel, command, a, b) =>
            { actual.Speed = speed; s.ProcessMidiMessage(channel, command, a + 12, b / 2); };
            expected.OnSendMessage = (s, channel, command, a, b) => s.ProcessMidiMessage(channel, command, a + 12, b / 2);
            SameAudio(actual, expected);
        }

        [Test]
        public void InvalidRenderArgumentsDoNotLeaveGuardHeld()
        {
            var actual = Sequence(); var expected = Sequence();
            Assert.Throws<ArgumentException>(() => actual.Render(new float[1], new float[2]));
            SameAudio(actual, expected);
        }

        [TestCase(MidiFileLoopType.None, -1, -1)]
        [TestCase(MidiFileLoopType.RpgMaker, 111, -1)]
        [TestCase(MidiFileLoopType.FinalFantasy, 116, 117)]
        [TestCase(MidiFileLoopType.IncredibleMachine, 110, 111)]
        public void HookExceptionResumesAcrossLoopsWithoutSkippingOrDuplicatingInitialNotes(MidiFileLoopType type, int start, int end)
        {
            var note = new byte[] { 0x90, 60, 100 }; var eot = new byte[] { 0xFF, 0x2F, 0 };
            var track = start < 0 ? Track((0, note), (96, eot)) : end < 0
                ? Track((0, new byte[] { 0xB0, (byte)start, 0 }), (0, note), (96, eot))
                : Track((0, new byte[] { 0xB0, (byte)start, 0 }), (0, note),
                    (96, new byte[] { 0xB0, (byte)end, 0 }), (0, eot));
            var midi = Midi(type, track); var actual = Sequence(); var expected = Sequence();
            actual.Play(midi, true); expected.Play(midi, true);
            var actualKeys = new List<int>(); var expectedKeys = new List<int>(); var failed = false;
            actual.OnSendMessage = (s, channel, command, a, b) =>
            {
                actualKeys.Add(a); s.ProcessMidiMessage(channel, command, a, b);
                if (!failed) { failed = true; throw new ApplicationException("Fail once at first loop"); }
            };
            expected.OnSendMessage = (s, channel, command, a, b) => { expectedKeys.Add(a); s.ProcessMidiMessage(channel, command, a, b); };
            Assert.Throws<ApplicationException>(() => actual.Render(new float[64], new float[64]));
            SameAudio(actual, expected, 16000);
            Assert.That(actualKeys, Is.EqualTo(expectedKeys)); Assert.That(actualKeys.Count, Is.GreaterThan(2));
            actual.Stop(); Assert.That(actual.EndOfSequence, Is.True); Assert.That(actual.Synthesizer.ActiveVoiceCount, Is.Zero);
        }
    }
}
