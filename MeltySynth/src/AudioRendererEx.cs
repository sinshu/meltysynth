using System;
using System.Buffers;

namespace MeltySynth
{
    /// <summary>
    /// Provides utility methods to convert the format of samples.
    /// </summary>
    public static class AudioRendererEx
    {
        /// <summary>
        /// Renders the waveform as a stereo interleaved signal.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="destination">The destination buffer.</param>
        public static void RenderInterleaved(this IAudioRenderer renderer, Span<float> destination)
        {
            if (destination.Length % 2 != 0)
            {
                throw new ArgumentException("The length of the destination buffer must be even.", nameof(destination));
            }

            var sampleCount = destination.Length / 2;
            var bufferLength = destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            var left = buffer.AsSpan(0, sampleCount);
            var right = buffer.AsSpan(sampleCount, sampleCount);
            renderer.Render(left, right);

            var pos = 0;
            for (var t = 0; t < sampleCount; t++)
            {
                destination[pos++] = left[t];
                destination[pos++] = right[t];
            }

            ArrayPool<float>.Shared.Return(buffer);
        }

        /// <summary>
        /// Renders the waveform as a monaural signal.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="destination">The destination buffer.</param>
        public static void RenderMono(this IAudioRenderer renderer, Span<float> destination)
        {
            var sampleCount = destination.Length;
            var bufferLength = 2 * destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            var left = buffer.AsSpan(0, sampleCount);
            var right = buffer.AsSpan(sampleCount, sampleCount);
            renderer.Render(left, right);

            for (var t = 0; t < sampleCount; t++)
            {
                destination[t] = (left[t] + right[t]) / 2;
            }

            ArrayPool<float>.Shared.Return(buffer);
        }

        /// <summary>
        /// Renders the waveform as a stereo interleaved signal with 16-bit quantization.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="destination">The destination buffer.</param>
        public static void RenderInterleavedInt16(this IAudioRenderer renderer, Span<short> destination)
        {
            if (destination.Length % 2 != 0)
            {
                throw new ArgumentException("The length of the destination buffer must be even.", nameof(destination));
            }

            var sampleCount = destination.Length / 2;
            var bufferLength = destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            var left = buffer.AsSpan(0, sampleCount);
            var right = buffer.AsSpan(sampleCount, sampleCount);
            renderer.Render(left, right);

            var pos = 0;
            for (var t = 0; t < sampleCount; t++)
            {
                var sampleLeft = (int)(32768 * left[t]);
                if (sampleLeft < short.MinValue)
                {
                    sampleLeft = short.MinValue;
                }
                else if (sampleLeft > short.MaxValue)
                {
                    sampleLeft = short.MaxValue;
                }

                var sampleRight = (int)(32768 * right[t]);
                if (sampleRight < short.MinValue)
                {
                    sampleRight = short.MinValue;
                }
                else if (sampleRight > short.MaxValue)
                {
                    sampleRight = short.MaxValue;
                }

                destination[pos++] = (short)sampleLeft;
                destination[pos++] = (short)sampleRight;
            }

            ArrayPool<float>.Shared.Return(buffer);
        }

        /// <summary>
        /// Renders the waveform as a monaural signal with 16-bit quantization.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="destination">The destination buffer.</param>
        public static void RenderMonoInt16(this IAudioRenderer renderer, Span<short> destination)
        {
            var sampleCount = destination.Length;
            var bufferLength = 2 * destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            var left = buffer.AsSpan(0, sampleCount);
            var right = buffer.AsSpan(sampleCount, sampleCount);
            renderer.Render(left, right);

            for (var t = 0; t < sampleCount; t++)
            {
                var sample = (int)(16384 * (left[t] + right[t]));
                if (sample < short.MinValue)
                {
                    sample = short.MinValue;
                }
                else if (sample > short.MaxValue)
                {
                    sample = short.MaxValue;
                }

                destination[t] = (short)sample;
            }

            ArrayPool<float>.Shared.Return(buffer);
        }
    }
}
