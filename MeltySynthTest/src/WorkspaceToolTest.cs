using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MeltySynth;
using NAudio.Wave;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class WorkspaceToolTest
    {
        [TestCase("chord", 1)]
        [TestCase("examples", 5)]
        [TestCase("benchmark", 1)]
        public void OfflineCommandsProduceFiniteNonSilentStereoInBin(string command, int waveCount)
        {
            var assets = Path.Combine(AppContext.BaseDirectory, "workspace-fixtures");
            Directory.CreateDirectory(assets);
            File.WriteAllBytes(Path.Combine(assets, "Original.sf2"), MidiLoopTestAssets.SoundFontBytes());
            File.WriteAllBytes(Path.Combine(assets, "Original.mid"), SynthesisBaselineScenario.MidiBytes());
            var run = "test-" + command + "-" + Guid.NewGuid().ToString("N");
            var arguments = new[] { command, "--assets", assets, "--soundfont", "Original.sf2", "--midi", "Original.mid", "--run", run };
            if (command == "benchmark") arguments = arguments.Concat(new[] { "--seconds", "1" }).ToArray();
            using var log = new StringWriter();
            Assert.That(WorkspaceCommand.Run(arguments, log), Is.Zero);
            var paths = ToolPaths.FromEnvironment(assets, "Original.sf2", "Original.mid", run);
            var waves = Directory.GetFiles(paths.OutputDirectory, "*.wav");
            Assert.That(waves.Length, Is.EqualTo(waveCount));
            foreach (var path in waves)
            {
                using var reader = new WaveFileReader(path);
                Assert.That(reader.WaveFormat.Channels, Is.EqualTo(2));
                Assert.That(reader.WaveFormat.SampleRate, Is.EqualTo(44100));
                Assert.That(reader.WaveFormat.BitsPerSample, Is.EqualTo(16));
                Assert.That(reader.Length, Is.GreaterThan(0));
                double energy = 0;
                float[]? frame;
                while ((frame = reader.ReadNextSampleFrame()) != null)
                    foreach (var sample in frame) { Assert.That(float.IsFinite(sample), Is.True); energy += sample * sample; }
                Assert.That(energy, Is.GreaterThan(0), path);
            }
            if (command == "chord")
            {
                var synth = new Synthesizer(Path.Combine(assets, "Original.sf2"), 44100);
                foreach (var note in new[] { 60, 64, 67 }) synth.NoteOn(0, note, 100);
                var left = new float[3 * 44100]; var right = new float[left.Length];
                synth.Render(left, right);
                var gain = .99F / Math.Max(left.Max(x => Math.Abs(x)), right.Max(x => Math.Abs(x)));
                using var reader = new WaveFileReader(waves[0]);
                Assert.That(reader.SampleCount, Is.EqualTo(left.Length));
                double maxError = 0;
                for (var i = 0; i < left.Length; i++)
                {
                    var frame = reader.ReadNextSampleFrame() ?? throw new AssertionException("Truncated WAV");
                    maxError = Math.Max(maxError, Math.Abs(frame[0] - gain * left[i]));
                    maxError = Math.Max(maxError, Math.Abs(frame[1] - gain * right[i]));
                }
                Assert.That(maxError, Is.LessThanOrEqualTo(2.0 / 32768), "PCM quantization tolerance");
            }
            if (command == "benchmark")
            {
                using var receipt = JsonDocument.Parse(File.ReadAllText(paths.Output("benchmark.json")));
                Assert.That(receipt.RootElement.GetProperty("frames").GetInt32(), Is.EqualTo(44100));
                Assert.That(receipt.RootElement.GetProperty("renderMilliseconds").GetDouble(), Is.GreaterThan(0));
            }
            // A second exclusive open verifies writers/readers have released their files.
            foreach (var wave in waves) using (File.Open(wave, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }

        [TestCase(0)]
        [TestCase(64)]
        public void EmptyAndSilentWavesRemainZeroAndReleaseTheirWriter(int frames)
        {
            var paths = ToolPaths.FromEnvironment(run: "silence-" + Guid.NewGuid().ToString("N"));
            var path = paths.Output("silence.wav");
            Examples.WriteWaveFile(new float[frames], new float[frames], 44100, path);
            using (var reader = new WaveFileReader(path))
            {
                Assert.That(reader.Length, Is.EqualTo(frames * 4));
                var data = new byte[frames * 4];
                Assert.That(reader.Read(data, 0, data.Length), Is.EqualTo(data.Length));
                Assert.That(data, Is.All.Zero);
            }
            using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }

        [TestCase("../escape")]
        [TestCase("/absolute")]
        [TestCase("C:\\escape")]
        public void OutputNamesCannotEscapeTheRunDirectory(string name)
        {
            var paths = ToolPaths.FromEnvironment();
            Assert.Throws<ArgumentException>(() => paths.Output(name));
        }

        [Test]
        public void MissingInputsAndInvalidOptionsFailBeforeOutputCreation()
        {
            var run = "missing-" + Guid.NewGuid().ToString("N");
            var paths = ToolPaths.FromEnvironment(Path.Combine(AppContext.BaseDirectory, run), run: run);
            Assert.Throws<FileNotFoundException>(() => WorkspaceCommand.Run(new[] { "chord", "--assets", paths.AssetDirectory, "--run", run }, TextWriter.Null));
            Assert.That(Directory.Exists(paths.OutputDirectory), Is.False);
            Assert.Throws<ArgumentException>(() => WorkspaceCommand.Run(new[] { "benchmark", "--seconds", "0" }, TextWriter.Null));
            Assert.Throws<ArgumentException>(() => WorkspaceCommand.Run(new[] { "typo" }, TextWriter.Null));
        }
    }
}
