using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MeltySynth;
using MeltySynthTest;

internal static class Program
{
    private const int Rate = 44100, Frames = 64, Warmup = 1024;
    private static readonly double TickUs = 1_000_000.0 / Stopwatch.Frequency;
    private static readonly long Deadline = (long)((double)Frames / Rate * Stopwatch.Frequency);
    private static int Main(string[] args)
    {
        var output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output);
        var blocks = args.Length > 1 ? int.Parse(args[1]) : 4000;
        var trials = args.Length > 2 ? int.Parse(args[2]) : 5;
        if (blocks is < 100 or > 100000 || trials is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(args));
#if MELTYSYNTH_AUDIO_AUDIT
        const bool profiled = true;
#else
        const bool profiled = false;
#endif
        var hooksPresent = typeof(Synthesizer).GetProperty("AuditCounters", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) != null;
        if (hooksPresent != profiled) throw new InvalidOperationException("Audit hooks leaked into the wrong build.");
        var fixture = Path.Combine(output, "original.sf2");
        var score = Path.Combine(output, "original.mid");
        File.WriteAllBytes(fixture, MidiLoopTestAssets.SoundFontBytes());
        File.WriteAllBytes(score, MidiLoopTestAssets.MidiBytes(MidiLoopTestAssets.Track(
            (0, new byte[] { 0x90, 60, 100 }), (96, new byte[] { 0x80, 60, 0 }), (96, new byte[] { 0xFF, 0x2F, 0 }))));
        var (fontBytes, fontRead) = Once(() => File.ReadAllBytes(fixture));
        var (midiBytes, midiRead) = Once(() => File.ReadAllBytes(score));
        var (font, fontDecode) = Once(() => { using var stream = new MemoryStream(fontBytes, false); return new SoundFont(stream); });
        var (midi, midiDecode) = Once(() => { using var stream = new MemoryStream(midiBytes, false); return new MidiFile(stream); });
        var results = new List<object>();
        foreach (var voices in new[] { 8, 256 })
        foreach (var effects in new[] { false, true })
        {
            var (synth, setup) = Once(() => Synth(font, voices, effects));
            var left = new float[Frames]; var right = new float[Frames];
            Action notes = () => { synth.NoteOffAll(true); StartNotes(synth, voices); };
            var events = Measure(notes, blocks, trials, "events", output, voices, effects);
            synth.Reset(); StartNotes(synth, voices);
            for (var i = 0; i < Warmup; i++) synth.Render(left, right);
            if (synth.ActiveVoiceCount != voices) throw new InvalidOperationException("The workload did not sustain its requested polyphony.");
#if MELTYSYNTH_AUDIO_AUDIT
            var counters = new AudioAuditCounters(); synth.AuditCounters = counters;
#endif
            var total = new long[blocks * trials];
            var voiceTicks = new long[total.Length]; var dryTicks = new long[total.Length];
            var chorusTicks = new long[total.Length]; var reverbTicks = new long[total.Length];
            long allocated = 0; var trialAllocatedBytes = new long[trials];
            for (var trial = 0; trial < trials; trial++)
            {
                synth.Reset(); StartNotes(synth, voices);
                for (var warm = 0; warm < Warmup; warm++) synth.Render(left, right);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var sample = 0; sample < blocks; sample++)
                {
                    var i = trial * blocks + sample;
                    var began = Stopwatch.GetTimestamp(); synth.Render(left, right); total[i] = Stopwatch.GetTimestamp() - began;
#if MELTYSYNTH_AUDIO_AUDIT
                    voiceTicks[i] = counters.Voices; dryTicks[i] = counters.DryMix;
                    chorusTicks[i] = counters.Chorus; reverbTicks[i] = counters.Reverb;
#endif
                }
                trialAllocatedBytes[trial] = GC.GetAllocatedBytesForCurrentThread() - before;
                allocated += trialAllocatedBytes[trial];
            }
            if (synth.ActiveVoiceCount != voices || !left.All(float.IsFinite) || !right.All(float.IsFinite))
                throw new InvalidOperationException("Inactive or nonfinite render workload.");
            Raw(output, voices, effects, "render", total);
#if MELTYSYNTH_AUDIO_AUDIT
            Raw(output, voices, effects, "voices", voiceTicks); Raw(output, voices, effects, "dry-mix", dryTicks);
            Raw(output, voices, effects, "chorus", chorusTicks); Raw(output, voices, effects, "reverb", reverbTicks);
#endif
            var render = Stats(total, allocated, trials);
            var sequence = new MidiFileSequencer(Synth(font, voices, effects)); sequence.Play(midi, true);
            var sequencer = Measure(() => sequence.Render(left, right), blocks, trials, "sequencer", output, voices, effects);
            var source = new FixedRenderer(left, right);
            var f32 = new float[Frames * 2]; var i16 = new short[Frames * 2];
            var formatFloat = Measure(() => source.RenderInterleaved(f32), blocks, trials, "float-interleave", output, voices, effects);
            var formatShort = Measure(() => source.RenderInterleavedInt16(i16), blocks, trials, "int16-interleave", output, voices, effects);
            var queue = QueueProbe(font, voices, effects, 256);
            var checksum = Hash(Synth(font, voices, effects), voices, 256);
            results.Add(new { voices, maximumPolyphony = synth.MaximumPolyphony, effects, setup, events, render, renderTrialAllocatedBytes = trialAllocatedBytes,
                stageAllocationBytes = allocated == 0 ? (long?)0 : null,
                stageAllocationsScope = "A zero whole-render allocation count implies zero allocations in every enclosed stage; otherwise no stage allocation attribution is claimed.",
                stages = profiled ? new { voices = Stats(voiceTicks, null, trials), dryMix = Stats(dryTicks, null, trials), chorus = Stats(chorusTicks, null, trials), reverb = Stats(reverbTicks, null, trials) } : null,
                sequencerLoop = sequencer, formatFloat, formatShort, queue, pcmSha256 = checksum });
            Console.WriteLine($"{(profiled ? "profiled" : "plain")} voices={voices} effects={effects}: median={render.MedianUs:F2}us p99={render.P99Us:F2}us max={render.MaxUs:F2}us alloc={allocated}");
        }
        var music = new List<object>();
        if (args.Length > 4)
        {
            var (musicFontBytes, readFont) = Once(() => File.ReadAllBytes(args[3]));
            var (musicMidiBytes, readMidi) = Once(() => File.ReadAllBytes(args[4]));
            var (musicFont, decodeFont) = Once(() => { using var source = new MemoryStream(musicFontBytes, false); return new SoundFont(source); });
            var (musicMidi, decodeMidi) = Once(() => { using var source = new MemoryStream(musicMidiBytes, false); return new MidiFile(source); });
            foreach (var effects in new[] { false, true })
            {
                var (synth, setup) = Once(() => Synth(musicFont, 8, effects));
                var sequence = new MidiFileSequencer(synth); sequence.Play(musicMidi, true);
                var left = new float[Frames]; var right = new float[Frames]; var peakVoices = 0; var wraps = 0;
                Action render = () =>
                {
                    var before = sequence.Position; sequence.Render(left, right);
                    if (sequence.Position < before) wraps++;
                    peakVoices = Math.Max(peakVoices, synth.ActiveVoiceCount);
                };
                var timing = Measure(render, blocks, trials, "real-music", output, 64, effects);
                if (wraps < 1 || peakVoices == 0) throw new InvalidOperationException("The real music observation must include an audible MIDI wrap; increase sample count.");
                var reference = new MidiFileSequencer(Synth(musicFont, 8, effects)); reference.Play(musicMidi, true);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var pcm = new short[Frames * 2];
                for (var i = 0; i < 4096; i++) { reference.RenderInterleavedInt16(pcm); hash.AppendData(MemoryMarshal.AsBytes(pcm.AsSpan())); }
                music.Add(new { effects, maximumPolyphony = 64, setup, timing, peakVoices, wraps,
                    sourceFont = Path.GetFileName(args[3]), sourceMidi = Path.GetFileName(args[4]),
                    fontSha256 = Convert.ToHexString(SHA256.HashData(musicFontBytes)), midiSha256 = Convert.ToHexString(SHA256.HashData(musicMidiBytes)),
                    sourceFontBytes = musicFontBytes.Length, sourceMidiBytes = musicMidiBytes.Length,
                    retainedPcmBytes = (long)musicFont.WaveData.Length * sizeof(short),
                    retainedMidiEventPayloadBytes = (long)musicMidi.Messages.Length * Marshal.SizeOf<MidiFile.Message>() + (long)musicMidi.Times.Length * sizeof(long),
                    readFont, readMidi, decodeFont, decodeMidi, pcmSha256 = Convert.ToHexString(hash.GetHashAndReset()) });
            }
        }
        var empty = Measure(() => { _ = Stopwatch.GetTimestamp(); _ = Stopwatch.GetTimestamp(); }, blocks, trials, "timer-pair", output, 0, false);
        var receipt = new { schemaVersion = 1, profiled, hooksPresent, rate = Rate, frames = Frames, deadlineUs = (double)Frames / Rate * 1E6,
            blocksPerTrial = blocks, trials, warmupBlocks = Warmup, stopwatchFrequency = Stopwatch.Frequency,
            runtime = Environment.Version.ToString(), tieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "runtime-default", architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            os = RuntimeInformation.OSDescription, simd = Vector.IsHardwareAccelerated, vectorFloatCount = Vector<float>.Count,
            fixture = new { fontSha256 = Convert.ToHexString(SHA256.HashData(fontBytes)), midiSha256 = Convert.ToHexString(SHA256.HashData(midiBytes)),
                planarCopyBytesPerRenderBlock = Frames * 2 * sizeof(float), sourceFontBytes = fontBytes.Length, sourceMidiBytes = midiBytes.Length,
                retainedPcmBytes = (long)font.WaveData.Length * sizeof(short),
                retainedMidiEventPayloadBytes = (long)midi.Messages.Length * Marshal.SizeOf<MidiFile.Message>() + (long)midi.Times.Length * sizeof(long),
                scope = "Tracked PCM/event array payload only, excluding object headers/metadata. Source byte arrays are retained by this audit, not by the runtime loaders." },
            loading = new { fontRead, midiRead, fontDecode, midiDecode, scope = "Original tiny fixture, warm filesystem. Read bytes separately from in-memory parse/PCM decode; not cold disk performance." },
            timerPair = empty, results, music,
            scope = "Use plain render timings for performance. Profiled stages include timestamp overhead and are attribution only. Formats include actual ArrayPool and span copies. Queue sink is a paced offline simulation, not device playback. No runtime tuning or per-voice parallelism." };
        File.WriteAllText(Path.Combine(output, "receipt.json"), JsonSerializer.Serialize(receipt, JsonOptions()) + "\n");
        return 0;
    }

    internal static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private sealed record Distribution(double MedianUs, double P99Us, double MaxUs, long? AllocatedBytes, int Samples, int? DeadlineMisses, double[] TrialMedianUs);
    private static Distribution Stats(long[] values, long? allocated, int trials = 1, bool deadlineApplicable = true)
    {
        var ordered = (long[])values.Clone(); Array.Sort(ordered);
        var trialMedians = new double[trials];
        for (var trial = 0; trial < trials; trial++)
        {
            var slice = values.AsSpan(trial * values.Length / trials, values.Length / trials).ToArray(); Array.Sort(slice);
            trialMedians[trial] = slice[slice.Length / 2] * TickUs;
        }
        return new(ordered[ordered.Length / 2] * TickUs, ordered[(int)Math.Ceiling(ordered.Length * .99) - 1] * TickUs,
            ordered[^1] * TickUs, allocated, values.Length, deadlineApplicable ? values.Count(x => x > Deadline) : null, trialMedians);
    }
    private static Distribution Measure(Action action, int blocks, int trials, string name, string output, int voices, bool effects)
    {
        for (var i = 0; i < Warmup; i++) action();
        var ticks = new long[blocks * trials]; var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < ticks.Length; i++) { var start = Stopwatch.GetTimestamp(); action(); ticks[i] = Stopwatch.GetTimestamp() - start; }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Raw(output, voices, effects, name, ticks);
        return Stats(ticks, allocated, trials);
    }
    private static (T Value, object Stats) Once<T>(Func<T> operation)
    {
        var allocated = GC.GetAllocatedBytesForCurrentThread(); var began = Stopwatch.GetTimestamp();
        var value = operation(); var elapsed = Stopwatch.GetTimestamp() - began;
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        return (value, new { microseconds = elapsed * TickUs, allocatedBytes = allocated, scope = "One setup observation, includes first-use/JIT; not a steady-state distribution." });
    }
    private static void Raw(string directory, int voices, bool effects, string stage, long[] ticks)
    {
        using var writer = new StreamWriter(Path.Combine(directory, $"{voices}-{effects}-{stage}.csv"));
        writer.WriteLine("sample,ticks"); for (var i = 0; i < ticks.Length; i++) writer.WriteLine($"{i},{ticks[i]}");
    }
    private static Synthesizer Synth(SoundFont font, int voices, bool effects) => new(font,
        new SynthesizerSettings(Rate) { BlockSize = Frames, MaximumPolyphony = voices == 8 ? 64 : 256, EnableReverbAndChorus = effects }) { MasterVolume = .01F };
    private static void StartNotes(Synthesizer synth, int voices)
    {
        for (var channel = 0; channel < 4; channel++) { synth.ProcessMidiMessage(channel, 0xB0, 0x5B, 64); synth.ProcessMidiMessage(channel, 0xB0, 0x5D, 64); }
        for (var i = 0; i < voices; i++) synth.NoteOn(i / 64, 32 + i % 64, 100);
    }
    private static string Hash(Synthesizer synth, int voices, int blocks)
    {
        StartNotes(synth, voices); using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var pcm = new short[Frames * 2];
        for (var i = 0; i < blocks; i++) { synth.RenderInterleavedInt16(pcm); hash.AppendData(MemoryMarshal.AsBytes(pcm.AsSpan())); }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private static object QueueProbe(SoundFont font, int voices, bool effects, int blocks)
    {
        using var pipe = new BoundedAudioPipe(4, Frames); using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var waits = new long[blocks]; var completion = new long[blocks]; var producedAt = new long[blocks];
        long producerBytes = 0, consumerBytes = 0; int producerThread = 0, consumerThread = 0, consumed = 0;
        string checksum = ""; long began = 0;
        var producer = Task.Factory.StartNew(() =>
        {
            try
            {
                producerThread = Environment.CurrentManagedThreadId;
                var warm = Synth(font, voices, effects); var scratch = new short[Frames * 2]; StartNotes(warm, voices);
                for (var i = 0; i < Warmup; i++) warm.RenderInterleavedInt16(scratch);
                var synth = Synth(font, voices, effects); StartNotes(synth, voices);
                Action<short[], int> fill = (samples, index) => { synth.RenderInterleavedInt16(samples); producedAt[index] = Stopwatch.GetTimestamp(); };
                var allocated = GC.GetAllocatedBytesForCurrentThread(); began = Stopwatch.GetTimestamp();
                for (var i = 0; i < blocks; i++) waits[i] = pipe.Produce(i, fill, cancellation.Token);
                producerBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                pipe.Complete();
            }
            catch { cancellation.Cancel(); throw; }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        var consumer = Task.Factory.StartNew(() =>
        {
            try
            {
                consumerThread = Environment.CurrentManagedThreadId;
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                long target = 0;
                PcmConsumer sink = (samples, index) =>
                {
                    if (index != consumed) throw new InvalidOperationException("Out-of-order PCM.");
                    hash.AppendData(MemoryMarshal.AsBytes(samples));
                    if (consumed == 0) target = Stopwatch.GetTimestamp();
                    target += Deadline;
                    while (Stopwatch.GetTimestamp() < target)
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        if (target - Stopwatch.GetTimestamp() > Stopwatch.Frequency / 500) Thread.Sleep(1);
                        else Thread.SpinWait(30);
                    }
                    completion[index] = Stopwatch.GetTimestamp() - producedAt[index]; consumed++;
                };
                var allocated = GC.GetAllocatedBytesForCurrentThread(); pipe.Consume(sink, cancellation.Token);
                consumerBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                checksum = Convert.ToHexString(hash.GetHashAndReset());
            }
            catch { cancellation.Cancel(); throw; }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task.WaitAll(producer, consumer);
        var elapsed = Stopwatch.GetTimestamp() - began;
        var reference = Hash(Synth(font, voices, effects), voices, blocks);
        if (checksum != reference || consumed != blocks || pipe.AvailableBlocks != 4 || producerThread == consumerThread)
            throw new InvalidOperationException("Queue integrity/ownership failure.");
        return new { capacity = 4, pipe.PeakQueueDepth, pipe.ResidentPcmBytes, allBuffersReturned = pipe.AvailableBlocks == 4,
            blocks, consumed, producerThread, consumerThread, producerAllocatedBytes = producerBytes, consumerAllocatedBytes = consumerBytes,
            producerQueueWait = Stats(waits, null, deadlineApplicable: false), fillToConsumerCompletion = Stats(completion, null, deadlineApplicable: false),
            drainCompletionMilliseconds = elapsed * TickUs / 1000, pcmMatchesSequential = true,
            scope = "Paced offline sink, no device/OS callback. Queue wait includes free-slot acquisition/publication; completion includes sink pacing. DeadlineMisses here are not DSP underruns." };
    }
    private sealed class FixedRenderer(float[] left, float[] right) : IAudioRenderer
    {
        public void Render(Span<float> destinationLeft, Span<float> destinationRight) { left.CopyTo(destinationLeft); right.CopyTo(destinationRight); }
    }
}
