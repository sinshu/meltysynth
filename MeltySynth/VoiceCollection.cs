using System;
using System.Collections;
using System.Collections.Generic;

namespace MeltySynth
{
    internal sealed class VoiceCollection
    {
        private Synthesizer synthesizer;

        private Voice[] voices;

        private int activeVoiceCount;

        internal VoiceCollection(Synthesizer synthesizer, int maxActiveVoiceCount)
        {
            this.synthesizer = synthesizer;

            voices = new Voice[maxActiveVoiceCount];
            for (var i = 0; i < voices.Length; i++)
            {
                voices[i] = new Voice(synthesizer);
            }
        }

        public Voice RequestNew(InstrumentRegion region, int channel, int key)
        {
            Voice free = null;
            Voice low = null;
            var lowestPriority = float.MaxValue;

            var exclusiveClass = region.ExclusiveClass;
            if (exclusiveClass == 0)
            {
                for (var i = 0; i < activeVoiceCount; i++)
                {
                    var voice = voices[i];
                    if (voice.Region == region && voice.Channel == channel && voice.Key == key)
                    {
                        voice.Kill();
                        free = voice;
                    }
                    if (voice.Priority < lowestPriority)
                    {
                        lowestPriority = voice.Priority;
                        low = voice;
                    }
                }
            }
            else
            {
                for (var i = 0; i < activeVoiceCount; i++)
                {
                    var voice = voices[i];
                    if (voice.ExclusiveClass == exclusiveClass && voice.Channel == channel)
                    {
                        voice.Kill();
                        free = voice;
                    }
                    if (voice.Priority < lowestPriority)
                    {
                        lowestPriority = voice.Priority;
                        low = voice;
                    }
                }
            }

            if (free != null)
            {
                return free;
            }

            if (activeVoiceCount < voices.Length)
            {
                free = voices[activeVoiceCount];
                activeVoiceCount++;
                return free;
            }
            else
            {
                return low;
            }
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
            }

            public Voice Current => current;

            object IEnumerator.Current => throw new NotSupportedException();
        }
    }
}
