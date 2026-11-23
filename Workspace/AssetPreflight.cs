using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeltySynth;

// Packaging-only validation reuses the runtime loaders and their immutable profile.
// No extra checks, copies or synthesis state are introduced in the render loop.
public static class AssetPreflight
{
    public static AssetLoadLimits ReadProfile(string path)
    {
        using var source = File.OpenRead(path);
        if (source.Length > 16384) throw new InvalidDataException("Loading profiles must fit within 16 KiB.");
        var profile = JsonSerializer.Deserialize<Profile>(source, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException("A loading profile must be a JSON object.");
        return new AssetLoadLimits(profile.MaxFileBytes, profile.MaxDecodedSampleBytes, profile.MaxMidiTrackBytes,
            profile.MaxMidiTracks, profile.MaxMidiEvents, profile.MaxSoundFontRecords, profile.MaxSoundFontRegions,
            profile.MaxMetadataBytes, profile.MaxMidiDuration, profile.AllowMidiEndOfTrackPadding);
    }

    private sealed class Profile
    {
        public long MaxFileBytes { get; init; } = AssetLoadLimits.Default.MaxFileBytes;
        public int MaxDecodedSampleBytes { get; init; } = AssetLoadLimits.Default.MaxDecodedSampleBytes;
        public int MaxMidiTrackBytes { get; init; } = AssetLoadLimits.Default.MaxMidiTrackBytes;
        public int MaxMidiTracks { get; init; } = AssetLoadLimits.Default.MaxMidiTracks;
        public int MaxMidiEvents { get; init; } = AssetLoadLimits.Default.MaxMidiEvents;
        public int MaxSoundFontRecords { get; init; } = AssetLoadLimits.Default.MaxSoundFontRecords;
        public int MaxSoundFontRegions { get; init; } = AssetLoadLimits.Default.MaxSoundFontRegions;
        public int MaxMetadataBytes { get; init; } = AssetLoadLimits.Default.MaxMetadataBytes;
        public TimeSpan MaxMidiDuration { get; init; } = AssetLoadLimits.Default.MaxMidiDuration;
        public bool AllowMidiEndOfTrackPadding { get; init; } = AssetLoadLimits.Default.AllowMidiEndOfTrackPadding;
    }

    public static object Validate(string fontPath, string midiPath, AssetLoadLimits limits)
    {
        using var fontSource = File.OpenRead(fontPath);
        using var midiSource = File.OpenRead(midiPath);
        var font = new SoundFont(fontSource, limits);
        var midi = new MidiFile(midiSource, limits);
        fontSource.Position = 0; midiSource.Position = 0;
        return new
        {
            schemaVersion = 1,
            limits,
            soundFont = new { name = Path.GetFileName(fontPath), bytes = fontSource.Length,
                sha256 = Convert.ToHexString(SHA256.HashData(fontSource)), decodedBytes = (long)font.WaveData.Length * sizeof(short),
                presets = font.Presets.Count, instruments = font.Instruments.Count },
            midi = new { name = Path.GetFileName(midiPath), bytes = midiSource.Length,
                sha256 = Convert.ToHexString(SHA256.HashData(midiSource)), durationTicks = midi.Length.Ticks },
            scope = "Load-time structural/profile validation, not licensing or hardware playback acceptance. Package these exact hashed bytes with this profile."
        };
    }
}
