// This reverb implementation is based on Freeverb, a public domain reverb
// implementation by Jezar at Dreampoint.

using System;
using System.Numerics;

namespace MeltySynth
{
    internal sealed class Reverb
    {
        private const float fixedGain = 0.015F;
        private const float scaleWet = 3F;
        private const float scaleDamp = 0.4F;
        private const float scaleRoom = 0.28F;
        private const float offsetRoom = 0.7F;
        private const float initialRoom = 0.5F;
        private const float initialDamp = 0.5F;
        private const float initialWet = 1F / scaleWet;
        private const float initialWidth = 1F;
        private const int stereoSpread = 23;

        private const int cfTuningL1 = 1116;
        private const int cfTuningR1 = 1116 + stereoSpread;
        private const int cfTuningL2 = 1188;
        private const int cfTuningR2 = 1188 + stereoSpread;
        private const int cfTuningL3 = 1277;
        private const int cfTuningR3 = 1277 + stereoSpread;
        private const int cfTuningL4 = 1356;
        private const int cfTuningR4 = 1356 + stereoSpread;
        private const int cfTuningL5 = 1422;
        private const int cfTuningR5 = 1422 + stereoSpread;
        private const int cfTuningL6 = 1491;
        private const int cfTuningR6 = 1491 + stereoSpread;
        private const int cfTuningL7 = 1557;
        private const int cfTuningR7 = 1557 + stereoSpread;
        private const int cfTuningL8 = 1617;
        private const int cfTuningR8 = 1617 + stereoSpread;
        private const int apfTuningL1 = 556;
        private const int apfTuningR1 = 556 + stereoSpread;
        private const int apfTuningL2 = 441;
        private const int apfTuningR2 = 441 + stereoSpread;
        private const int apfTuningL3 = 341;
        private const int apfTuningR3 = 341 + stereoSpread;
        private const int apfTuningL4 = 225;
        private const int apfTuningR4 = 225 + stereoSpread;

        private readonly CombFilter4[] cfsL;
        private readonly CombFilter4[] cfsR;
        private readonly AllPassFilter4 apfsL;
        private readonly AllPassFilter4 apfsR;

        private float gain;
        private float roomSize, roomSize1;
        private float damp, damp1;
        private float wet, wet1, wet2;
        private float width;

        internal Reverb(int sampleRate)
        {
            cfsL = new CombFilter4[]
            {
                new CombFilter4(
                    ScaleTuning(sampleRate, cfTuningL1),
                    ScaleTuning(sampleRate, cfTuningL2),
                    ScaleTuning(sampleRate, cfTuningL3),
                    ScaleTuning(sampleRate, cfTuningL4)),
                new CombFilter4(
                    ScaleTuning(sampleRate, cfTuningL5),
                    ScaleTuning(sampleRate, cfTuningL6),
                    ScaleTuning(sampleRate, cfTuningL7),
                    ScaleTuning(sampleRate, cfTuningL8))
            };

            cfsR = new CombFilter4[]
            {
                new CombFilter4(
                    ScaleTuning(sampleRate, cfTuningR1),
                    ScaleTuning(sampleRate, cfTuningR2),
                    ScaleTuning(sampleRate, cfTuningR3),
                    ScaleTuning(sampleRate, cfTuningR4)),
                new CombFilter4(
                    ScaleTuning(sampleRate, cfTuningR5),
                    ScaleTuning(sampleRate, cfTuningR6),
                    ScaleTuning(sampleRate, cfTuningR7),
                    ScaleTuning(sampleRate, cfTuningR8))
            };

            apfsL = new AllPassFilter4(
                ScaleTuning(sampleRate, apfTuningL1),
                ScaleTuning(sampleRate, apfTuningL2),
                ScaleTuning(sampleRate, apfTuningL3),
                ScaleTuning(sampleRate, apfTuningL4));

            apfsR = new AllPassFilter4(
                ScaleTuning(sampleRate, apfTuningR1),
                ScaleTuning(sampleRate, apfTuningR2),
                ScaleTuning(sampleRate, apfTuningR3),
                ScaleTuning(sampleRate, apfTuningR4));

            apfsL.Feedback = 0.5F;
            apfsR.Feedback = 0.5F;

            Wet = initialWet;
            RoomSize = initialRoom;
            Damp = initialDamp;
            Width = initialWidth;
        }

        private int ScaleTuning(int sampleRate, int tuning)
        {
            return (int)Math.Round((double)sampleRate / 44100 * tuning);
        }

        public void Process(float[] input, float[] outputLeft, float[] outputRight)
        {
            Array.Clear(outputLeft, 0, outputLeft.Length);
            Array.Clear(outputRight, 0, outputRight.Length);

            foreach (var cf in cfsL)
            {
                cf.Process(input, outputLeft);
            }

            apfsL.Process(outputLeft);

            foreach (var cf in cfsR)
            {
                cf.Process(input, outputRight);
            }

            apfsR.Process(outputRight);

            // With the default settings, we can skip this part.
            if (1F - wet1 > 1.0E-3 || wet2 > 1.0E-3)
            {
                for (var t = 0; t < input.Length; t++)
                {
                    var left = outputLeft[t];
                    var right = outputRight[t];
                    outputLeft[t] = left * wet1 + right * wet2;
                    outputRight[t] = right * wet1 + left * wet2;
                }
            }
        }

        public void Mute()
        {
            foreach (var cf in cfsL)
            {
                cf.Mute();
            }

            foreach (var cf in cfsR)
            {
                cf.Mute();
            }

            apfsL.Mute();
            apfsR.Mute();
        }

        private void Update()
        {
            wet1 = wet * (width / 2F + 0.5F);
            wet2 = wet * ((1F - width) / 2F);

            roomSize1 = roomSize;
            damp1 = damp;
            gain = fixedGain;

            foreach (var cf in cfsL)
            {
                cf.Feedback = roomSize1;
                cf.Damp = damp1;
            }

            foreach (var cf in cfsR)
            {
                cf.Feedback = roomSize1;
                cf.Damp = damp1;
            }
        }

        public float InputGain => gain;

        public float RoomSize
        {
            get
            {
                return (roomSize - offsetRoom) / scaleRoom;
            }

            set
            {
                roomSize = (value * scaleRoom) + offsetRoom;
                Update();
            }
        }

        public float Damp
        {
            get
            {
                return damp / scaleDamp;
            }

            set
            {
                damp = value * scaleDamp;
                Update();
            }
        }

        public float Wet
        {
            get
            {
                return wet / scaleWet;
            }

            set
            {
                wet = value * scaleWet;
                Update();
            }
        }

        public float Width
        {
            get
            {
                return width;
            }

            set
            {
                width = value;
                Update();
            }
        }



        internal sealed class CombFilter4
        {
            private readonly float[] buffer1;
            private readonly float[] buffer2;
            private readonly float[] buffer3;
            private readonly float[] buffer4;

            private int bufferIndex1;
            private int bufferIndex2;
            private int bufferIndex3;
            private int bufferIndex4;
            private Vector4 filterStore;

            private float feedback;
            private float damp1;
            private float damp2;

            public CombFilter4(int bufferSize1, int bufferSize2, int bufferSize3, int bufferSize4)
            {
                buffer1 = new float[bufferSize1];
                buffer2 = new float[bufferSize2];
                buffer3 = new float[bufferSize3];
                buffer4 = new float[bufferSize4];

                bufferIndex1 = 0;
                bufferIndex2 = 0;
                bufferIndex3 = 0;
                bufferIndex4 = 0;
                filterStore = Vector4.Zero;

                feedback = 0F;
                damp1 = 0F;
                damp2 = 0F;
            }

            public void Mute()
            {
                Array.Clear(buffer1, 0, buffer1.Length);
                Array.Clear(buffer2, 0, buffer2.Length);
                Array.Clear(buffer3, 0, buffer3.Length);
                Array.Clear(buffer4, 0, buffer4.Length);
                filterStore = Vector4.Zero;
            }

            public void Process(float[] inputBlock, float[] outputBlock)
            {
                for (var t = 0; t < outputBlock.Length; t++)
                {
                    var input = new Vector4(inputBlock[t]);

                    var output = new Vector4(
                        buffer1[bufferIndex1],
                        buffer2[bufferIndex2],
                        buffer3[bufferIndex3],
                        buffer4[bufferIndex4]);

                    var absOutput = Vector4.Abs(output);
                    if (absOutput.X < 1.0E-6F) output.X = 0;
                    if (absOutput.Y < 1.0E-6F) output.Y = 0;
                    if (absOutput.Z < 1.0E-6F) output.Z = 0;
                    if (absOutput.W < 1.0E-6F) output.W = 0;

                    filterStore = (output * damp2) + (filterStore * damp1);

                    var absFilterStore = Vector4.Abs(filterStore);
                    if (absFilterStore.X < 1.0E-6F) filterStore.X = 0;
                    if (absFilterStore.Y < 1.0E-6F) filterStore.Y = 0;
                    if (absFilterStore.Z < 1.0E-6F) filterStore.Z = 0;
                    if (absFilterStore.W < 1.0E-6F) filterStore.W = 0;

                    var bufout = input + (filterStore * feedback);
                    buffer1[bufferIndex1] = bufout.X;
                    buffer2[bufferIndex2] = bufout.Y;
                    buffer3[bufferIndex3] = bufout.Z;
                    buffer4[bufferIndex4] = bufout.W;

                    if (++bufferIndex1 >= buffer1.Length) bufferIndex1 = 0;
                    if (++bufferIndex2 >= buffer2.Length) bufferIndex2 = 0;
                    if (++bufferIndex3 >= buffer3.Length) bufferIndex3 = 0;
                    if (++bufferIndex4 >= buffer4.Length) bufferIndex4 = 0;

                    outputBlock[t] += output.X + output.Y + output.Z + output.W;
                }
            }

            public float Feedback
            {
                get => feedback;
                set => feedback = value;
            }

            public float Damp
            {
                get => damp1;

                set
                {
                    damp1 = value;
                    damp2 = 1F - value;
                }
            }
        }



        internal sealed class AllPassFilter4
        {
            private readonly float[] buffer1;
            private readonly float[] buffer2;
            private readonly float[] buffer3;
            private readonly float[] buffer4;

            private int bufferIndex1;
            private int bufferIndex2;
            private int bufferIndex3;
            private int bufferIndex4;

            private float feedback;

            public AllPassFilter4(int bufferSize1, int bufferSize2, int bufferSize3, int bufferSize4)
            {
                buffer1 = new float[bufferSize1];
                buffer2 = new float[bufferSize2];
                buffer3 = new float[bufferSize3];
                buffer4 = new float[bufferSize4];

                bufferIndex1 = 0;
                bufferIndex2 = 0;
                bufferIndex3 = 0;
                bufferIndex4 = 0;

                feedback = 0F;
            }

            public void Mute()
            {
                Array.Clear(buffer1, 0, buffer1.Length);
                Array.Clear(buffer2, 0, buffer2.Length);
                Array.Clear(buffer3, 0, buffer3.Length);
                Array.Clear(buffer4, 0, buffer4.Length);
            }

            public void Process(float[] block)
            {
                for (var t = 0; t < block.Length; t++)
                {
                    float input1 = block[t];
                    float output1;
                    {
                        var bufout = buffer1[bufferIndex1];
                        if (MathF.Abs(bufout) < 1.0E-6) bufout = 0;

                        output1 = -input1 + bufout;
                        buffer1[bufferIndex1] = input1 + (bufout * feedback);

                        if (++bufferIndex1 >= buffer1.Length) bufferIndex1 = 0;
                    }

                    float input2 = output1;
                    float output2;
                    {
                        var bufout = buffer2[bufferIndex2];
                        if (MathF.Abs(bufout) < 1.0E-6) bufout = 0;

                        output2 = -input2 + bufout;
                        buffer2[bufferIndex2] = input2 + (bufout * feedback);

                        if (++bufferIndex2 >= buffer2.Length) bufferIndex2 = 0;
                    }

                    float input3 = output2;
                    float output3;
                    {
                        var bufout = buffer3[bufferIndex3];
                        if (MathF.Abs(bufout) < 1.0E-6) bufout = 0;

                        output3 = -input3 + bufout;
                        buffer3[bufferIndex3] = input3 + (bufout * feedback);

                        if (++bufferIndex3 >= buffer3.Length) bufferIndex3 = 0;
                    }

                    float input4 = output3;
                    float output4;
                    {
                        var bufout = buffer4[bufferIndex4];
                        if (MathF.Abs(bufout) < 1.0E-6) bufout = 0;

                        output4 = -input4 + bufout;
                        buffer4[bufferIndex4] = input4 + (bufout * feedback);

                        if (++bufferIndex4 >= buffer4.Length) bufferIndex4 = 0;
                    }

                    block[t] = output4;
                }
            }

            public float Feedback
            {
                get => feedback;
                set => feedback = value;
            }
        }
    }
}
