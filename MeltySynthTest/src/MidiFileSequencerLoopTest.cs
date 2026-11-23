using System;
using System.Collections.Generic;
using MeltySynth;
using NUnit.Framework;
using static MeltySynthTest.MidiLoopTestAssets;

namespace MeltySynthTest
{
    public class MidiFileSequencerLoopTest
    {
        private static readonly byte[] Note = { 0x90, 60, 100 };
        private static readonly byte[] End = { 0xFF, 0x2F, 0 };
        private static byte[] Control(int controller) => new byte[] { 0xB0, (byte)controller, 0 };
        private static Synthesizer Synth(SoundFont font) => new Synthesizer(font, new SynthesizerSettings(32000) { EnableReverbAndChorus = false, MaximumPolyphony = 8 });

        [TestCase(MidiFileLoopType.FinalFantasy, 116, 117)]
        [TestCase(MidiFileLoopType.IncredibleMachine, 110, 111)]
        public void SameTickExplicitLoopIsRejectedBeforeRendering(MidiFileLoopType type, int start, int end)
        {
            var midi = Midi(type, Track((0, Control(start)), (0, Note), (0, Control(end)), (96, End)));
            var sequencer = new MidiFileSequencer(Synth(SoundFont()));
            // Assert at Play, not Render: a regression fails immediately instead of hanging a worker.
            var error = Assert.Throws<ArgumentException>(() => sequencer.Play(midi, true));
            Assert.That(error!.ParamName, Is.EqualTo("midiFile"));
            Assert.That(sequencer.MidiFile, Is.Null);
        }

        [TestCase(MidiFileLoopType.None, -1)]
        [TestCase(MidiFileLoopType.RpgMaker, 111)]
        [TestCase(MidiFileLoopType.FinalFantasy, 116)]
        [TestCase(MidiFileLoopType.IncredibleMachine, 110)]
        public void ZeroDurationEndOfSequenceLoopIsRejected(MidiFileLoopType type, int start)
        {
            var events = start < 0 ? Track((0, Note), (0, End)) : Track((0, Note), (96, Control(start)), (0, End));
            var midi = Midi(type, events);
            var sequencer = new MidiFileSequencer(Synth(SoundFont()));
            Assert.Throws<ArgumentException>(() => sequencer.Play(midi, true));
        }

        [Test]
        public void SameTickLoopAcrossTracksIsRejected()
        {
            var midi = Midi(MidiFileLoopType.FinalFantasy,
                Track((96, Control(116)), (0, Note), (96, End)),
                Track((96, Control(117)), (96, End)));
            var sequencer = new MidiFileSequencer(Synth(SoundFont()));
            Assert.Throws<ArgumentException>(() => sequencer.Play(midi, true));
        }

        [Test]
        public void EmptySequenceIsRejectedForLooping()
        {
            var sequencer = new MidiFileSequencer(Synth(SoundFont()));
            Assert.Throws<ArgumentException>(() => sequencer.Play(Midi(MidiFileLoopType.None, Track((0, End))), true));
        }

        [Test]
        public void RejectedReplacementPreservesExistingPlayback()
        {
            var synth = Synth(SoundFont());
            var sequencer = new MidiFileSequencer(synth);
            var valid = Midi(MidiFileLoopType.None, Track((0, Note), (96, End)));
            sequencer.Play(valid, true);
            sequencer.Render(new float[512], new float[512]);
            var time = sequencer.Position;
            var voices = synth.ActiveVoiceCount;
            Assert.Throws<ArgumentException>(() => sequencer.Play(Midi(MidiFileLoopType.None, Track((0, End))), true));
            Assert.That(sequencer.MidiFile, Is.SameAs(valid));
            Assert.That(sequencer.Position, Is.EqualTo(time));
            Assert.That(synth.ActiveVoiceCount, Is.EqualTo(voices));
            sequencer.Render(new float[64], new float[64]);
            Assert.That(sequencer.Position, Is.GreaterThan(time));
        }

        [TestCase(MidiFileLoopType.FinalFantasy, 116, 117)]
        [TestCase(MidiFileLoopType.IncredibleMachine, 110, 111)]
        [TestCase(MidiFileLoopType.RpgMaker, 111, -1)]
        [TestCase(MidiFileLoopType.None, -1, -1)]
        public void PositiveLoopPreservesTimingAndReleaseTail(MidiFileLoopType type, int start, int end)
        {
            var track = start < 0 ? Track((0, Note), (96, End)) : end < 0
                ? Track((0, Control(start)), (0, Note), (96, End))
                : Track((0, Control(start)), (0, Note), (96, Control(end)), (0, End));
            var font = SoundFont();
            var synth = Synth(font);
            var reference = Synth(font);
            var sequencer = new MidiFileSequencer(synth);
            var notes = 0;
            var maxVoices = 0;
            sequencer.OnSendMessage = (s, channel, command, a, b) =>
            {
                if (command == 0x90) { notes++; if (notes > 4) throw new InvalidOperationException("Unexpected unbounded loop"); }
                s.ProcessMidiMessage(channel, command, a, b);
            };
            sequencer.Play(Midi(type, track), true);
            reference.Reset();
            var left = new float[64]; var right = new float[64];
            var expectedLeft = new float[64]; var expectedRight = new float[64];
            // 96 ticks at 480 PPQ and 120 BPM = 100 ms = 50 blocks at 32 kHz.
            for (var block = 0; block < 160; block++)
            {
                var wrap = block > 0 && block % 50 == 0;
                if (wrap) reference.NoteOffAll(false);
                // Explicit ends replay immediately; end-of-sequence replay starts next block.
                if (block == 0 || (end >= 0 && wrap) || (end < 0 && block > 1 && block % 50 == 1))
                    reference.NoteOn(0, 60, 100);
                reference.Render(expectedLeft, expectedRight);
                sequencer.Render(left, right);
                Assert.That(left, Is.EqualTo(expectedLeft).Within(1E-6));
                Assert.That(right, Is.EqualTo(expectedRight).Within(1E-6));
                maxVoices = Math.Max(maxVoices, synth.ActiveVoiceCount);
                Assert.That(notes, Is.EqualTo(end >= 0 ? block / 50 + 1 : block == 0 ? 1 : (block - 1) / 50 + 1));
            }
            Assert.That(maxVoices, Is.GreaterThan(1), "The old note must release rather than being killed on wrap.");
        }

        [Test]
        public void PositiveLoopRetainsItsIntroAndReplaysOnlyTheLoopRegion()
        {
            var midi = Midi(MidiFileLoopType.FinalFantasy, Track(
                (0, new byte[] { 0x90, 72, 100 }), (96, new byte[] { 0x80, 72, 0 }),
                (0, Control(116)), (0, Note), (96, Control(117)), (0, End)));
            var sequencer = new MidiFileSequencer(Synth(SoundFont()));
            var notes = new List<int>();
            var times = new List<TimeSpan>();
            sequencer.OnSendMessage = (synth, channel, command, key, velocity) =>
            {
                if (command == 0x90)
                {
                    if (notes.Count >= 4) throw new InvalidOperationException("Unexpected unbounded loop");
                    notes.Add(key); times.Add(sequencer.Position);
                }
                synth.ProcessMidiMessage(channel, command, key, velocity);
            };
            sequencer.Play(midi, true);
            sequencer.Render(new float[64 * 160], new float[64 * 160]);
            Assert.That(notes, Is.EqualTo(new[] { 72, 60, 60, 60 }));
            Assert.That(times, Is.EqualTo(new[] { TimeSpan.Zero, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100) }));
        }

        [Test]
        public void UnreachableTailDoesNotInvalidateFirstExplicitLoop()
        {
            var midi = Midi(MidiFileLoopType.FinalFantasy, Track((0, Control(116)), (0, Note), (96, Control(117)), (0, Control(116)), (0, End)));
            var sequencer = new MidiFileSequencer(Synth(SoundFont()));
            Assert.DoesNotThrow(() => sequencer.Play(midi, true));
        }

        [Test]
        public void LoopMarkersAreIgnoredWhenLoopingIsDisabled()
        {
            var midi = Midi(MidiFileLoopType.FinalFantasy, Track((0, Control(116)), (0, Note), (0, Control(117)), (0, End)));
            var sequencer = new MidiFileSequencer(Synth(SoundFont()));
            Assert.DoesNotThrow(() => sequencer.Play(midi, false));
            sequencer.Render(new float[64], new float[64]);
            Assert.That(sequencer.EndOfSequence, Is.True);
        }
    }
}
