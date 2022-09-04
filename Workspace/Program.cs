using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using MeltySynth;

public static class Program
{
    public static void Main(string[] args)
    {
        RenderingTest.Flourish();
        RenderingTest.SimpleChord();
    }
}
