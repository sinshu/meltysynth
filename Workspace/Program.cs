using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;
using MeltySynth.SoundFont;

public static class Program
{
    public static void Main(string[] args)
    {
        var sf = new SoundFont("TimGM6mb.sf2");
        Console.WriteLine("OK");
    }
}
