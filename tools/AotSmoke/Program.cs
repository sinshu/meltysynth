using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using MeltySynth;

internal static class Program
{
    private const int Frames = 32768;
    private static int Main(string[] args)
    {
#if PREPARE_FIXTURES
        Directory.CreateDirectory(args[0]);
        File.WriteAllBytes(Path.Combine(args[0], "Original.sf2"), MeltySynthTest.MidiLoopTestAssets.SoundFontBytes());
        File.WriteAllBytes(Path.Combine(args[0], "Original.mid"), MeltySynthTest.SynthesisBaselineScenario.MidiBytes());
        return 0;
#else
        var output = Path.GetFullPath(args[0]); Directory.CreateDirectory(output);
        var assets = Path.Combine(AppContext.BaseDirectory, "assets");
        using var golden = JsonDocument.Parse(File.ReadAllText(Path.Combine(assets, "synthesis.json")));
        var root = golden.RootElement;
        if (root.GetProperty("frames").GetInt32() != Frames) throw new InvalidDataException("Unexpected baseline profile.");
        CheckHash(Path.Combine(assets, "Original.sf2"), root.GetProperty("soundFontSha256").GetString());
        CheckHash(Path.Combine(assets, "Original.mid"), root.GetProperty("midiSha256").GetString());
        VerifyAssetViews(new SoundFont(Path.Combine(assets, "Original.sf2")));
        using var json = new Utf8JsonWriter(Console.OpenStandardOutput());
        json.WriteStartObject(); json.WriteBoolean("dynamicCodeSupported", System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported);
        json.WriteBoolean("assetCollectionsReadOnly", true);
        json.WriteString("architecture", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString());
        json.WriteStartArray("scenarios");
#if FIXED_EFFECTS_OFF
        Run(false, assets, output, root, json);
#else
        // A runtime argument intentionally keeps both optional effects paths reachable.
        var mode = args.Length > 1 ? args[1] : "both";
        if (mode is "both" or "off") Run(false, assets, output, root, json);
        if (mode is "both" or "on") Run(true, assets, output, root, json);
        if (mode is not ("both" or "off" or "on")) throw new ArgumentException("Use both, off or on.");
#endif
        json.WriteEndArray(); json.WriteBoolean("accepted", true); json.WriteEndObject(); json.Flush();
        return 0;
#endif
    }
#if !PREPARE_FIXTURES
    private static void VerifyAssetViews(SoundFont font)
    {
        CheckView(() => font.SampleHeaders); CheckView(() => font.Instruments); CheckView(() => font.Presets);
        foreach (var instrument in font.Instruments) CheckView(() => instrument.Regions);
        foreach (var preset in font.Presets) CheckView(() => preset.Regions);
    }
    private static void CheckView<T>(Func<IReadOnlyList<T>> get) where T : class
    {
        var view = get();
        if (view is T[] || view is IList<T> || view is ICollection<T> || view is IList || view is ICollection || !ReferenceEquals(view, get()))
            throw new InvalidDataException("Prepared collection exposes mutable storage or an unstable view.");
        var index = 0;
        foreach (var item in view)
            if (!ReferenceEquals(item, view[index++])) throw new InvalidDataException("Prepared collection identity/order changed.");
        if (index != view.Count) throw new InvalidDataException("Prepared collection count changed.");
    }

    private static Synthesizer Synth(string assets, bool effects) => new(new SoundFont(Path.Combine(assets, "Original.sf2")),
        new SynthesizerSettings(32000) { BlockSize = 64, MaximumPolyphony = 8, EnableReverbAndChorus = effects }) { MasterVolume = .5F };
    private static (float[] Left, float[] Right) Render(string assets, bool effects, bool fragmented)
    {
        var sequence = new MidiFileSequencer(Synth(assets, effects));
        var midi = new MidiFile(Path.Combine(assets, "Original.mid")); sequence.Play(midi, true);
        var left = new float[Frames]; var right = new float[Frames];
        if (!fragmented) sequence.Render(left, right);
        else
        {
            var sizes = new[] { 1, 63, 65, 257, 511 };
            for (int at = 0, n = 0; at < Frames; n++)
            {
                var count = Math.Min(Frames - at, sizes[n % sizes.Length]);
                sequence.Render(left.AsSpan(at, count), right.AsSpan(at, count)); at += count;
            }
        }
        return (left, right);
    }
    private static void Run(bool effects, string assets, string output, JsonElement root, Utf8JsonWriter json)
    {
        var actual = Render(assets, effects, false); var fragmented = Render(assets, effects, true);
        var metrics = new double[4];
        for (var i = 0; i < Frames; i++)
        {
            var left = actual.Left[i]; var right = actual.Right[i];
            if (!float.IsFinite(left) || !float.IsFinite(right) || Math.Abs(left - fragmented.Left[i]) > 1E-6 || Math.Abs(right - fragmented.Right[i]) > 1E-6)
                throw new InvalidDataException("Nonfinite or fragmented output mismatch.");
            metrics[0] += (double)left * left; metrics[1] += (double)right * right;
            metrics[2] = Math.Max(metrics[2], Math.Abs(left)); metrics[3] = Math.Max(metrics[3], Math.Abs(right));
        }
        var expected = root.GetProperty("scenarios")[effects ? 1 : 0];
        for (var i = 0; i < metrics.Length; i++)
        {
            var value = expected.GetProperty("metrics")[i].GetDouble();
            if (Math.Abs(value - metrics[i]) > Math.Max(1E-6, Math.Abs(value) * 1E-6)) throw new InvalidDataException("Frozen synthesis metric changed.");
        }
        foreach (var point in expected.GetProperty("checkpoints").EnumerateArray())
        {
            var frame = point.GetProperty("frame").GetInt32();
            if (Math.Abs(actual.Left[frame] - point.GetProperty("left").GetDouble()) > 1E-6 || Math.Abs(actual.Right[frame] - point.GetProperty("right").GetDouble()) > 1E-6)
                throw new InvalidDataException("Frozen synthesis checkpoint changed.");
        }
        var synth = Synth(assets, effects); var silence = new float[513]; var rightSilence = new float[513];
        synth.NoteOn(0, 60, 100); synth.Render(silence, rightSilence); synth.Reset(); synth.Render(silence, rightSilence);
        for (var i = 0; i < silence.Length; i++) if (silence[i] != 0 || rightSilence[i] != 0) throw new InvalidDataException("Reset is not silent.");
        using var binary = new BinaryWriter(File.Create(Path.Combine(output, effects ? "effects-on.f32" : "effects-off.f32")));
        for (var i = 0; i < Frames; i++) { binary.Write(actual.Left[i]); binary.Write(actual.Right[i]); }
        json.WriteStartObject(); json.WriteBoolean("effects", effects); json.WriteBoolean("frozenNumericsPassed", true);
        json.WriteBoolean("fragmentationAndResetPassed", true); json.WriteStartArray("metrics");
        foreach (var metric in metrics) json.WriteNumberValue(metric);
        json.WriteEndArray(); json.WriteEndObject();
    }
    private static void CheckHash(string path, string? expected)
    {
        using var source = File.OpenRead(path);
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(source)), expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Asset identity mismatch.");
    }
#endif
}
