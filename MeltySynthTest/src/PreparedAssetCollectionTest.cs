// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class PreparedAssetCollectionTest
    {
        [Test]
        public void InstrumentRegionArrayCannotBeClearedThroughPublicView()
        {
            var font = MidiLoopTestAssets.SoundFont();
            var synth = new Synthesizer(font, 32000);
            Assert.Throws<InvalidCastException>(() => Array.Clear((InstrumentRegion[])font.Instruments[0].Regions));
            synth.NoteOn(0, 60, 100); Assert.That(synth.ActiveVoiceCount, Is.GreaterThan(0));
        }

        private static void Check<T>(IReadOnlyList<T> view, T[] expected) where T : class
        {
            Assert.That(view, Is.Not.InstanceOf<T[]>());
            var original = expected.ToArray();
            Assert.That(view.Count, Is.EqualTo(original.Length));
            for (var i = 0; i < original.Length; i++) Assert.That(view[i], Is.SameAs(original[i]));
            Assert.That(view, Is.Not.InstanceOf<IList<T>>());
            Assert.That(view, Is.Not.InstanceOf<ICollection<T>>());
            Assert.That(view, Is.Not.InstanceOf<IList>());
            Assert.That(view, Is.Not.InstanceOf<ICollection>());
            Assert.Throws<InvalidCastException>(() => ((IList<T>)view)[0] = null!);
            Assert.Throws<InvalidCastException>(() => ((IList<T>)view).Clear());
            Assert.Throws<InvalidCastException>(() => ((ICollection<T>)view).Add(original[0]));
            Assert.Throws<InvalidCastException>(() => ((IList)view)[0] = null);
            Assert.Throws<InvalidCastException>(() => ((IList)view).Clear());
            Assert.Throws<InvalidCastException>(() => { _ = ((ICollection)view).SyncRoot; });
            // A caller-created copy is independent, never a mutable alias to owned storage.
            var copy = view.ToArray(); Array.Clear(copy);
            Assert.That(expected, Is.EqualTo(original));
            Assert.That(view, Is.EqualTo(original));
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = ((IReadOnlyList<T>)expected)[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = ((IReadOnlyList<T>)expected)[expected.Length]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = view[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = view[view.Count]; });
        }

        [TestCase("samples")] [TestCase("instruments")] [TestCase("presets")]
        [TestCase("instrument-regions")] [TestCase("preset-regions")]
        public void PublicCollectionsRejectAllMutableInterfaces(string kind)
        {
            var font = MultiFont();
            switch (kind)
            {
                case "samples": Check(font.SampleHeaders, font.SampleHeaderArray); break;
                case "instruments": Check(font.Instruments, font.InstrumentArray); break;
                case "presets": Check(font.Presets, font.PresetArray); break;
                case "instrument-regions": Check(font.Instruments[0].Regions, font.InstrumentArray[0].RegionArray); break;
                case "preset-regions": Check(font.Presets[0].Regions, font.PresetArray[0].RegionArray); break;
            }
        }

        // Original multi-entry bank: two samples, instruments and presets, two regions
        // each. Reverse the second preset's instrument order to exercise real ordering.
        private static SoundFont MultiFont()
        {
            var bytes = MidiLoopTestAssets.SoundFontBytes();
            int Chunk(byte[] source, string id) => source.AsSpan().IndexOf(Encoding.ASCII.GetBytes(id)) + 8;
            void Word(byte[] target, int at, int value) => BinaryPrimitives.WriteUInt16LittleEndian(target.AsSpan(at), (ushort)value);
            void Name(byte[] target, int at, string name) { target.AsSpan(at, 20).Clear(); Encoding.ASCII.GetBytes(name).CopyTo(target, at); }
            void Replace(string id, byte[] data)
            {
                var at = Chunk(bytes, id); var size = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(at - 4));
                var delta = data.Length - size; var result = new byte[bytes.Length + delta];
                bytes.AsSpan(0, at).CopyTo(result); data.CopyTo(result, at);
                bytes.AsSpan(at + size).CopyTo(result.AsSpan(at + data.Length));
                BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(at - 4), data.Length);
                var pdta = bytes.AsSpan().IndexOf(Encoding.ASCII.GetBytes("pdta"));
                BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(pdta - 4), BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(pdta - 4)) + delta);
                BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), result.Length - 8); bytes = result;
            }
            var samples = new byte[46 * 3];
            var header = bytes.AsSpan(Chunk(bytes, "shdr"), 46).ToArray();
            for (var i = 0; i < 2; i++) { header.CopyTo(samples, i * 46); Name(samples, i * 46, "Tone " + i); samples[i * 46 + 40] = (byte)(60 + i * 12); }
            Name(samples, 92, "EOS"); Replace("shdr", samples);
            var instruments = new byte[22 * 3];
            for (var i = 0; i < 3; i++) { Name(instruments, i * 22, i == 2 ? "EOI" : "Instrument " + i); Word(instruments, i * 22 + 20, i * 2); }
            Replace("inst", instruments);
            var presets = new byte[38 * 3];
            for (var i = 0; i < 3; i++)
            {
                Name(presets, i * 38, i == 2 ? "EOP" : "Preset " + i);
                Word(presets, i * 38 + 20, i == 1 ? 7 : 0); Word(presets, i * 38 + 22, i == 1 ? 1 : 0);
                Word(presets, i * 38 + 24, i * 2);
            }
            Replace("phdr", presets);
            var pbags = new byte[5 * 4]; var ibags = new byte[5 * 4];
            for (var i = 0; i < 5; i++) { Word(pbags, i * 4, i); Word(ibags, i * 4, 3 * i); }
            Replace("pbag", pbags); Replace("ibag", ibags);
            var pgens = new byte[5 * 4]; var igens = new byte[13 * 4];
            for (var i = 0; i < 4; i++)
            {
                Word(pgens, i * 4, 41); Word(pgens, i * 4 + 2, i is 0 or 3 ? 0 : 1);
                Word(igens, i * 12, 38); Word(igens, i * 12 + 2, unchecked((ushort)-3600));
                Word(igens, i * 12 + 4, 54); Word(igens, i * 12 + 6, 1);
                Word(igens, i * 12 + 8, 53); Word(igens, i * 12 + 10, i % 2);
            }
            Replace("pgen", pgens); Replace("igen", igens);
            return new SoundFont(new MemoryStream(bytes, false));
        }

        private static void StableAccess<T>(Func<IReadOnlyList<T>> get) where T : class
        {
            var first = get(); var item = first[0]; var stable = true; var sum = 0;
            for (var i = 0; i < 1000; i++) { _ = get()[0]; }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++)
            {
                var view = get(); stable &= ReferenceEquals(first, view) && ReferenceEquals(item, view[0]); sum += view.Count;
            }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(stable, Is.True); Assert.That(sum, Is.EqualTo(10000 * first.Count)); Assert.That(bytes, Is.Zero);
        }

        [TestCase("samples")] [TestCase("instruments")] [TestCase("presets")]
        [TestCase("instrument-regions")] [TestCase("preset-regions")]
        public void RepeatedPublicAccessReusesThePreparedViewWithoutAllocating(string kind)
        {
            var font = MultiFont();
            switch (kind)
            {
                case "samples": StableAccess(() => font.SampleHeaders); break;
                case "instruments": StableAccess(() => font.Instruments); break;
                case "presets": StableAccess(() => font.Presets); break;
                case "instrument-regions": StableAccess(() => font.Instruments[0].Regions); break;
                case "preset-regions": StableAccess(() => font.Presets[0].Regions); break;
            }
        }

        [Test]
        public void RegionOrderIdentityAndPatchLookupArePreserved()
        {
            var font = MultiFont();
            Assert.That(font.SampleHeaders.Select(s => s.Name), Is.EqualTo(new[] { "Tone 0", "Tone 1" }));
            Assert.That(font.Instruments.Select(s => s.Name), Is.EqualTo(new[] { "Instrument 0", "Instrument 1" }));
            Assert.That(font.Presets.Select(s => s.Name), Is.EqualTo(new[] { "Preset 0", "Preset 1" }));
            foreach (var instrument in font.Instruments)
                for (var i = 0; i < 2; i++) Assert.That(instrument.Regions[i].Sample, Is.SameAs(font.SampleHeaders[i]));
            for (var p = 0; p < 2; p++)
                for (var r = 0; r < 2; r++) Assert.That(font.Presets[p].Regions[r].Instrument, Is.SameAs(font.Instruments[p == 0 ? r : 1 - r]));
            Assert.That(font.Presets[1].BankNumber, Is.EqualTo(1)); Assert.That(font.Presets[1].PatchNumber, Is.EqualTo(7));
            var synth = new Synthesizer(font, 32000);
            synth.ProcessMidiMessage(0, 0xB0, 0, 1); synth.ProcessMidiMessage(0, 0xC0, 7, 0); synth.NoteOn(0, 60, 100);
            Assert.That(synth.ActiveVoiceCount, Is.EqualTo(4));
            Assert.That(synth.SoundFont, Is.SameAs(font));
        }

        [Test]
        public void DefaultEmptyRegionViewsAreStableAndNotArrays()
        {
            var instrument = Instrument.Default.Regions; var preset = Preset.Default.Regions;
            Assert.That(instrument, Is.SameAs(Instrument.Default.Regions)); Assert.That(preset, Is.SameAs(Preset.Default.Regions));
            Assert.That(instrument, Is.Empty); Assert.That(preset, Is.Empty);
            Assert.That(instrument, Is.Not.InstanceOf<InstrumentRegion[]>()); Assert.That(preset, Is.Not.InstanceOf<PresetRegion[]>());
            Assert.That(instrument, Is.Not.InstanceOf<IList>()); Assert.That(preset, Is.Not.InstanceOf<IList>());
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = instrument[0]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = preset[0]; });
        }

        [Test]
        public void WarmNoteAndRenderPathDoesNotAllocateOrRebuildViews()
        {
            var font = MultiFont(); var synth = new Synthesizer(font, new SynthesizerSettings(32000) { MaximumPolyphony = 8, EnableReverbAndChorus = false });
            var left = new float[64]; var right = new float[64]; var view = font.Presets;
            for (var i = 0; i < 1000; i++) { synth.NoteOn(0, 60, 100); synth.Render(left, right); synth.NoteOffAll(true); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++) { synth.NoteOn(0, 60, 100); synth.Render(left, right); synth.NoteOffAll(true); }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.Zero); Assert.That(font.Presets, Is.SameAs(view));
        }

        private static (float[] Left, float[] Right) OwnerScenario(SoundFont font, int owner, bool effects)
        {
            var synth = new Synthesizer(font, new SynthesizerSettings(owner % 2 == 0 ? 32000 : 48000)
            { MaximumPolyphony = 8, EnableReverbAndChorus = effects });
            var left = new float[8192]; var right = new float[8192];
            synth.MasterVolume = .1F * (owner + 1);
            synth.ProcessMidiMessage(0, 0xB0, 0, owner % 2); synth.ProcessMidiMessage(0, 0xC0, owner % 2 == 0 ? 0 : 7, 0);
            synth.ProcessMidiMessage(0, 0xB0, 10, 20 + owner * 25); synth.NoteOn(0, 55 + owner, 70 + owner * 12);
            synth.Render(left.AsSpan(0, 2049), right.AsSpan(0, 2049));
            if (owner % 2 == 0) synth.Reset(); else synth.NoteOff(0, 55 + owner);
            synth.ProcessMidiMessage(0, 0xE0, owner * 20, 64); synth.NoteOn(0, 67 + owner, 90);
            synth.Render(left.AsSpan(2049), right.AsSpan(2049));
            return (left, right);
        }

        [TestCase(false)] [TestCase(true)]
        public async Task SeparateSynthOwnersCanRenderSharedPreparedAssetsIndependently(bool effects)
        {
            var shared = MultiFont(); var sample = shared.WaveData.ToArray();
            var originals = shared.Instruments.SelectMany(i => i.Regions).ToArray();
            var expected = Enumerable.Range(0, 4).Select(owner => OwnerScenario(MultiFont(), owner, effects)).ToArray();
            // Each task owns one entire synth; no concurrent calls on a mutable core.
            var tasks = Enumerable.Range(0, 4).Select(owner => Task.Run(() => OwnerScenario(shared, owner, effects))).ToArray();
            var actual = await Task.WhenAll(tasks);
            for (var i = 0; i < actual.Length; i++)
            {
                Assert.That(actual[i].Left, Is.EqualTo(expected[i].Left)); Assert.That(actual[i].Right, Is.EqualTo(expected[i].Right));
                Assert.That(actual[i].Left.Any(x => x != 0), Is.True);
            }
            Assert.That(shared.WaveData.ToArray(), Is.EqualTo(sample));
            Assert.That(shared.Instruments.SelectMany(i => i.Regions), Is.EqualTo(originals));
        }
    }
}
