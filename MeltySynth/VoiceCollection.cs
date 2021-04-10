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

        public Voice GetFreeVoice()
        {
            Voice freeVoice;
            if (activeVoiceCount < voices.Length)
            {
                freeVoice = voices[activeVoiceCount];
                activeVoiceCount++;
            }
            else
            {
                freeVoice = null;
            }

            return freeVoice;
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
