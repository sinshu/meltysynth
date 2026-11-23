# Portable .NET 10 Workspace tools

Workspace is an offline developer executable. It uses the local library project
and the existing NAudio.Core WAV writer; it does not open a device or add a native
playback backend. The root SDK pin and optional local SDK installer apply here too.

## Commands

```sh
./bin/dotnet/dotnet run --project Workspace/Workspace.csproj -c Release -- --help
./bin/dotnet/dotnet run --project Workspace/Workspace.csproj -c Release -- chord --assets /path/to/permitted-assets
./bin/dotnet/dotnet run --project Workspace/Workspace.csproj -c Release -- examples --assets /path/to/permitted-assets
./bin/dotnet/dotnet run --project Workspace/Workspace.csproj -c Release -- benchmark --assets /path/to/permitted-assets --seconds 10 --run trial-01
```

Use `bin\dotnet\dotnet.exe` on Windows, or `dotnet` with the pinned SDK installed.
In Rider, run Workspace with the same arguments after `--` above. Assets default
to `MELTYSYNTH_TEST_ASSETS`, then the checkout's ignored
`MeltySynthTest/ExternalAssets`. `--assets` overrides that location. Inputs default
to `TimGM6mb.sf2` and `flourish.mid`; `--soundfont FILE --midi FILE` selects other
filenames inside the asset directory. Only `chord` does not need MIDI. No download,
renaming a synthetic fixture as a third-party bank, or Windows Media path is assumed.

The three commands render the existing chord, all five existing examples, or a
bounded offline benchmark. Generated WAVs and benchmark JSON are always under
**`bin/workspace/NAME`**, with `latest` as the default run name. Reusing a run name
overwrites that run's outputs. Run names and input/output filenames cannot contain
directory components. The checkout is found from the executable location, not the
current working directory. Missing required files are reported before creating
outputs. This executable is a checkout tool, not a standalone deployed application.

Examples accept MIDI durations up to 300 seconds; the benchmark accepts 1–300
seconds (default 10). These are offline tools with whole-output arrays, not a
streaming runtime benchmark. WAV output uses shared peak normalization to 0.99,
16-bit stereo at 44.1 kHz; empty/silent output stays zero. All writers are disposed.
The benchmark times only `sequencer.Render`, excluding asset loading, allocation,
normalization, WAV writing and receipt serialization. Its single elapsed value is
a smoke measurement, not a warmed throughput, allocation or latency claim.

## Regression and acceptance

```sh
python3 tools/regression_baseline.py --output-dir bin/validation/net10-tooling/regression
./bin/dotnet/dotnet build MeltySynth.slnx -c Release -warnaserror
# Direct test runs also place TRX files under the test project's ignored bin/.
./bin/dotnet/dotnet test MeltySynthTest/MeltySynthTest.csproj -c Release --logger trx
```

The actual Workspace sources are linked into both test compilation modes, avoiding
a second implementation or a Workspace project reference that could accidentally
restore a binary library dependency to the source-inclusive lane. Nine cases use
small original SoundFont/MIDI content, explicitly named `Original.sf2`/`Original.mid`:

- all three commands produce expected stereo WAVs; chord PCM matches direct
  synthesis within two 16-bit quantization steps, and every rendered WAV is finite
  with positive energy;
- benchmark frame count and positive render duration are asserted;
- empty/silent WAVs contain exactly zero PCM;
- exclusive reopening checks all audio handles are released;
- traversal names, missing assets and invalid arguments fail explicitly.

Tests generate original input bytes beside the test assembly under its `bin/`;
WAVs and receipts use unique `bin/workspace/test-*` runs. Nothing writes into checked-in
reference data or OS temp. The runner rejects empty discovery and unexpected skips,
checks the frozen corpus, and now treats compiler warnings as errors in both modes.
Ordinary test runs use `MeltySynthTest/bin/TestResults` unless explicitly overridden.

Accepted 2026-11-23 on macOS ARM64, SDK 10.0.404 / runtime 10.0.15: root Release build
**0 warnings / 0 errors**; both project-reference and source-inclusive suites
**308 passed / 48 external unavailable / 0 failed**, 356 discovered per mode.
All six prior nullable warnings are resolved: required key/velocity fields are
initialized during region construction, and a missing MinBy result throws an
explicit numerical-test assertion. Nullable checking is enabled in both modes and
Workspace; no suppression or null-forgiving operator was added. Existing package
versions and numerical tolerances are unchanged.

A direct compiled CLI run from `bin/validation/net10-tooling/non-root` also rendered
44,100 stereo frames, produced a positive timing and wrote only to the intended
run folder. See local `bin/validation/net10-tooling/summary.json` and regression
logs/TRX. The 48 unavailable external comparisons are not reported as passing;
external-bank and hardware playback acceptance remain separate.

## Asset packaging preflight

```sh
./bin/dotnet/dotnet run --project Workspace/Workspace.csproj -c Release -- validate --assets /path/to/scene-audio --soundfont Scene.sf2 --midi Scene.mid --profile Workspace/asset-profile.example.json --run scene-audio-v1
```

`--profile` is required. The example is an illustrative small-scene budget, not a
universal device limit; choose the actual application profile before packaging.
JSON names are case-insensitive; unknown fields and invalid budgets fail, profiles
are bounded to 16 KiB, omitted fields inherit `AssetLoadLimits.Default`. The receipt
records **all resolved limits**, both SHA-256 hashes, file lengths, decoded PCM bytes,
preset/instrument counts and MIDI duration. The same core loaders are used at
packaging and runtime. Hashing rereads the owned source streams at packaging time;
there is no additional PCM copy or repeated validation in the audio frame path.

A fresh named run publishes `assets.validated.json` by same-directory rename only
after the entire pair is validated. Failures publish no ready marker; writers and
source streams are closed on success/failure. Existing ready receipts are never
overwritten: use a new run/version and require successful exit. The packaging owner
must package those exact hashed bytes and the resolved profile; changed bytes or a
different target profile require revalidation. A receipt alone must not authorize
unrelated replacement bytes. Runtime still performs its structural/profile checks
once per load; it does not continuously rehash or rescan samples during rendering.
This repository has no scene packager to wire automatically: call this gate for each
pair in the consuming packaging pipeline. No readiness state, parallel audio owner
or synchronization layer is introduced in the core.

Item-7 acceptance (2026-11-23): both compilation modes pass **336 self-contained /
48 external unavailable / 0 failed**, warnings as errors; 28 new boundary cases
cover invalid controls with unchanged subsequent audio, gain/mute/zero-speed/resume,
positive speed extrema at 192 kHz with eight-sample blocks, preflight identity,
budgets, truncated input, malformed/oversized profiles and atomic no-overwrite
promotion. The compiled CLI also passed from a non-root directory using the example
profile. Frozen synthesis values/tolerances are unchanged. These are structural and
correctness results, not hardware playback or asset-licensing acceptance.
Local receipts: `bin/validation/asset-control-boundaries/`.
