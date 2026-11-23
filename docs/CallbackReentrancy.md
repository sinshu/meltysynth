<!--
SPDX-License-Identifier: MIT
Copyright (C) 2026 James Carpenter
Contributed by KilnGod (James Carpenter).
See LICENSE.txt for the full MIT license and inherited notices.
-->

# Sequencer callback reentrancy

Implemented for outline item 12 on 2026-11-23, .NET 10/C# 14, macOS ARM64.
`MidiFileSequencer` and its synthesizer still have one serialized owner. The new
render guard enforces same-thread playback reentrancy; it is not a lock or a
promise that concurrent calls are safe.

## Operations and event boundaries

`OnSendMessage` runs synchronously during event dispatch at a sequencer block
boundary, before the synthesizer renders that block. A hook may:

- inspect sequencer state and synchronously modify, forward or discard MIDI using
  the supplied synthesizer's validated MIDI/control APIs;
- change `Speed` using its existing numeric contract;
- replace or clear `OnSendMessage`; the captured current invocation finishes and
  the replacement applies to the next event, including another event at the same tick;
- signal its owner to change playback after the outer `Render` returns or unwinds.

Sequencer `Play`, `Stop` and recursive `Render` throw `InvalidOperationException`
while the outer render is active, before changing file, loop, cursor, clock,
partial-block state or synthesizer state. Nested rendering is rejected even for
empty spans. `Play` validates null/non-progressing-loop arguments first; those
retain `ArgumentNullException`/`ArgumentException` without mutation. Direct playback
changes after rendering retain their normal behavior, including replacing a file
from a partially consumed block and delivering the new file's initial events.

The owner must not independently render the synthesizer or share it with another
sequencer while this sequencer owns its audio timeline. Hooks do not establish a
second renderer owner. No task, deferred-command queue, extra event scanner or
alternate MIDI dispatcher was added. `Stop` and `Play` retain the configured hook,
as before; the caller owns its captured resources and can clear the property.

For example, request a stop without mutating the sequencer inside its hook:

```csharp
var stopRequested = false;
sequencer.OnSendMessage = (synth, channel, command, a, b) =>
{
    if (ShouldStop(channel, command, a, b))
    {
        stopRequested = true;
        return; // Deliberately drop this message.
    }
    synth.ProcessMidiMessage(channel, command, a, b);
};

sequencer.Render(left, right);
if (stopRequested) sequencer.Stop(); // Same owner, after the rendering call.
```

The owner chooses its output-buffer size, hence the latency of that request.
The library does not silently add pending commands or change their ordering.

## Exceptions and recovery

Each normal event's cursor advances immediately before its one delivery attempt.
If its hook forwards MIDI and then throws, the MIDI must not be sent again on
resume. If it throws before forwarding, that event is deliberately dropped rather
than guessed/retried. Exceptions keep their original type and identity. Pending
later events and loop indices remain available to the existing dispatcher.

Completed hook changes (such as MIDI, speed or hook replacement) remain effective;
there is no transactional rollback of arbitrary application callbacks. `Render`'s
`finally` releases its guard. The owner may render again, stop, or play a valid
replacement. For a failure before any block samples, no clock advance occurs. For
a failure after earlier blocks, their clock/voice advancement and written prefix
remain committed. Unwritten output is unspecified: discard the failed call's
buffers. Catch expected application errors inside the hook if uninterrupted output
is required. This policy does not retry side effects or swallow callback failures.

## Reproduction and verification

```sh
./bin/dotnet/dotnet test MeltySynthTest -c Release --filter FullyQualifiedName~SequencerReentrancyTest
python3 tools/regression_baseline.py --output-dir bin/validation/callback-reentrancy/regression
python3 tools/audio_audit.py --font /path/to/neon-mile.sf2 --midi /path/to/neon-mile.mid --output-dir bin/validation/callback-reentrancy/audio
```

The isolated pre-fix sequencer reproduction in
`bin/validation/callback-reentrancy/original-reproduction` records its commit/source
identities and runs a 64-frame Stop-from-hook case with a 45-second process limit.
It reproduces `NullReferenceException` after the file has already been cleared.
An initial recursive-render regression exposed the missing guard by overflowing
the test host; the checked-in reproduction now bounds its own recursion so a
future guard regression fails without recursively exhausting the test host.

All **26 new cases** pass. Coverage includes caught/uncaught Stop, valid Play and
nested Render rejection; invalid Play arguments; owner-side Stop/replacement;
same-tick hook replacement/clearing; forwarding and dropping; exceptions before
and after forwarding; replacement followed by failure; partial output/clock state;
allowed speed changes; and recovery across all four supported loop modes. Exact
audio comparisons verify no duplicate/omitted successful deliveries or skipped
initial replacement notes. Existing loop, FIFO, numeric-boundary and synthesis
regressions retain their original assertions.

The complete warning-as-error Release suite passes **462 self-contained tests,
48 unavailable external-bank comparisons, zero failures per compilation mode**:
project reference and actual C# 14 source inclusion, 510 discovered each. The
discovery floor is 462. Reference corpus and frozen synthesis data/tolerances are
unchanged. No hardware/platform or external-bank acceptance is inferred from this.

The ordinary Motel California score also loops successfully with effects off/on in
both plain and profiled source builds. Its exact stereo PCM hashes match item 11,
and measured rendering allocates zero bytes in those music runs. Existing timing
outliers remain recorded; this is no new frame/deadline performance claim. No fresh
browser, SDL3-device or acoustic run was needed/claimed for this boundary change.
Raw original-source reproduction, TRX, audio/timing data and consolidated acceptance
stay under ignored `bin/validation/callback-reentrancy/`; the versioned tests and
this contract remain reviewable outside ignored `codex/`.
