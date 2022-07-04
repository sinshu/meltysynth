using System;
using System.Collections.Generic;
using System.Drawing;
using DotFeather;
using MeltySynth;

class MidiExampleScene : Scene
{
    private AudioPlayer audioPlayer;
    private MidiAudioStream audioStream;

    public MidiExampleScene()
    {
        audioPlayer = new AudioPlayer();
        audioStream = new MidiAudioStream("TimGM6mb.sf2");

        // Load the MIDI file.
        var midiFile = new MidiFile(@"C:\Windows\Media\flourish.mid");

        // Play the MIDI file.
        audioStream.Play(midiFile, true);
    }

    public override void OnStart(Dictionary<string, object> args)
    {
        Title = "MIDI file playback example";

        DF.Window.BackgroundColor = Color.LightGray;

        DF.Root.Add(
            new TextElement("Music should be playing!", DFFont.GetDefault(30), Color.DimGray)
            {
                Location = new Vector(10, 10)
            });

        audioPlayer.Play(audioStream);
    }

    public override void OnDestroy()
    {
        audioPlayer.Stop();
        audioPlayer.Dispose();
    }

    static void Main()
    {
        DF.Run<MidiExampleScene>();
    }
}
