// SPDX-License-Identifier: MIT
// Copyright and inherited upstream notices: see LICENSE.txt.
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).

using System;
using System.Collections.Generic;
using System.IO;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public static class TestSettings
    {
        private static readonly string[] Names =
        {
            "TimGM6mb", "Arachno SoundFont - Version 1.0",
            "GeneralUser GS MuseScore v1.442", "SGM-V2.01"
        };
        // Four named immutable external banks at most; never load them during discovery.
        private static readonly Dictionary<string, Lazy<SoundFont>> Banks = CreateBanks();
        private static readonly Lazy<SoundFont> Synthetic = new Lazy<SoundFont>(MidiLoopTestAssets.SoundFont);
        public static SoundFont SyntheticSoundFont => Synthetic.Value;

        private static Dictionary<string, Lazy<SoundFont>> CreateBanks()
        {
            var result = new Dictionary<string, Lazy<SoundFont>>();
            foreach (var name in Names)
                result.Add(name, new Lazy<SoundFont>(() => new SoundFont(TestAssets.RequireExternal(name + ".sf2"))));
            return result;
        }

        public static SoundFont LoadExternalSoundFont(string name) => Banks[name].Value;
        public static IEnumerable<TestCaseData> SoundFonts => Cases(Names);
        public static IEnumerable<TestCaseData> LightSoundFonts => Cases(new[] { Names[0], Names[2] });
        private static IEnumerable<TestCaseData> Cases(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                var test = new TestCaseData(name).SetCategory("ExternalAssets");
                if (!File.Exists(TestAssets.ExternalPath(name + ".sf2")))
                    test.Ignore("Unavailable external SoundFont: " + name + ".sf2; configure MELTYSYNTH_TEST_ASSETS.");
                yield return test;
            }
        }
    }
}
