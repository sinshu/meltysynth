using System;
using System.Collections.Generic;
using System.IO;
using System.Buffers;

namespace MeltySynth
{
    internal static class MathF
    {
        public const float PI = (float)Math.PI;
        public static float Abs(float value) => Math.Abs(value);
        public static float Cos(float d) => (float)Math.Cos(d);
        public static float Log10(double d) => (float)Math.Log10(d);
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);
        public static float Sin(float a) => (float)Math.Sin(a);
        public static float Sqrt(float d) => (float)Math.Sqrt(d);
    }

    internal static class MissingMethods
    {
        public static int Read(this BinaryReader reader, Span<byte> buffer)
        {
            var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
            var read = reader.Read(array, 0, buffer.Length);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = array[i];
            }
            ArrayPool<byte>.Shared.Return(array);
            return read;
        }

        public static void TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
        }
    }
}
