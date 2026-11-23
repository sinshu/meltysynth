// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;
using NUnit.Framework;

namespace MeltySynthTest
{
    internal static class TestAssets
    {
        internal static string ExternalRoot => Path.GetFullPath(
            Environment.GetEnvironmentVariable("MELTYSYNTH_TEST_ASSETS") ?? Path.Combine(AppContext.BaseDirectory, "ExternalAssets"));
        internal static string ReferenceRoot => Path.GetFullPath(
            Environment.GetEnvironmentVariable("MELTYSYNTH_REFERENCE_DATA") ?? Path.Combine(AppContext.BaseDirectory, "ReferenceData"));

        internal static string ExternalPath(string name) => Path.Combine(ExternalRoot, name);
        internal static string RequireExternal(string name)
        {
            var path = ExternalPath(name);
            if (!File.Exists(path)) Assert.Ignore("Unavailable external fixture: " + path + ". Set MELTYSYNTH_TEST_ASSETS to permitted assets.");
            return path;
        }
        internal static string Reference(params string[] parts) => Path.Combine(ReferenceRoot, Path.Combine(parts));
    }
}
