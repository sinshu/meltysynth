// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;

// Tool configuration is resolved once; output paths are independent of the current directory.
public sealed class ToolPaths
{
    public string AssetDirectory { get; }
    public string OutputDirectory { get; }
    public string SoundFontName { get; }
    public string MidiName { get; }

    public ToolPaths(string assetDirectory, string outputDirectory, string soundFontName = "TimGM6mb.sf2", string midiName = "flourish.mid")
    {
        CheckName(soundFontName); CheckName(midiName);
        SoundFontName = soundFontName; MidiName = midiName;
        AssetDirectory = Path.GetFullPath(assetDirectory);
        OutputDirectory = Path.GetFullPath(outputDirectory);
    }

    public static ToolPaths FromEnvironment(string? assets = null, string soundFontName = "TimGM6mb.sf2", string midiName = "flourish.mid", string run = "latest")
    {
        CheckName(run);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "MeltySynth.slnx"))) root = root.Parent;
        var repository = root?.FullName
            ?? throw new DirectoryNotFoundException("Run the Workspace build from this checkout.");
        return new ToolPaths(assets ?? Environment.GetEnvironmentVariable("MELTYSYNTH_TEST_ASSETS")
            ?? Path.Combine(repository, "MeltySynthTest", "ExternalAssets"), Path.Combine(repository, "bin", "workspace", run), soundFontName, midiName);
    }

    public string Asset(string name)
    {
        CheckName(name);
        var path = Path.Combine(AssetDirectory, name);
        return File.Exists(path) ? path : throw new FileNotFoundException("Missing asset; use --assets or MELTYSYNTH_TEST_ASSETS.", path);
    }

    public string Output(string name)
    {
        CheckName(name);
        Directory.CreateDirectory(OutputDirectory);
        return Path.Combine(OutputDirectory, name);
    }

    private static void CheckName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            throw new ArgumentException("Use an asset/output filename, without directory components.", nameof(name));
    }
}
