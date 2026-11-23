// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

#if MELTYSYNTH_AUDIO_AUDIT
namespace MeltySynth
{
    internal sealed class AudioAuditCounters
    {
        internal long Voices, DryMix, Chorus, Reverb;
    }
}
#endif
