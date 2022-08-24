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
        /// <remarks>
        /// This utility method internally uses <see cref="ArrayPool{T}"/>,
        /// which may result in memory allocation on the first call.
        /// To completely avoid memory allocation,
        /// use <see cref="IAudioRenderer.Render(Span{float}, Span{float})"/>.
        /// </remarks>
        public static void RenderInterleaved(this IAudioRenderer renderer, Span<float> destination)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (destination.Length % 2 != 0)
            {
                throw new ArgumentException("The length of the destination buffer must be even.", nameof(destination));
            }

            var sampleCount = destination.Length / 2;
            var bufferLength = destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            try
            {
                var left = buffer.AsSpan(0, sampleCount);
                var right = buffer.AsSpan(sampleCount, sampleCount);
                renderer.Render(left, right);

                var pos = 0;
                for (var t = 0; t < sampleCount; t++)
                {
                    destination[pos++] = left[t];
                    destination[pos++] = right[t];
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Renders the waveform as a monaural signal.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <remarks>
        /// This utility method internally uses <see cref="ArrayPool{T}"/>,
        /// which may result in memory allocation on the first call.
        /// To completely avoid memory allocation,
        /// use <see cref="IAudioRenderer.Render(Span{float}, Span{float})"/>.
        /// </remarks>
        public static void RenderMono(this IAudioRenderer renderer, Span<float> destination)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            var sampleCount = destination.Length;
            var bufferLength = 2 * destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            try
            {
                var left = buffer.AsSpan(0, sampleCount);
                var right = buffer.AsSpan(sampleCount, sampleCount);
                renderer.Render(left, right);

                for (var t = 0; t < sampleCount; t++)
                {
                    destination[t] = (left[t] + right[t]) / 2;
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Renders the waveform with 16-bit quantization.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="left">The buffer of the left channel to store the rendered waveform.</param>
        /// <param name="right">The buffer of the right channel to store the rendered waveform.</param>
        /// <remarks>
        /// Out of range samples will be clipped.
        /// This utility method internally uses <see cref="ArrayPool{T}"/>,
        /// which may result in memory allocation on the first call.
        /// To completely avoid memory allocation,
        /// use <see cref="IAudioRenderer.Render(Span{float}, Span{float})"/>.
        /// The output buffers for the left and right must be the same length.
        /// </remarks>
        public static void RenderInt16(this IAudioRenderer renderer, Span<short> left, Span<short> right)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (left.Length != right.Length)
            {
                throw new ArgumentException("The output buffers for the left and right must be the same length.");
            }

            var sampleCount = left.Length;
            var bufferLength = 2 * left.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            try
            {
                var bufLeft = buffer.AsSpan(0, sampleCount);
                var bufRight = buffer.AsSpan(sampleCount, sampleCount);
                renderer.Render(bufLeft, bufRight);

                for (var t = 0; t < sampleCount; t++)
                {
                    var sample = 32768 * bufLeft[t];
                    if (sample < short.MinValue)
                    {
                        sample = short.MinValue;
                    }
                    else if (sample > short.MaxValue)
                    {
                        sample = short.MaxValue;
                    }

                    left[t] = (short)sample;
                }

                for (var t = 0; t < sampleCount; t++)
                {
                    var sample = 32768 * bufRight[t];
                    if (sample < short.MinValue)
                    {
                        sample = short.MinValue;
                    }
                    else if (sample > short.MaxValue)
                    {
                        sample = short.MaxValue;
                    }

                    right[t] = (short)sample;
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Renders the waveform as a stereo interleaved signal with 16-bit quantization.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <remarks>
        /// Out of range samples will be clipped.
        /// This utility method internally uses <see cref="ArrayPool{T}"/>,
        /// which may result in memory allocation on the first call.
        /// To completely avoid memory allocation,
        /// use <see cref="IAudioRenderer.Render(Span{float}, Span{float})"/>.
        /// </remarks>
        public static void RenderInterleavedInt16(this IAudioRenderer renderer, Span<short> destination)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            if (destination.Length % 2 != 0)
            {
                throw new ArgumentException("The length of the destination buffer must be even.", nameof(destination));
            }

            var sampleCount = destination.Length / 2;
            var bufferLength = destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            try
            {
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
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Renders the waveform as a monaural signal with 16-bit quantization.
        /// </summary>
        /// <param name="renderer">The audio renderer.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <remarks>
        /// Out of range samples will be clipped.
        /// This utility method internally uses <see cref="ArrayPool{T}"/>,
        /// which may result in memory allocation on the first call.
        /// To completely avoid memory allocation,
        /// use <see cref="IAudioRenderer.Render(Span{float}, Span{float})"/>.
        /// </remarks>
        public static void RenderMonoInt16(this IAudioRenderer renderer, Span<short> destination)
        {
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            var sampleCount = destination.Length;
            var bufferLength = 2 * destination.Length;

            var buffer = ArrayPool<float>.Shared.Rent(bufferLength);

            try
            {
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
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }
    }
}
