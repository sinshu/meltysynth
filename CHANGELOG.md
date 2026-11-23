# Unreleased

- Add a dependency-free Release audio audit with plain/profiled source builds,
  separated loading, synthesis/effects and conversion observations, raw timing data,
  allocations, deadline/asset/queue metrics and exact PCM comparisons. Stage hooks
  are compiled out of normal builds; no DSP algorithm or per-voice task changed.
- Add a preallocated single-producer/single-consumer PCM integration ring in
  Workspace, replacing measured allocating collection bookkeeping. Cover bounded
  backpressure, FIFO audio identity, cancellation, lease cleanup and shutdown.

- Reject nonfinite/out-of-range master gain and sequencer speed before changing
  playback state. Define gain [0,1], speed zero or [1/128,16], block-boundary changes,
  mute/zero-speed semantics and single-owner command/rendering requirements.
- Add offline target-profile SoundFont/MIDI preflight with exact SHA-256 identities
  and an atomic, non-overwriting validation receipt. Keep structural checks at load
  and control validation in setters, with no extra per-sample validation/copies.

- Complete nullable flow in external-bank comparison helpers and enable nullable
  checks in both test modes and Workspace. Require initialized region ranges and
  explicitly fail missing numerical matches instead of suppressing warnings.
- Make Workspace's existing examples and benchmark runnable with configurable
  assets and filenames. Keep WAV/JSON output in named bin/workspace runs and direct
  test receipts in bin/TestResults; remove Windows Media/current-directory paths.
  Share WAV writing, bound offline durations and verify original-fixture stereo,
  PCM accuracy, silence, invalid inputs and released file handles.

- Target the dependency-free core, tests and Workspace at .NET 10 with explicit
  C# 14. Pin stable SDK 10.0.404 and provide an optional SHA-512-verified local SDK
  installer. Resolve preset lookup nullable flow without suppression, added copies
  or changed fallback behavior; cover exact, GM and default fallback paths.

- Reject non-progressing MIDI loops before playback to prevent a zero-duration loop
  extension from hanging audio rendering. A rejected Play request leaves the
  current playback unchanged; non-looped playback still ignores loop markers.

- Correct sample-loop wrapping when a playback step crosses multiple loop lengths,
  including loop-until-note-off release transitions. Reject empty/reversed sample
  extents and active loops outside their effective sample range at SoundFont load.

- Make writable SIMD and SoundFont sample-loading spans explicit for .NET 10/C# 14
  source integration. Add an opt-in source-inclusive test configuration and exact
  PCM-loading coverage; the library target is now upgraded as described above.

- Fill PCM sample buffers across short reads and fail explicitly on incomplete
  data. Validate sample LIST/RIFF boundaries, 16-bit alignment and RIFF padding;
  reject duplicate sample chunks and retain caller-owned stream lifetime.

- Add immutable, application-configurable `AssetLoadLimits` to MIDI/SoundFont
  constructors. Enforce source/PCM/metadata/record/region and MIDI track/event/time
  budgets during loading; validate INFO/pdta ranges and MIDI fields before use.
  Require EOT at the track end by default; an explicit option permits only bounded
  zero-byte padding. Trailing events/arbitrary bytes are rejected. Existing
  constructors use documented default limits; playback has no new checks.

- Establish a portable, identity-checked regression baseline with original MIDI/
  SoundFont synthesis fixtures, frozen stereo metrics and independent reader/timing
  checks. Separate unavailable external-bank comparisons from self-contained DSP
  tests, use configurable assembly-relative paths and invariant numeric parsing,
  and validate complete interleaved conversion output.

# v2.4.1

- Fixed potential issue where loading a file could fail.
- Now loading SoundFont3 fails with an explicit error.
- Optimized chorus for better performance.

# v2.4.0

- Reduced memory allocation on loading SoundFont.
- Enabled nullable analysis.
- Dropped the .NET Framework support.
- Added the ability to hook MIDI sequencer event processing.

# v2.3.0

- Using fixed-point numbers in the oscillator has improved rendering speed by 10%.
- Added RenderInt16 method to IAudioRenderer for audio drivers which use non-interleaved buffers of Int16.
- Deleted some unused code for memory efficiency.

# v2.2.3

- Improved voice prioritization.
- Improved fallback when no corresponding preset was found.
- Now unknown generators are ignored.

# v2.2.2

- Added fallback when no corresponding preset was found.
- Fixed wrong MIDI message handling.
- Improved error check to allow some non-standard SoundFonts.

# v2.2.1

- Added the EndOfSequence property to MidiFileSequencer.
- Added the Position property to MidiFileSequencer.
- Some minor improvements.

# v2.2.0

- This version supports .NET Standard 2.1.

# v2.1.0

- Added support for MIDI file loop extension.
- Added utility methods to convert sample format.
- Fixed pop noise in certain instruments.
- Improved performance by using Vector<T>.

# v2.0.0

- Revised the API to simplify the MIDI playback functionality.

# v1.1.3

- Fixed pop noise in certain instruments.

# v1.1.2

- Now the package contains the XML documentation.

# v1.1.1

- Revised accessibility of members.

# v1.1.0

- Implemented reverb and chorus.

# v1.0.3

- Improved the loudness correction.

# v1.0.2

- Fixed crash where the MIDI file sequencer tries to read a MIDI file even if the file is not yet loaded.

# v1.0.1

- Fixed crash due to negative sustain level.

# v1.0.0

- Most of the basic functionalities are implemented.
