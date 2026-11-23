using System;
using System.IO;
using System.Runtime.InteropServices;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class OriginalFixtureReferenceTest
    {
        [Test]
        public void OriginalSoundFontAgreesWithIndependentNaudioReader()
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes(completeInfo: true);
            using var stream = new MemoryStream(bytes, false);
            var reference = new NAudio.SoundFont.SoundFont(stream);
            using var actualStream = new MemoryStream(bytes, false);
            var actual = new SoundFont(actualStream);
            Assert.That(reference.SampleHeaders.Length, Is.EqualTo(1));
            var sample = reference.SampleHeaders[0];
            Assert.That(sample.Start, Is.EqualTo(actual.SampleHeaders[0].Start));
            Assert.That(sample.End, Is.EqualTo(actual.SampleHeaders[0].End));
            Assert.That(sample.StartLoop, Is.EqualTo(actual.SampleHeaders[0].StartLoop));
            Assert.That(sample.EndLoop, Is.EqualTo(actual.SampleHeaders[0].EndLoop));
            Assert.That(sample.SampleRate, Is.EqualTo(actual.SampleHeaders[0].SampleRate));
            Assert.That(reference.Instruments.Length, Is.EqualTo(actual.Instruments.Count));
            Assert.That(reference.Instruments[0].Name, Is.EqualTo(actual.Instruments[0].Name));
            Assert.That(reference.Presets.Length, Is.EqualTo(actual.Presets.Count));
            Assert.That(reference.Presets[0].Name, Is.EqualTo(actual.Presets[0].Name));
            // NAudio 2.3 includes the 12-byte sdta/LIST header in SampleData.
            var pcm = MemoryMarshal.Cast<byte, short>(reference.SampleData.AsSpan()).Slice(6);
            Assert.That(pcm.ToArray(), Is.EqualTo(actual.WaveData.ToArray()));
        }

        [Test]
        public void OriginalTempoTrackHasKnownTimingAndSurvivesSourceDisposal()
        {
            var bytes = MidiLoopTestAssets.MidiBytes(MidiLoopTestAssets.Track(
                (0, new byte[] { 0xFF, 0x51, 3, 7, 161, 32 }), // 500,000 microseconds/quarter.
                (480, new byte[] { 0x90, 60, 100 }),
                (0, new byte[] { 0xFF, 0x51, 3, 15, 66, 64 }), // 1,000,000 microseconds/quarter.
                (480, new byte[] { 0x80, 60, 0 }), (0, new byte[] { 0xFF, 0x2F, 0 })));
            MidiFile midi;
            using (var stream = new MemoryStream(bytes, false))
            {
                midi = new MidiFile(stream);
                Assert.That(stream.CanRead, Is.True);
                Assert.That(stream.Position, Is.EqualTo(stream.Length));
            }
            Assert.That(midi.Length.Ticks, Is.EqualTo(15000000));
            Assert.That(midi.Times[0].Ticks, Is.EqualTo(5000000));
            Assert.That(midi.Times[1].Ticks, Is.EqualTo(15000000));
            var sequencer = new MidiFileSequencer(SynthesisBaselineScenario.Synth(false));
            Assert.DoesNotThrow(() => { sequencer.Play(midi, false); sequencer.Render(new float[64], new float[64]); });
        }
    }
}
