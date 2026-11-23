// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class AssetPreflightTest
    {
        [TestCase("valid")]
        [TestCase("numeric-generator")]
        [TestCase("budget")]
        [TestCase("truncated")]
        [TestCase("unknown-profile-field")]
        [TestCase("invalid-profile")]
        [TestCase("oversized-profile")]
        public void OnlyFullyValidatedTargetAssetsPublishAReceipt(string scenario)
        {
            var assets = Path.Combine(AppContext.BaseDirectory, "preflight-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(assets);
            var font = scenario == "numeric-generator" ? NumericAssetBoundaryTest.Font(51, 32767) : MidiLoopTestAssets.SoundFontBytes(); var midi = SynthesisBaselineScenario.MidiBytes();
            File.WriteAllBytes(Path.Combine(assets, "Original.sf2"), font);
            File.WriteAllBytes(Path.Combine(assets, "Original.mid"), scenario == "truncated" ? midi[..^1] : midi);
            var profile = Path.Combine(assets, "profile.json");
            var json = scenario switch
            {
                "invalid-profile" => "{\"maxMidiEvents\":0}",
                "oversized-profile" => new string(' ', 16385),
                "budget" => "{\"maxDecodedSampleBytes\":2}",
                "unknown-profile-field" => "{\"typoBudget\":1}",
                _ => "{\"maxFileBytes\":1048576,\"maxMidiDuration\":\"00:00:02\"}"
            };
            File.WriteAllText(profile, json);
            var run = "preflight-" + Guid.NewGuid().ToString("N");
            var paths = ToolPaths.FromEnvironment(assets, "Original.sf2", "Original.mid", run);
            var args = new[] { "validate", "--assets", assets, "--soundfont", "Original.sf2", "--midi", "Original.mid", "--profile", profile, "--run", run };
            var receipt = Path.Combine(paths.OutputDirectory, "assets.validated.json");
            if (scenario == "valid")
            {
                Assert.That(WorkspaceCommand.Run(args, TextWriter.Null), Is.Zero);
                using var result = JsonDocument.Parse(File.ReadAllText(receipt));
                Assert.That(result.RootElement.GetProperty("soundFont").GetProperty("sha256").GetString(), Is.EqualTo(Convert.ToHexString(SHA256.HashData(font))));
                Assert.That(result.RootElement.GetProperty("midi").GetProperty("sha256").GetString(), Is.EqualTo(Convert.ToHexString(SHA256.HashData(midi))));
                Assert.That(result.RootElement.GetProperty("limits").GetProperty("MaxFileBytes").GetInt64(), Is.EqualTo(1048576));
                var original = File.ReadAllBytes(receipt);
                Assert.Throws<IOException>(() => WorkspaceCommand.Run(args, TextWriter.Null));
                Assert.That(File.ReadAllBytes(receipt), Is.EqualTo(original));
            }
            else
            {
                if (scenario == "unknown-profile-field") Assert.Throws<JsonException>(() => WorkspaceCommand.Run(args, TextWriter.Null));
                else if (scenario == "invalid-profile") Assert.Throws<ArgumentOutOfRangeException>(() => WorkspaceCommand.Run(args, TextWriter.Null));
                else Assert.Throws<InvalidDataException>(() => WorkspaceCommand.Run(args, TextWriter.Null));
                Assert.That(File.Exists(receipt), Is.False);
                Assert.That(Directory.Exists(paths.OutputDirectory), Is.False);
            }
            Assert.That(File.Exists(receipt + ".partial"), Is.False);
            using (File.Open(Path.Combine(assets, "Original.sf2"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            using (File.Open(Path.Combine(assets, "Original.mid"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }
    }
}
