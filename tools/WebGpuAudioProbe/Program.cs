// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace KilnGod.WebGPU.BlazorNative;

internal static unsafe class NativeDesktopLibraries
{
    private static nint library;
    internal static void Register() { }
    internal static void Initialize(string path)
    {
        library = NativeLibrary.Load(Path.GetFullPath(path));
        NativeLibrary.SetDllImportResolver(typeof(MotelNativeAudio).Assembly, (name, _, _) => name == "SDL3" ? library : 0);
        var version = ((delegate* unmanaged[Cdecl]<int>)NativeLibrary.GetExport(library, "SDL_GetVersion"))();
        if (version != 3_004_020) throw new InvalidOperationException("Expected pinned SDL3 3.4.20.");
    }
    internal static string Driver => Marshal.PtrToStringUTF8(((delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(library, "SDL_GetCurrentAudioDriver"))()) ?? "unknown";
    internal static uint AudioInitialized => ((delegate* unmanaged[Cdecl]<uint, uint>)NativeLibrary.GetExport(library, "SDL_WasInit"))(0x10);
}

internal sealed partial class MotelNativeAudio
{
    private readonly ProbeSample[] probeSamples = new ProbeSample[4096];
    private readonly double[] probeCueEnergy = new double[5];
    private int probeCount, probeWraps, probeSeen, probeMinQueued = int.MaxValue, probeMaxQueued, probeDrains;
    private int probeLastQueued;
    private long probePosition;
    private double probeBlockFx;
    private readonly record struct ProbeSample(float Music, float Effects, double MusicEnergy, double FxEnergy, double StereoEnergy, double StereoDifference);
    private void ProbeQueue(int queued)
    {
        probeMinQueued = Math.Min(probeMinQueued, queued); probeMaxQueued = Math.Max(probeMaxQueued, queued);
        if (queued < probeLastQueued) probeDrains++; probeLastQueued = queued;
    }
    private void ProbeBegin() => probeBlockFx = 0;
    private void ProbeCueSeen(int cue) => probeSeen |= 1 << cue;
    private void ProbeEffect(int cue, int cursor, int frames, float left, float right)
    {
        double energy = 0;
        for (var i = 0; i < frames; i++)
        {
            var sample = _effects[cue][cursor + i]; energy += sample * sample * ((double)left * left + (double)right * right);
        }
        probeCueEnergy[cue] += energy; probeBlockFx += energy;
    }
    private void ProbeCommit(float[] left, float[] right, float[] stereo, float music, float effects)
    {
        if (_score.Position.Ticks < probePosition) probeWraps++;
        probePosition = _score.Position.Ticks;
        double musicEnergy = 0, totalEnergy = 0, difference = 0;
        for (var i = 0; i < left.Length; i++)
        {
            musicEnergy += ((double)left[i] * left[i] + (double)right[i] * right[i]) * music * music;
            var a = stereo[i * 2]; var b = stereo[i * 2 + 1];
            if (!float.IsFinite(a) || !float.IsFinite(b)) throw new InvalidDataException("Nonfinite mixed stereo.");
            totalEnergy += (double)a * a + (double)b * b; difference += Math.Abs(a - b);
        }
        if (probeCount == probeSamples.Length) throw new InvalidOperationException("Probe observation budget exhausted.");
        probeSamples[probeCount++] = new(music, effects, musicEnergy, probeBlockFx, totalEnergy, difference);
    }
    internal object ProbeResult()
    {
        // Inspect only after Dispose has joined Feed, never race mutable synthesis state.
        var blocks = probeSamples.AsSpan(0, probeCount).ToArray();
        var effectsOnly = blocks.Where(x => x.Music == 0 && x.Effects > 0).ToArray();
        var musicOnly = blocks.Where(x => x.Music > 0 && x.Effects == 0).ToArray();
        var muted = blocks.Where(x => x.Music == 0 && x.Effects == 0).ToArray();
        var independent = effectsOnly.Length > 0 && effectsOnly.Sum(x => x.MusicEnergy) == 0 && effectsOnly.Sum(x => x.FxEnergy) > 0 &&
            musicOnly.Length > 0 && musicOnly.Sum(x => x.MusicEnergy) > 0 && musicOnly.Sum(x => x.FxEnergy) == 0 &&
            muted.Length > 0 && muted.Sum(x => x.StereoEnergy) == 0;
        if (probeWraps < 1 || probeSeen != 31 || probeCueEnergy.Any(x => x <= 0) || !independent || probeDrains == 0 || probeMaxQueued > BlockBytes * 3 || blocks.Sum(x => x.StereoDifference) <= 0)
            throw new InvalidOperationException("Loop, cue, stereo, mute or device queue acceptance failed.");
        return new { blocks = probeCount, loopWraps = probeWraps, seenCueMask = probeSeen, cueEnergy = probeCueEnergy,
            independentVolumesPassed = independent, finiteStereo = true, stereoDifference = blocks.Sum(x => x.StereoDifference),
            minQueuedBytes = probeMinQueued, maxQueuedBytes = probeMaxQueued, drainObservations = probeDrains,
            feedJoined = Completion.IsCompletedSuccessfully, scope = "Actual adapter mixed/queued output; no acoustic recording or hard-real-time claim." };
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        NativeDesktopLibraries.Initialize(args[0]);
        var audio = new MotelNativeAudio();
        var driver = NativeDesktopLibraries.Driver;
        if (driver is "dummy" or "disk" or "unknown") { audio.Dispose(); throw new InvalidOperationException("A real default audio device is required."); }
        try
        {
            Thread.Sleep(17_500);
            audio.MusicVolume = 0; audio.EffectsVolume = .75F;
            for (var i = 0; i < 5; i++) { audio.Play((MotelCue)i); Thread.Sleep(900); }
            audio.MusicVolume = .55F; audio.EffectsVolume = 0; audio.Play(MotelCue.Setback); Thread.Sleep(1000);
            audio.MusicVolume = 0; audio.EffectsVolume = 0; audio.Play(MotelCue.Finish); Thread.Sleep(750);
            if (audio.Completion.IsCompleted) audio.Completion.GetAwaiter().GetResult();
        }
        finally { audio.Dispose(); }
        var observation = audio.ProbeResult();
        if (NativeDesktopLibraries.AudioInitialized != 0) throw new InvalidOperationException("SDL audio subsystem retained after disposal.");
        using (var reopened = new MotelNativeAudio()) { Thread.Sleep(250); if (reopened.Completion.IsCompleted) reopened.Completion.GetAwaiter().GetResult(); }
        if (NativeDesktopLibraries.AudioInitialized != 0) throw new InvalidOperationException("Repeated close retained audio.");
        Console.WriteLine(JsonSerializer.Serialize(new { accepted = true, driver, forkAssembly = typeof(MeltySynth.Synthesizer).Assembly.Location,
            observation, repeatedOpenClosePassed = true, appUsesDirectProjectReference = true }));
    }
}
