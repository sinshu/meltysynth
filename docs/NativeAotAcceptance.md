# NativeAOT, trimming and source delivery acceptance

macOS ARM64 acceptance completed 2026-11-23 with pinned SDK 10.0.404, runtime/
ILCompiler 10.0.15 and the selected Xcode 26.4.1 / macOS 26.4 SDK. This proves the
dependency-free core in a minimal console consumer on this Mac; it does not accept
Windows/Linux, mobile/TV packaging, signed distribution or an audio device.

## Reproduce

```sh
python3 tools/native_aot_smoke.py
python3 tools/regression_baseline.py --output-dir bin/validation/native-aot/regression
```

The macOS runner requires ARM64 and the selected Xcode toolchain. It scopes SDKROOT
to `xcrun --sdk macosx --show-sdk-path` for its child builds; no global Xcode or SDK
selection is changed. This avoids the observed mismatch where clang from Xcode
26.4 attempted to read the newer Command Line Tools macOS 27.2 text stubs.

All downloads/restores use the normal SDK/NuGet mechanisms. Generated fixtures,
source archives/extractions, local feeds, native maps, publish output and execution
receipts stay under ignored `bin/validation/native-aot`; intermediates stay in
`tools/AotSmoke/obj`. The runner creates a unique local validation package version
per run and verifies the restored archive SHA-512, preventing an upstream/stale
NuGet cache entry from passing as this fork. Nothing is published.

## Native and managed checks

The managed consumer establishes the frozen stereo output. Every native lane is
published with NativeAOT and trimming diagnostics enabled, warnings as errors and
no collapsed trimming warning. The core now declares `IsAotCompatible=true`, enabling
its AOT/trimming analyzers; this adds no delivered NuGet dependency. The probe uses
reflection-free JSON APIs and invariant globalization in the consumer, not as a
new globalization policy in the library.

Original assets are external files; their generator is compiled only in a separate
fixture-preparation configuration. It is excluded from the published consumer.
Each native distribution is extracted afresh with executable permissions preserved
and executed from that extraction. Every execution reports `IsDynamicCodeSupported
= false` and ARM64, checks asset SHA-256, frozen effects-on/off metrics/checkpoints,
repeated MIDI loops, 1/63/65/257/511-frame fragmentation and reset silence.

Six native scenarios across four delivery lanes each compare **65,536 stereo float
samples** with the managed run. All observed maximum absolute errors were **0**;
existing acceptance tolerances remain unchanged (1e-6 sample/checkpoint and the
frozen relative/absolute metric thresholds). Both full test compilation modes
also pass **343 self-contained / 48 external unavailable / 0 failed**, 391 discovered.
Root Release build: zero warnings/errors. Native publishes: zero compiler, AOT or
trimming warnings; no warning suppression or dynamic-code fallback.

## Size and feature reachability

| Delivery lane | Executable bytes | Model bytes | Golden data bytes | Effect methods retained | Effect-named method code bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| project-runtime | 1,850,712 | 806 | 3,087 | 11 | 4,804 |
| project-fixed-off | 1,834,200 | 806 | 3,087 | 11 | 4,804 |
| source-fixed-off | 1,834,200 | 806 | 3,087 | 11 | 4,804 |
| package-runtime | 1,850,712 | 806 | 3,087 | 11 | 4,804 |

The runtime lanes accept effects on/off at execution. The off-only entry points
call only the disabled configuration, including when compiling extracted source.
The 16,512-byte executable reduction **does not remove effects implementation**:
compiler maps retain the same 11 chorus/reverb-named methods, totaling 4,804 method
code bytes. Named map bytes exclude metadata, alignment and shared framework code;
they are evidence of reachability, not a complete accounting of feature size.
Runtime disabling avoids allocating/processing effect buffers (measured in the
audio audit), while the constructors/render path keep their code reachable.
A separate static feature boundary or build-time exclusion would require its own
contract and acceptance before promising a genuinely effects-free build. No such
DSP/source fork was added merely to improve this measurement.

Model storage is **760 SoundFont + 46 MIDI bytes**. The 3,087-byte frozen JSON file
is validation data, not model or engine size. Decoded sample storage is 348 bytes
for this tiny original fixture. Native debug symbols are recorded separately in
summary.json (about 8 MB per lane) and excluded from the distribution ZIPs. ZIPs
contain only the executable, three external validation assets and the MIT notice.

These sizes belong to a deliberately minimal consumer and fixture, not an estimate
of a game bundle, a full SoundFont bank or a signed Apple application. They include
the statically linked .NET native runtime/framework code used by the executable.

## License and packaging

The source archive contains **43 core C# files / 211,694 source bytes**,
plus manifest, SDK pin, integration README and the unchanged root MIT notice; ZIP size was **55,003 bytes**.
Every extracted source hash is checked before the source-native build. All C# Synth,
TinySoundFont, SFZero and MeltySynth copyright/attribution text remains intact.

The local NuGet has only net10.0 library/documentation, metadata and the preserved
license, with no package dependencies. Its unique validation version is recorded
in the receipt; the repository's normal package version remains unchanged. A real
NuGet restore uses and verifies that exact archive before its NativeAOT publish.

Every freshly extracted native distribution contains the exact MIT notice and
**zero standalone managed DLLs or native dylibs**. `otool -L` lists only system
libraries/frameworks (including Swift/Foundation/CryptoKit); no CoreCLR or hostfxr
library is loaded. System frameworks remain OS dependencies. NuGet DLLs are build
inputs and are linked into this native executable, not copied into its distribution.

NuGet license metadata does not automatically copy a notice to an application's
publish output. The probe explicitly ships LICENSE.txt. Consumers of either raw
source or NuGet must retain the notices in their actual application distribution.
Source integration can use the following alongside the app's normal target/AOT
properties, adjusting the source root:

```xml
<ItemGroup>
  <Compile Include="/path/to/source-extracted/src/*.cs" Link="MeltySynth/%(Filename)%(Extension)" />
  <None Include="/path/to/source-extracted/LICENSE.txt" Link="LICENSE.txt"
        CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

Do not combine this source inclusion with a second MeltySynth binary reference.
No device, host UI, game, NAudio or audit implementation is in the source archive.

## Remaining platform acceptance

- **Windows/Linux: outstanding.** Build and execute the same numerical smoke on
  each actual host/toolchain/RID, retain native dependency/maps and size evidence,
  and repeat source/NuGet delivery there. This Mac does not establish those results.
- **iOS/tvOS: outstanding.** Execute the numerical smoke inside appropriate host
  builds with the actual platform AOT/linker configuration. Inspect static/native
  linkage, trimming, bundle notices and simulator/device receipts separately.
  A desktop executable is not proof of mobile/TV packaging or App Store acceptance.

Evidence: `bin/validation/native-aot/summary.json`, per-lane publish logs, compiler
maps, effect method inventories, dependency lists and fresh-extraction run JSON.
The combined final check is `acceptance.json`; no platform deferral is marked done.
