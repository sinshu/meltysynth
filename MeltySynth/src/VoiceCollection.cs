using System;
using System.Collections;
using System.Collections.Generic;

namespace MeltySynth
{
    internal sealed class VoiceCollection
    {
        private readonly Synthesizer synthesizer;

        private readonly Voice[] voices;

        private int activeVoiceCount;

        internal VoiceCollection(Synthesizer synthesizer, int maxActiveVoiceCount)
        {
            this.synthesizer = synthesizer;

            voices = new Voice[maxActiveVoiceCount];
            for (var i = 0; i < voices.Length; i++)
            {
                voices[i] = new Voice(synthesizer);
            }

            activeVoiceCount = 0;
        }

        public Voice RequestNew(InstrumentRegion region, int channel)
        {
            // If the voice is an exclusive class, reuse the existing one so that only one note
            // will be played at a time.
            var exclusiveClass = region.ExclusiveClass;
            if (exclusiveClass != 0)
            {
                for (var i = 0; i < activeVoiceCount; i++)
                {
                    var voice = voices[i];
                    if (voice.ExclusiveClass == exclusiveClass && voice.Channel == channel)
                    {
                        return voice;
                    }
                }
            }

            // If active voices are less than the limit, a new one can be used without problem.
            if (activeVoiceCount < voices.Length)
            {
                var free = voices[activeVoiceCount];
                activeVoiceCount++;
                return free;
            }

            // Too many active voices...
            // Find one which has the lowest priority.
            Voice low = null;
            var lowestPriority = float.MaxValue;
            for (var i = 0; i < activeVoiceCount; i++)
            {
                var voice = voices[i];
                var priority = voice.Priority;
                if (priority < lowestPriority)
                {
                    lowestPriority = priority;
                    low = voice;
                }
                else if (priority == lowestPriority)
                {
                    // Same priority...
                    // The older one should be more suitable for reuse.
                    if (voice.VoiceLength > low.VoiceLength)
                    {
                        low = voice;
                    }
                }
            }
            return low;
        }

        public void Process()
        {
            var i = 0;

            while (true)
            {
                if (i == activeVoiceCount)
                {
                    return;
                }

                if (voices[i].Process())
                {
                    i++;
                }
                else
                {
                    activeVoiceCount--;

                    var tmp = voices[i];
                    voices[i] = voices[activeVoiceCount];
                    voices[activeVoiceCount] = tmp;
                }
            }
        }

        public void Clear()
        {
            activeVoiceCount = 0;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int ActiveVoiceCount => activeVoiceCount;



        public struct Enumerator : IEnumerator<Voice>
        {
            private VoiceCollection collection;

            private int index;
            private Voice current;

            internal Enumerator(VoiceCollection collection)
            {
                this.collection = collection;

                index = 0;
                current = null;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (index < collection.activeVoiceCount)
                {
                    current = collection.voices[index];
                    index++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                index = 0;
                current = null;
            }

            public Voice Current => current;

            object IEnumerator.Current => throw new NotSupportedException();
        }
    }
}
