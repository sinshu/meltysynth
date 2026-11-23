// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;

namespace MeltySynth
{
    /// <summary>Immutable budgets applied once while loading MIDI and SoundFont assets.</summary>
    public sealed class AssetLoadLimits
    {
        /// <summary>Gets the shared, immutable default loading budgets.</summary>
        public static AssetLoadLimits Default { get; } = new AssetLoadLimits();

        /// <summary>Creates application-specific loading budgets. All numeric limits must be positive.</summary>
        /// <param name="maxFileBytes">Maximum remaining bytes in an asset stream (default 512 MiB).</param>
        /// <param name="maxDecodedSampleBytes">Maximum decoded 16-bit PCM bytes (default 256 MiB).</param>
        /// <param name="maxMidiTrackBytes">Maximum bytes in one MIDI track (default 16 MiB).</param>
        /// <param name="maxMidiTracks">Maximum MIDI tracks (default 256).</param>
        /// <param name="maxMidiEvents">Maximum parsed MIDI events across all tracks, including ignored events (default 1,000,000).</param>
        /// <param name="maxSoundFontRecords">Maximum aggregate SoundFont parameter-table records (default 262,144).</param>
        /// <param name="maxSoundFontRegions">Maximum aggregate referenced instrument/preset zones (default 65,536).</param>
        /// <param name="maxMetadataBytes">Maximum SoundFont INFO bytes (default 1 MiB).</param>
        /// <param name="maxMidiDuration">Maximum MIDI duration (default 24 hours).</param>
        /// <param name="allowMidiEndOfTrackPadding">Whether zero-byte padding after EOT inside its declared track is allowed (default false).</param>
        public AssetLoadLimits(long maxFileBytes = 512L * 1024 * 1024,
            int maxDecodedSampleBytes = 256 * 1024 * 1024, int maxMidiTrackBytes = 16 * 1024 * 1024,
            int maxMidiTracks = 256, int maxMidiEvents = 1000000,
            int maxSoundFontRecords = 262144, int maxSoundFontRegions = 65536,
            int maxMetadataBytes = 1024 * 1024, TimeSpan? maxMidiDuration = null,
            bool allowMidiEndOfTrackPadding = false)
        {
            if (maxFileBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxFileBytes));
            if (maxDecodedSampleBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxDecodedSampleBytes));
            if (maxMidiTrackBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxMidiTrackBytes));
            if (maxMidiTracks <= 0) throw new ArgumentOutOfRangeException(nameof(maxMidiTracks));
            if (maxMidiEvents <= 0) throw new ArgumentOutOfRangeException(nameof(maxMidiEvents));
            if (maxSoundFontRecords <= 0) throw new ArgumentOutOfRangeException(nameof(maxSoundFontRecords));
            if (maxSoundFontRegions <= 0) throw new ArgumentOutOfRangeException(nameof(maxSoundFontRegions));
            if (maxMetadataBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxMetadataBytes));
            var duration = maxMidiDuration ?? TimeSpan.FromHours(24);
            if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maxMidiDuration));
            MaxFileBytes = maxFileBytes; MaxDecodedSampleBytes = maxDecodedSampleBytes;
            MaxMidiTrackBytes = maxMidiTrackBytes; MaxMidiTracks = maxMidiTracks; MaxMidiEvents = maxMidiEvents;
            MaxSoundFontRecords = maxSoundFontRecords; MaxSoundFontRegions = maxSoundFontRegions;
            MaxMetadataBytes = maxMetadataBytes; MaxMidiDuration = duration;
            AllowMidiEndOfTrackPadding = allowMidiEndOfTrackPadding;
        }

        /// <summary>Gets the maximum remaining source bytes.</summary>
        public long MaxFileBytes { get; }
        /// <summary>Gets the maximum decoded PCM bytes.</summary>
        public int MaxDecodedSampleBytes { get; }
        /// <summary>Gets the maximum bytes in one MIDI track.</summary>
        public int MaxMidiTrackBytes { get; }
        /// <summary>Gets the maximum MIDI track count.</summary>
        public int MaxMidiTracks { get; }
        /// <summary>Gets the maximum aggregate parsed MIDI event count.</summary>
        public int MaxMidiEvents { get; }
        /// <summary>Gets the maximum aggregate SoundFont table record count.</summary>
        public int MaxSoundFontRecords { get; }
        /// <summary>Gets the maximum aggregate referenced SoundFont zone count.</summary>
        public int MaxSoundFontRegions { get; }
        /// <summary>Gets the maximum SoundFont INFO bytes.</summary>
        public int MaxMetadataBytes { get; }
        /// <summary>Gets the maximum MIDI duration.</summary>
        public TimeSpan MaxMidiDuration { get; }
        /// <summary>Gets whether zero-byte EOT padding inside a track is accepted.</summary>
        public bool AllowMidiEndOfTrackPadding { get; }
    }
}
