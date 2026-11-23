using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MeltySynth;

public static class WorkspaceCommand
{
    public static int Run(string[] args, TextWriter output)
    {
        if (args.Length == 0 || args[0] is "--help" or "help")
        {
            output.WriteLine("Workspace chord|examples|benchmark|validate [--assets DIRECTORY] [--soundfont FILE] [--midi FILE] [--seconds 1..300] [--run NAME] [--profile JSON]");
            output.WriteLine("Inputs: TimGM6mb.sf2 and flourish.mid. Outputs: repository bin/workspace/NAME (default latest). Benchmark defaults to 10 seconds.");
            return 0;
        }
        var command = args[0];
        if (command is not ("chord" or "examples" or "benchmark" or "validate")) throw new ArgumentException("Unknown Workspace command: " + command);
        string? assets = null;
        string? profile = null;
        var seconds = 10;
        var soundFontName = "TimGM6mb.sf2"; var midiName = "flourish.mid"; var run = "latest";
        for (var i = 1; i < args.Length; i += 2)
        {
            if (i + 1 == args.Length) throw new ArgumentException("Missing option value: " + args[i]);
            if (args[i] == "--assets") assets = args[i + 1];
            else if (args[i] == "--soundfont") soundFontName = args[i + 1];
            else if (args[i] == "--midi") midiName = args[i + 1];
            else if (args[i] == "--profile" && command == "validate") profile = args[i + 1];
            else if (args[i] == "--run") run = args[i + 1];
            else if (args[i] == "--seconds" && command == "benchmark" && int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is >= 1 and <= 300) seconds = value;
            else throw new ArgumentException("Invalid option/value: " + args[i]);
        }
        if (command == "validate" && profile == null) throw new ArgumentException("validate requires --profile JSON.");
        var paths = ToolPaths.FromEnvironment(assets, soundFontName, midiName, run);
        // Resolve every required input before creating any output.
        var font = paths.Asset(paths.SoundFontName);
        var midi = command == "chord" ? null : paths.Asset(paths.MidiName);
        if (command == "validate")
        {
            var limits = AssetPreflight.ReadProfile(profile ?? throw new InvalidOperationException());
            var receipt = AssetPreflight.Validate(font, midi ?? throw new InvalidOperationException(), limits);
            var json = JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true });
            // The receipt is the readiness marker, published only after all validation succeeds.
            var pending = paths.Output("assets.validated.json.partial");
            try
            {
                File.WriteAllText(pending, json + Environment.NewLine);
                File.Move(pending, paths.Output("assets.validated.json"), overwrite: false);
            }
            finally { File.Delete(pending); }
            output.WriteLine(json);
        }
        else if (command == "chord") Examples.SimpleChord(paths);
        else if (command == "examples") Examples.RunAll(paths);
        else
        {
            var settings = new SynthesizerSettings(44100);
            var elapsed = Benchmark.Run(font, midi ?? throw new InvalidOperationException(), seconds, settings, paths.Output("benchmark.wav"));
            var receipt = new { schemaVersion = 1, sampleRate = settings.SampleRate, frames = seconds * settings.SampleRate,
                seconds, renderMilliseconds = elapsed.TotalMilliseconds, soundFont = font, midi,
                scope = "Single offline render; excludes loading and WAV writing. Not a warmed performance baseline." };
            var json = JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(paths.Output("benchmark.json"), json + Environment.NewLine);
            output.WriteLine(json);
        }
        output.WriteLine("Outputs: " + paths.OutputDirectory);
        return 0;
    }
}
