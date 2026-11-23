# MeltySynth modernization inventory and frozen scope

Reviewed 2026-11-23. This inventory closes implementation-sequence item 1. It
records the checkout and review decisions; it does not establish device playback,
AOT acceptance or completion of the core framework upgrade.
Subsequent fixture/path baseline work is recorded in
[RegressionBaselines.md](RegressionBaselines.md): the frozen pre-upgrade suite had 295
self-contained passes, with named external-bank comparisons reported separately.

Subsequent upgrade acceptance: the core, tests and Workspace now target .NET 10 /
C# 14 with SDK 10.0.404 pinned. Both compilation paths pass 299 self-contained cases;
the historical inventory below records the original toolchain and target state.
See the migration acceptance in RegressionBaselines.md.

## Scope and source baseline

- Reuse existing work branch `kilngod/fix-midi-loop-progress`; HEAD remains
  `4ba079c9ee1453b03be7e164d254f48426f0aac3`. Findings 1–5 are local changes above
  that upstream baseline. No new branch, commit, push or GitHub operation was
  performed for this inventory. Rider remains the Git review/push gatekeeper.
- `origin`: `https://github.com/kilngod/meltysynth.git`;
  `upstream`: `https://github.com/sinshu/meltysynth.git`.
- The active modernization covers the dependency-free synthesis core, its focused
  and numerical tests, and useful offline Workspace tooling. Preserve the MIT
  license and the C# Synth/TinySoundFont/SFZero/MeltySynth attribution in
  [LICENSE.txt](../LICENSE.txt). No replacement audio backend, speech system,
  rendering dependency or per-voice threading is included.
- Per the user's final scope decision, **all 16 examples receive only a basic
  .NET 10/C# 14 upgrade**, including narrow compilation fixes. Preserve their
  dependency versions and audio APIs. They are reference examples, not WebGPU
  dependencies; backend modernization and live playback are deferred unless a
  concrete platform consumer needs one. No example was deleted or moved.
- Before retargeting, all 19 project and 17 solution identities were recorded in
  `bin/validation/pre-upgrade-inventory/before-retarget.json`. Its source-manifest
  SHA-256 is `8ae7a9d935f5b7d8db250421ddfd4d0c514effe38b3b422f25e855b8bcf54c9c`.
  The post-change receipt separately records the updated source identities.

The WebGPU checkout currently has one direct package consumer:
`KilnGod.WebGPU.BlazorNative/KilnGod.WebGPU.BlazorNative.csproj` references NuGet
`MeltySynth` **2.4.1**. `MotelNativeAudio.cs` owns a synthesizer/sequencer on one
long-running producer, feeds stereo float32 SDL3 blocks at 32 kHz/512 frames,
uses a bounded cue channel, and joins the producer before destroying the stream.
It loads embedded MIDI/SoundFont/cues and does not reference the sixteen example
projects. It still consumes the upstream package, **not these local fork fixes**.
Switching it to the reviewed fork/source belongs to a separate integration step;
no WebGPU project was changed here. Inspect each Apple/mobile adapter separately
before inferring that desktop SDL3 audio establishes its playback support.

## Toolchain freeze

| Item | Observed value |
| --- | --- |
| Host | macOS 27.2.1, build 26B131; `osx-arm64` |
| Selected .NET SDK | 10.0.203, commit c23858a6d8 |
| MSBuild | 18.3.3+c23858a6d |
| .NET host/runtime | 10.0.7 ARM64; net8 runtime also installed |
| Xcode selection | `/Applications/Xcode.26.4.app/Contents/Developer` |
| Xcode | 26.4.1, build 17E202; macOS SDK 26.4 |
| Clang | Apple clang 21.0.0 (clang-2100.0.123.102) |
| SDK/workspace configuration | No `global.json`, Directory.Build props/targets, local tool manifest, NuGet config or GitHub workflow in this checkout. SDK pinning remains a core-upgrade task. |

Installed macOS/Mac Catalyst/MAUI workloads do not make MAUI a project dependency
or establish iOS/tvOS workload acceptance. The inventory receipt captures the full
`dotnet --info`, OS and Xcode output, including command diagnostics.

## All projects and solutions

There are **19 projects and 17 XML `.slnx` solutions**, not legacy `.sln` files.
All solution project paths resolve. The root [MeltySynth.slnx](../MeltySynth.slnx)
contains exactly core, tests and Workspace. Each of 16 example directories has
one solution pointing to its own project. The inventory tool validates all links;
adding the examples to the root solution is unnecessary for this scope.

| Project area | Current configuration | Frozen disposition |
| --- | --- | --- |
| [MeltySynth](../MeltySynth/MeltySynth.csproj) | netstandard2.1, nullable enabled, no package dependencies; package 2.4.1 generated on build | Primary .NET 10/C# 14 migration target; framework switch, SDK pin and nullable cleanup remain separate. |
| [MeltySynthTest](../MeltySynthTest/MeltySynthTest.csproj) | Default net8.0 project reference; opt-in net10.0/C# 14 source-inclusive lane | Retain focused/numerical coverage; make the primary framework upgrade in its own step. 153 focused cases already pass in both lanes. Full external-bank suite remains unaccepted. |
| [Workspace](../Workspace/Workspace.csproj) | net8.0; `Program.Main` is empty | Retain useful offline render/benchmark tools. No audio device owner in Main. Helpers allocate full-song buffers and write relative WAV paths; replace hardcoded inputs/output locations when upgrading. |
| Sixteen example projects | net10.0, except NAudio net10.0-windows; explicit LangVersion 14.0 | Basic upgrade completed; all 16 Release builds passed. Runtime behavior and backend dependencies otherwise unchanged. |

All examples still reference the **NuGet MeltySynth 2.4.1 package**, not the local
project. Their successful builds therefore do not test this fork's synthesis fixes.
The two basic source fixes are explicit mutable `buffer.AsSpan()` arguments in
FNA.NET/MonoGame, resolving C# 14 `ReadOnlySpan<short>` overload selection. No
threading, scheduling, resource ownership or device-format fixes were mixed in.

### Entry points, native dependencies and playback ownership

Every row below has the disposition **basic upgrade complete; reference-only for
WebGPU integration; platform playback deferred**. "Requires" describes the sample
API/native dependency, not a tested list of shipped architectures. All rows assume
44.1 kHz stereo unless the engine negotiates otherwise.

| Directory / entry point | Native/loading path and ownership review | Remaining issues if adopted |
| --- | --- | --- |
| CSCore / `Program.Main` | CSCore WaveOut uses Windows multimedia; `MidiWaveSource.Read`/Play/Stop share a lock; `using WaveOut` owns device lifetime, source Dispose is empty. | Windows APIs behind generic net10 TFM; NU1701 framework compatibility warning. No Windows execution here. |
| DotFeather / `MidiExampleScene.Main` | `DF.Run` creates the scene; AudioPlayer enumerates locked, reusable left/right PCM batches; OnDestroy stops/disposes AudioPlayer. NuSpec pulls Silk.NET and native OpenAL Soft. | Iterator/callback and scene teardown acceptance; transitive ImageSharp warnings; not a way to avoid Silk.NET. |
| DrippyAL / `Program.Main` | Nested `using AudioDevice`/AudioStream; locked short-buffer callback/control; package depends on Silk.NET.OpenAL and OpenAL Soft. | Native RIDs and callback shutdown untested; also indirectly adopts Silk.NET. |
| FNA.NET / `Program.Main` → `Game1.LoadContent` | Game owns MidiPlayer/DynamicSoundEffectInstance; BufferNeeded renders reusable byte PCM; UnloadContent disposes player. Native assets supplied through FNA.NET.NativeAssets. | Validate callback/control affinity and in-flight disposal on any future target; no locking shown in the example. |
| MonoGame / `Program.Main` → `Game1.LoadContent` | Same dynamic-sound ownership pattern; DesktopGL package depends on native SDL and Vorbis support. Game is disposed by using. | Same callback/control acceptance; native graphics/audio runtime is unrelated to WebGPU. |
| NAudio / `Program.Main` | Explicit Windows target; WaveOut function callbacks request float PCM from locked ISampleProvider; device disposed by using. | Windows playback only; Mac cross-build used EnableWindowsTargeting, not execution. |
| OpenTK / `Program.Main` | Raw OpenAL device/context created on Main; MidiPlayer owns queued AL buffers/source and a cancellation/polling task; locks serialize sequencer controls/rendering. | Verify context affinity, background faults and cancellation/join. Device/context cleanup is after the using block rather than in finally. Old batch script expects external openal32.dll. |
| Raylib_CsLo / `Program.Main` | Main/window loop refills one reusable Int16 stereo buffer when requested; Raylib owns audio device/stream. | Loop closes only the window explicitly; stream unload/device close and failure cleanup are not shown. MPL-2.0 wrapper is not adopted into the engine. |
| Raylib_cs / `Program.Main` | Same main-loop refill design, with fixed pointer upload and raylib native binaries resolved by package/backend. | Same cleanup omissions; no runtime/native architecture acceptance from a build. |
| SDL2 / `Program.Main` | SDL audio callback and controls share a lock; close device then quit audio on normal exit. ppy package includes native SDL2 assets. | `ALLOW_ANY_CHANGE` accepts a changed format while synth stays fixed; obtained format ignored; cleanup is not finally-protected. Inspected package lacks an osx-arm64 native asset. This is not the engine's SDL3 adapter. |
| SFML.Net / `Program.Main` | MidiPlayer owns a SoundStream; OnGetData renders a reusable ~50 ms stereo Int16 batch under lock; Dispose releases stream. Native CSFML resolved by package. | NETSDK1206 flags a version-specific `fedora-x64` native RID; validate callback drain and each adopted architecture. |
| Silk.NET.OpenAL / `Program.Main` | API wrappers, native OpenAL Soft, manual device/context; MidiPlayer owns AL buffers/source and cancel/join polling task. | Context/device cleanup outside finally; verify context affinity/fault cleanup. Reference-only per no-Silk application preference. |
| Silk.NET.SDL / `Program.Main` | Sdl.GetApi owns wrapper; locked callback/control; native SDL2 arrives through Ultz.Native.SDL; manual device close/audio quit. | Callback delegate lifetime, ignored obtained format, ANY_CHANGE and exception cleanup need review only if adopted. Not an SDL3 replacement. |
| Sokol / `Program.Main` | Static synth/sequencer, unmanaged stream callback with a shared lock, Audio.Setup/Shutdown; native sokol package resolves platform library. | Static asset construction, callback channel assumption, exceptions and shutdown on failure not accepted; experimental package remains reference-only. |
| SoundFlow / `Program.Main` | Using MiniAudioEngine owns native miniaudio; MidiPlayer added to static Master mixer; locked GenerateAudio/Play/Stop; engine rate supplied to synth. | Confirm mixer removal, callback drain and negotiated channels. Native package presence, including iOS framework, does not establish AOT/static or platform playback. |
| TinyAudio / `Program.Main` | DirectSoundAudioPlayer, 200 ms buffer, locked Int16 callback; using owns player. | Windows-specific implementation despite generic net10 TFM; no Mac playback claim. |

Common input/launch assumptions: every example loads `TimGM6mb.sf2` relative to its
working directory and uses absolute `C:\Windows\Media\flourish.mid`. None provides a
portable asset CLI or obtains the bank. Console samples block on ReadKey; graphical
samples run their framework loop. `Workspace/Examples.cs` shares those inputs;
its WAV normalization needs an explicit silence policy before reuse. Workspace
Benchmark.Run times a single full-buffer render, excluding setup/file output; it
is not a warmed block/p99/device-latency benchmark.

## Dependency versions and license evidence

The table below is exact-version direct PackageReference metadata, inspected from
NuGet manifests; file-based license texts were read from their nupkgs. Local raw
manifests, hashes and license text receipts are under
`bin/validation/pre-upgrade-inventory/`. This is a provenance inventory, not a claim
that one wrapper license covers every native/transitive component. The inventory
receipt also records resolved package versions/SHA-512 and available transitive
license metadata from each project's `project.assets.json` and local NuSpec cache.
No dependency versions were upgraded in this task.

| Package | Version | License evidence |
| --- | --- | --- |
| [CSCore](https://api.nuget.org/v3-flatcontainer/cscore/1.2.1.2/cscore.nuspec) | 1.2.1.2 | License URL only; text not present in package; unresolved |
| [DotFeather](https://api.nuget.org/v3-flatcontainer/dotfeather/3.2.0/dotfeather.nuspec) | 3.2.0 | MIT |
| [DrippyAL](https://api.nuget.org/v3-flatcontainer/drippyal/2.1.3/drippyal.nuspec) | 2.1.3 | MIT |
| [FNA.NET](https://api.nuget.org/v3-flatcontainer/fna.net/1.0.15.2403/fna.net.nuspec) | 1.0.15.2403 | MS-PL (packaged LICENSE) |
| [MathNet.Numerics](https://api.nuget.org/v3-flatcontainer/mathnet.numerics/5.0.0/mathnet.numerics.nuspec) | 5.0.0 | MIT |
| [MeltySynth](https://api.nuget.org/v3-flatcontainer/meltysynth/2.4.1/meltysynth.nuspec) | 2.4.1 | MIT (packaged LICENSE.txt) |
| [Microsoft.NET.Test.Sdk](https://api.nuget.org/v3-flatcontainer/microsoft.net.test.sdk/18.3.0/microsoft.net.test.sdk.nuspec) | 18.3.0 | MIT |
| [MonoGame.Framework.DesktopGL](https://api.nuget.org/v3-flatcontainer/monogame.framework.desktopgl/3.8.2.1105/monogame.framework.desktopgl.nuspec) | 3.8.2.1105 | MS-PL |
| [NAudio](https://api.nuget.org/v3-flatcontainer/naudio/2.2.1/naudio.nuspec) | 2.2.1 | MIT (packaged license.txt) |
| [NAudio.Core](https://api.nuget.org/v3-flatcontainer/naudio.core/2.3.0/naudio.core.nuspec) | 2.3.0 | MIT |
| [NUnit](https://api.nuget.org/v3-flatcontainer/nunit/4.5.1/nunit.nuspec) | 4.5.1 | MIT |
| [NUnit3TestAdapter](https://api.nuget.org/v3-flatcontainer/nunit3testadapter/6.1.0/nunit3testadapter.nuspec) | 6.1.0 | MIT |
| [OpenTK](https://api.nuget.org/v3-flatcontainer/opentk/4.8.2/opentk.nuspec) | 4.8.2 | MIT |
| [Raylib-CsLo](https://api.nuget.org/v3-flatcontainer/raylib-cslo/4.2.0.9/raylib-cslo.nuspec) | 4.2.0.9 | MPL-2.0 |
| [Raylib-cs](https://api.nuget.org/v3-flatcontainer/raylib-cs/6.1.1/raylib-cs.nuspec) | 6.1.1 | Zlib |
| [SFML.Net](https://api.nuget.org/v3-flatcontainer/sfml.net/2.6.0/sfml.net.nuspec) | 2.6.0 | Zlib |
| [Silk.NET.OpenAL](https://api.nuget.org/v3-flatcontainer/silk.net.openal/2.21.0/silk.net.openal.nuspec) | 2.21.0 | MIT |
| [Silk.NET.OpenAL.Soft.Native](https://api.nuget.org/v3-flatcontainer/silk.net.openal.soft.native/1.23.1/silk.net.openal.soft.native.nuspec) | 1.23.1 | LGPL-2.0-or-later |
| [Silk.NET.SDL](https://api.nuget.org/v3-flatcontainer/silk.net.sdl/2.21.0/silk.net.sdl.nuspec) | 2.21.0 | MIT |
| [SoundFlow](https://api.nuget.org/v3-flatcontainer/soundflow/1.0.3/soundflow.nuspec) | 1.0.3 | MIT (packaged LICENSE.md) |
| [TinyAudio](https://api.nuget.org/v3-flatcontainer/tinyaudio/0.3.1/tinyaudio.nuspec) | 0.3.1 | MIT |
| [ppy.SDL2-CS](https://api.nuget.org/v3-flatcontainer/ppy.sdl2-cs/1.0.82/ppy.sdl2-cs.nuspec) | 1.0.82 | No license metadata or packaged text; unresolved |
| [sokol_csharp.unofficial](https://api.nuget.org/v3-flatcontainer/sokol_csharp.unofficial/0.0.1/sokol_csharp.unofficial.nuspec) | 0.0.1 | Zlib |

Two often-hidden dependencies matter to the scope: DotFeather's manifest pulls
Silk.NET 2.15.0/OpenAL Soft 1.21.1.1; DrippyAL 2.1.3 pulls Silk.NET.OpenAL 2.21.0 /
OpenAL Soft 1.23.1. The latter native package declares LGPL-2.0-or-later. These
examples stay outside the WebGPU platform. Missing license evidence for CSCore or
ppy.SDL2-CS is recorded rather than guessed; neither needs adoption to upgrade the
core. Future redistribution of a retained backend requires its own exact native/
transitive notices, not an assumption that this direct-package table is sufficient.

### Basic example build results

All 16 Release builds completed under SDK 10.0.203. NAudio was cross-built with
`-p:EnableWindowsTargeting=true`; no Windows/device execution occurred. Three
independent builds ran at most concurrently. Initially 14 passed; only FNA.NET and
MonoGame required the already described mutable-span corrections, and their
focused rebuilds passed. No package warnings were suppressed:

- CSCore: NU1701, the old package restores .NET Framework assets rather than a
  matching net10 target. Compiling does not establish runtime compatibility.
- DotFeather: NU1902/NU1903 for resolved SixLabors.ImageSharp 1.0.2. This dependency
  is not part of the WebGPU synthesis path; repairing it is outside basic retargeting.
- SFML.Net: NETSDK1206 for CSFML's version-specific `fedora-x64` RID. Native loading
  on that target remains untested.

The other 13 final build logs contain none of those warning codes. Receipts are in
`bin/validation/pre-upgrade-inventory/example-builds.json` and per-example logs.
To repeat an individual basic build:

```sh
dotnet build Examples/Raylib_cs/Raylib_csExample.csproj -c Release
dotnet build Examples/NAudio/NAudioExample.csproj -c Release -p:EnableWindowsTargeting=true
```

### Package refresh rule for difficult examples

Per the user's follow-up, check current maintained NuGet options **before** doing
custom repairs to a complicated example. A package need not explicitly target
net10.0 to be consumable by a .NET 10 application: compatible .NET Standard/net8/
net9 libraries may suffice, subject to API/native runtime acceptance. Conversely,
a successful compile or a newer version does not prove native-device support.

The following exact latest-stable versions were checked against official NuGet
version indices/manifests on 2026-11-23. None of the inspected latest manifests
advertises a dedicated net10.0 group; several advertise compatible older targets.
These are evaluated candidates, **not package changes applied to the examples**.
Already successful basic builds remain the acceptance baseline. Do not label a
project dead solely because its NuGet version has not changed.

| Package | Current example dependency | Latest stable checked | Decision |
| --- | --- | --- | --- |
| [CSCore](https://api.nuget.org/v3-flatcontainer/cscore/1.2.1.2/cscore.nuspec) | 1.2.1.2 | 1.2.1.2 | No newer stable package found; existing .NET Framework asset warning remains. Do not rebuild the backend for an unused example. |
| [DotFeather](https://api.nuget.org/v3-flatcontainer/dotfeather/3.2.0/dotfeather.nuspec) | 3.2.0 | 3.2.0 | No newer stable parent package found. Do not force a major ImageSharp/Drawing upgrade into its old binary dependency graph. |
| [SFML.Net](https://api.nuget.org/v3-flatcontainer/sfml.net/3.0.0/sfml.net.nuspec) | 2.6.0 | 3.0.0 | 3.x candidate exists (.NET Standard 2.0 metadata); native/API migration is separate, not needed for the successful basic build. |
| [CSFML](https://api.nuget.org/v3-flatcontainer/csfml/3.0.0/csfml.nuspec) | 2.6.0 | 3.0.0 | 3.x native package candidate accompanies SFML 3; validate native RIDs if this backend is ever needed. |
| [SixLabors.ImageSharp](https://api.nuget.org/v3-flatcontainer/sixlabors.imagesharp/4.1.3/sixlabors.imagesharp.nuspec) | 1.0.2 (resolved) | 4.1.3 | Latest metadata targets net8.0; usable target family for net10 consumers, not a proven binary-compatible fix for DotFeather 3.2.0. |
| [SixLabors.ImageSharp.Drawing](https://api.nuget.org/v3-flatcontainer/sixlabors.imagesharp.drawing/3.1.3/sixlabors.imagesharp.drawing.nuspec) | 1.0.0-beta11 (declared) | 3.1.3 | Latest metadata targets net8.0 and ImageSharp 4.1.3. Major-version API and packaged license need review if adopted. |
| [FNA.NET](https://api.nuget.org/v3-flatcontainer/fna.net/2.2.11.2602/fna.net.nuspec) | 1.0.15.2403 | 2.2.11.2602 | Latest includes net9.0/.NET Standard 2.0 groups and newer native assets. Candidate only; current example already builds after one span fix. |
| [MonoGame.Framework.DesktopGL](https://api.nuget.org/v3-flatcontainer/monogame.framework.desktopgl/3.8.6.1/monogame.framework.desktopgl.nuspec) | 3.8.2.1105 | 3.8.6.1 | Latest targets net8.0 and newer SDL/OpenAL packages. No replacement needed for current basic build. |
| [OpenTK](https://api.nuget.org/v3-flatcontainer/opentk/4.9.4/opentk.nuspec) | 4.8.2 | 4.9.4 | Newer meta-package available; inspect its OpenAL component and runtime dependencies if a future platform needs it. |
| [SoundFlow](https://api.nuget.org/v3-flatcontainer/soundflow/1.4.2/soundflow.nuspec) | 1.0.3 | 1.4.2 | Newer net8.0 package available. Do not migrate its engine API without a real WebGPU consumer. |

For CSCore/DotFeather, no newer stable parent package removes the recorded issues.
For SFML, a newer major version exists, but migrating an unused backend is outside
the basic upgrade. Leave all three as reference-only rather than expanding this
core modernization. The same rule applies to FNA, MonoGame, OpenTK or SoundFlow if
they later become actual platform requirements. Retain required backend/version/
license evidence before selecting an upgrade; do not pin new transitive versions
just to hide restore warnings. Raw check results are in
`bin/validation/pre-upgrade-inventory/package-refresh-review.json`.

## Asset identities and availability

The receipt hashes all 2,258 reference data files plus the root demo MIDI
(**2,259 assets total**). Aggregate asset-manifest SHA-256:
`ed2e5e495a7e2059c2069d7f8db4351a59b67e81f6287291722dae99ec9b93b7`.

| Reference group | Files | Total bytes |
| --- | --- | --- |
| Freeverb | 5 | 194,446 |
| Polyphone | 2,083 | 2,056,151 |
| TinySoundFont | 170 | 9,108,874 |
| Root `demo_song_for_arachno_soundfont.mid` | 1 | 17,438 |

The demo MIDI SHA-256 is
`bf6277150062d73e0d4bc4b98b1c6df30553192a439de13b4a394f6fa09afa24`.
The Freeverb impulse WAV SHA-256 is
`25b502afc5d113a7099f7957044b33d9d202bc0d84e026274a2f25f43ca5b33a`.
All 2,258 `None Update` copy items resolve after proper **MSBuild percent-escape
unescaping**; 53 escaped filenames only look missing if interpreted literally.

Required external fixtures absent from this checkout: `TimGM6mb.sf2`,
`Arachno SoundFont - Version 1.0.sf2`, `GeneralUser GS MuseScore v1.442.sf2`,
`SGM-V2.01.sf2`, `MuseScore_General.sf3`, and `flourish.mid`, `onestop.mid`, `town.mid`.
TestSettings loads banks in static fixture data; their absence and Windows-style
runtime reference paths prevent claiming full upstream-suite acceptance. Reference
CSV/TSV availability is not the same as having the originating banks. The root MIT
code license does not establish redistribution permission for external banks;
obtain their provenance before including them in an asset pack. No banks were
copied or downloaded in this task. Original focused in-memory MIDI/SoundFont
fixtures are identified by their versioned test-source hashes.

The separate WebGPU audio set is also hashed when `--webgpu-root` is supplied:
`neon-mile.sf2` is 200,876 bytes, SHA-256
`d22b7c814aaf45bb16b01a2b378475229082aa3dc6a043058996fa3059028770`;
`neon-mile.mid` is 1,549 bytes, SHA-256
`d34fbb9e2518cb2482be7f41812916dc746fb33f3624cc98025fe647f4c4f969`.
The receipt also includes its rendered music WAV and five cue WAV identities.
These remain external game assets, not newly bundled library fixtures. Existing
40-second offline smoke evidence is recorded in the prior audit receipts; it is
not a new device-playback result from this inventory.

## Packaging, scripts and IDE assumptions

- Package-on-build is enabled in the core; version remains 2.4.1. The generated
  local NuGet package includes LICENSE.txt, netstandard2.1 DLL and XML docs and
  contains no backend/native runtime dependency. Package/project/release-note URLs
  still identify upstream. Choose fork package/version/repository metadata before
  distributing the modified library; no modified fork package was published or
  installed over the cached upstream package.
- The SDK reports a missing package README. Review package README wiring in the
  packaging step; it does not block this inventory or the sixteen basic builds.
- Root `clean.bat` removes only core/test/Workspace bin/obj. `Examples/clean.bat`
  lists all 16 example bin/obj pairs. Both are Windows-only, working-directory-
  dependent, unconditional recursive deletion plus pause. They were inspected,
  **not executed**. They do not remove root `bin/validation` receipts. Use the
  intended solution/Examples working directory; portable cleanup can be addressed
  later without changing backend code.
- `Examples/build_all.bat` lists all 16 projects but assumes Examples is CWD,
  copies an unprovided local TimGM bank and an OpenTK `openal32.dll`, hardcodes
  output paths and lacks fail-fast checks. Only its framework output paths were
  updated to net10.0/net10.0-windows. It is not the acceptance harness used here.
- .gitignore already excludes bin/obj/packages/test results; codex is locally
  ignored. `.idea` configuration and `.DS_Store` files were left untouched. Rider
  metadata maps this project to Git; UTF-8/BOM policy and console working directory
  assumptions exist, but no shared launch profile supplies the missing assets.
  No IDE metadata was added as a build requirement.

## Reproduce and remaining work

From the solution root, this read-only tool validates solution links, inventories
project references/entry points, hashes source and reference assets, and records
available resolved dependency metadata. It does not restore packages or open audio:

```sh
python3 tools/pre_upgrade_inventory.py
# Optional: also identify the game assets in a separate checkout.
python3 tools/pre_upgrade_inventory.py --webgpu-root /path/to/KilnGod.BlazorWebGPU
```

Default output is ignored `bin/validation/pre-upgrade-inventory/inventory.json`.
NuGet manifest/license inspection receipts and raw build logs remain local. The
full source/asset manifest makes the shorter hashes here reproducible. Core/test/
Workspace migration, SDK pinning, fork package integration and applicable AOT/
playback acceptance remain their own steps. Nonessential example backend repair,
new loaders and missing-asset acquisition do not block core modernization.
