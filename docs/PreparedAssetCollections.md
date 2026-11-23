<!--
SPDX-License-Identifier: MIT
Copyright (C) 2026 James Carpenter
Contributed by KilnGod (James Carpenter).
See LICENSE.txt for the full MIT license and inherited notices.
-->

# Prepared asset collection ownership

Implemented for outline item 14 on 2026-11-23, .NET 10/C# 14, macOS ARM64.

## Public and internal boundaries

The five public collection surfaces now return cached `ReadOnlyAssetList<T>`
instances through their existing `IReadOnlyList<T>` signatures:

| Owner | Public surface | Canonical internal storage |
| --- | --- | --- |
| SoundFont | SampleHeaders | SampleHeaderArray |
| SoundFont | Instruments | InstrumentArray |
| SoundFont | Presets | PresetArray |
| Instrument | Regions | RegionArray |
| Preset | Regions | RegionArray |

Each view has one private readonly array reference. It exposes only read-only
count/index access and generic/non-generic enumeration. It implements no mutable
IList/ICollection interfaces, exposes no SyncRoot, and provides no array conversion
or public storage property. The array elements are not copied. Supported lookup
ordering, reference identities, bank/program metadata and out-of-range index
exceptions match the prior IReadOnlyList array behavior.

SoundFont creates its three views after sample, region and effective-pitch checks
succeed, before construction returns. Instrument/preset constructors create their
region views once, including their internal empty defaults. Public getters return
the same instances. Synthesizer setup and note/render paths continue using the
existing internal arrays; no per-getter/per-note wrapper, snapshot or guard was
introduced. WaveData remains the existing ReadOnlySpan over canonical PCM.

The public object graph was reviewed: SampleHeader and region metadata expose
immutable values/references, and nested instrument/preset collections now use the
same protected boundary. SoundFontSampleData, SoundFontParameters and Zone array
surfaces are inside internal preparation types and are not public asset access.
Prepared data is shared after construction, while voice, oscillator, controller,
buffer and effect state remain owned by each synthesizer. This does not make a
mutable synthesizer thread-safe. Trusted internal storage access remains unchanged;
reflection or low-level reference rewriting is outside the read-only API contract.

## Why not Array.AsReadOnly?

The first wrapper experiment rejected ordinary setters but failed the added
`ICollection.SyncRoot` regression on .NET 10.0.15: SyncRoot returned the original
SampleHeader/Instrument/Preset/region array. A caller could still recover and clear
it without reflection. That intermediate result is retained in the local `after`
TRX and is not counted as accepted ownership protection.

The small internal adapter avoids inheriting those mutable/synchronization
interfaces entirely. Array, IList<T>, ICollection<T>, IList and ICollection casts
fail with InvalidCastException. A caller may enumerate into its own array and
change that copy, without changing the prepared asset. The existing public
IReadOnlyList API never promised mutable-interface or concrete-array casts.

## Regression coverage

`PreparedAssetCollectionTest` adds 16 original-fixture cases:

- The original Instrument.Regions array-clear route is rejected after constructing
  a synth; NoteOn still succeeds with active voices.
- All five views reject raw-array and generic/legacy mutable/synchronization paths,
  retain backing-array contents, and preserve indexing/iteration order. Bounds
  errors are checked against the original array's IReadOnlyList behavior.
- An original multi-entry bank has two samples, instruments and presets, and two
  regions per instrument/preset. Its second preset reverses instrument order;
  tests check exact reference/order identity and bank 1/program 7 selection.
- Each of five getter/index loops runs 10,000 measured iterations after warmup,
  returning the same view/element references and allocating zero managed bytes.
  An additional 10,000 NoteOn/render/NoteOffAll iterations also allocate zero bytes.
- Internal default empty region views remain stable and read-only.
- Four whole-synth owners render one shared bank concurrently, each serializing
  its own mutable core. Controls, pan, gain, bank/program, pitch, note release/reset,
  mixed 32/48 kHz rates and effects off/on match fresh independent-bank references
  exactly. PCM and region identities remain unchanged in the shared asset.

The allocation promise here covers getters/indexing and the measured canonical
note/render path, not a new promise of allocation-free public IEnumerator creation.
Internal array enumeration remains the rendering path.

```sh
./bin/dotnet/dotnet test MeltySynthTest -c Release --filter FullyQualifiedName~PreparedAssetCollectionTest
python3 tools/regression_baseline.py --output-dir bin/validation/asset-collections/regression-final
python3 tools/audio_audit.py --font /path/to/neon-mile.sf2 --midi /path/to/neon-mile.mid --output-dir bin/validation/asset-collections/audio
python3 tools/native_aot_smoke.py --output-dir bin/validation/asset-collections/native-aot
```

Both complete warning-as-error Release configurations pass **704 self-contained
tests / 48 unavailable external-bank comparisons / zero failures**, 752 discovered
per project-reference/source-integration lane. The discovery floor is raised to
704. Frozen corpus, MIDI inputs, 32 kHz synthesis values and tolerances are unchanged.
The initial raw-array regression run fails all six exposure cases, as expected.
The final adapter passes those cases plus sharing, order and allocation coverage.

Source delivery must include every `MeltySynth/src/*.cs` file, including the new
`ReadOnlyAssetList.cs`, with the unchanged MIT notice. Do not copy only the three
modified asset classes and omit their shared helper. The source-inclusive test
lane compiles the actual source set; no new runtime NuGet or game dependency is
introduced. Native smoke coverage now explicitly checks all five view types,
cache identity, rejected mutable interfaces and enumeration before accepting a
fresh package extraction.

Raw mutation experiments, TRX, audio and native receipts remain under ignored
`bin/validation/asset-collections/`. Versioned regressions and this contract are
outside ignored codex so they can be reviewed and committed through Rider.

## Ordinary audio and native delivery results

Motel California's effects-off/on PCM hashes match item 13 exactly in both plain
and profiled builds. The score wraps and music-render allocations remain zero.
Raw saturated-polyphony timing/deadline outliers are retained; no new real-time
performance guarantee is inferred from these correctness/ownership captures.

All four fresh macOS ARM64 NativeAOT deliveries passed: project/runtime effects,
project/fixed-off, extracted source/fixed-off and local NuGet/runtime effects.
Each explicitly verifies the protected collections, cached identity, lack of
mutable/SyncRoot interfaces and enumeration. Existing native PCM/frozen smoke
scenarios pass, diagnostics are empty and packages contain no standalone DLLs or
dylibs. The MIT-attributed source package includes all 45 current core source files,
including ReadOnlyAssetList.cs, with exact source identities. Runtime effects flags
still do not guarantee code removal. No fresh OS playback/browser/mobile or
Windows/Linux acceptance is claimed by this change.

The consolidated local acceptance is `bin/validation/asset-collections/acceptance.json`.
