using System;

namespace MeltySynth
{
    /// <summary>
    /// Defines a common interface for audio rendering.
    /// </summary>
    public interface IAudioRenderer
    {
        /// <summary>
        /// Renders the waveform.
        /// </summary>
        /// <param name="left">The buffer of the left channel to store the rendered waveform.</param>
        /// <param name="right">The buffer of the right channel to store the rendered waveform.</param>
        /// <remarks>
        /// The buffers must be the same length.
        /// </remarks>
        void Render(Span<float> left, Span<float> right);
    }
}
