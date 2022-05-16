using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

/// <summary>
/// Provides the functionalities for streaming audio.
/// </summary>
public sealed class AudioStream : IDisposable
{
    private static readonly int defaultLatency = 200;
    private static readonly int defaultBlockLength = 2048;

    private bool disposed = false;

    private int sampleRate;
    private int channelCount;
    private int latency;
    private int blockLength;

    private int[] alBuffers;
    private ALFormat format;

    private int alSource;

    private short[] blockData;
    private int[] alBufferQueue;

    private Action<short[]> fillBlock;
    private CancellationTokenSource pollingCts;
    private Task pollingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStream"/> class.
    /// </summary>
    /// <param name="sampleRate">The sample rate of the audio stream.</param>
    /// <param name="channelCount">The number of channels of the audio stream. This value must be 1 or 2.</param>
    /// <param name="latency">The desired latency for audio processing in milliseconds.</param>
    /// <param name="blockLength">The desired block length for audio processing in sample frames.</param>
    public AudioStream(int sampleRate, int channelCount, int latency, int blockLength)
    {
        try
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentException("The sample rate must be a positive value.", nameof(sampleRate));
            }

            if (channelCount != 1 && channelCount != 2)
            {
                throw new ArgumentException("The number of channels must be 1 or 2.", nameof(channelCount));
            }

            if (latency <= 0)
            {
                throw new ArgumentException("The latancy must be a positive value.", nameof(latency));
            }

            if (blockLength < 8)
            {
                throw new ArgumentException("The block length must be greater than or equal to 8.", nameof(blockLength));
            }

            this.sampleRate = sampleRate;
            this.channelCount = channelCount;
            this.latency = latency;
            this.blockLength = blockLength;

            var bufferCount = (int)Math.Ceiling((double)(sampleRate * latency) / (1000 * blockLength));
            if (bufferCount < 2)
            {
                bufferCount = 2;
            }

            alBuffers = new int[bufferCount];
            for (var i = 0; i < alBuffers.Length; i++)
            {
                alBuffers[i] = AL.GenBuffer();
                if (AL.GetError() != ALError.NoError)
                {
                    throw new Exception("Failed to generate an audio buffer.");
                }
            }

            format = channelCount == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

            alSource = AL.GenSource();
            if (AL.GetError() != ALError.NoError)
            {
                throw new Exception("Failed to generate an audio source.");
            }

            blockData = new short[channelCount * blockLength];
            alBufferQueue = new int[1];
        }
        catch (Exception e)
        {
            Dispose();
            ExceptionDispatchInfo.Throw(e);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStream"/> class.
    /// </summary>
    /// <param name="sampleRate">The sample rate of the audio stream.</param>
    /// <param name="channelCount">The number of channels of the audio stream. This value must be 1 or 2.</param>
    public AudioStream(int sampleRate, int channelCount)
        : this(sampleRate, channelCount, defaultLatency, defaultBlockLength)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStream"/> class.
    /// </summary>
    /// <param name="sampleRate">The sample rate of the audio stream.</param>
    /// <param name="channelCount">The number of channels of the audio stream. This value must be 1 or 2.</param>
    /// <param name="latency">The desired latency for audio processing in milliseconds.</param>
    public AudioStream(int sampleRate, int channelCount, int latency)
        : this(sampleRate, channelCount, latency, defaultBlockLength)
    {
    }

    /// <summary>
    /// Disposes the resources held by the <see cref="AudioStream"/>.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (pollingTask != null)
        {
            pollingCts.Cancel();
            pollingTask.Wait();
            pollingCts = null;
            pollingTask = null;
        }

        if (alSource != 0)
        {
            AL.SourceStop(alSource);
            AL.DeleteSource(alSource);
            alSource = 0;
        }

        if (alBuffers != null)
        {
            for (var i = 0; i < alBuffers.Length; i++)
            {
                if (alBuffers[i] != 0)
                {
                    AL.DeleteBuffer(alBuffers[i]);
                    alBuffers[i] = 0;
                }
            }
        }

        disposed = true;
    }

    /// <summary>
    /// Plays a sound from the wave data generated by the specified callback function.
    /// </summary>
    /// <param name="fillBlock">The callback function to generate the wave data.</param>
    public void Play(Action<short[]> fillBlock)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(AudioStream));
        }

        if (fillBlock == null)
        {
            throw new ArgumentNullException(nameof(fillBlock));
        }

        // If the previous playback is still ongoing, we have to stop it.
        if (pollingTask != null)
        {
            pollingCts.Cancel();
            pollingTask.Wait();
        }

        this.fillBlock = fillBlock;

        for (var i = 0; i < alBuffers.Length; i++)
        {
            fillBlock(blockData);
            AL.BufferData(alBuffers[i], format, blockData, sampleRate);
            alBufferQueue[0] = alBuffers[i];
            AL.SourceQueueBuffers(alSource, alBufferQueue);
        }

        AL.SourcePlay(alSource);

        pollingCts = new CancellationTokenSource();
        pollingTask = Task.Run(() => PollingLoop(pollingCts.Token));
    }

    /// <summary>
    /// Stops playing sound.
    /// </summary>
    public void Stop()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(AudioStream));
        }

        if (pollingTask != null)
        {
            pollingCts.Cancel();
        }
    }

    private void PollingLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            int processedCount;
            AL.GetSource(alSource, ALGetSourcei.BuffersProcessed, out processedCount);
            for (var i = 0; i < processedCount; i++)
            {
                fillBlock(blockData);
                AL.SourceUnqueueBuffers(alSource, alBufferQueue);
                AL.BufferData(alBufferQueue[0], format, blockData, sampleRate);
                AL.SourceQueueBuffers(alSource, alBufferQueue);
            }

            int value;
            AL.GetSource(alSource, ALGetSourcei.SourceState, out value);
            if (value == (int)ALSourceState.Stopped)
            {
                AL.SourcePlay(alSource);
            }

            Thread.Sleep(1);
        }

        AL.SourceStop(alSource);

        {
            // We have to unqueue remaining buffers for next playback.
            int processedCount;
            AL.GetSource(alSource, ALGetSourcei.BuffersProcessed, out processedCount);
            for (var i = 0; i < processedCount; i++)
            {
                AL.SourceUnqueueBuffers(alSource, alBufferQueue);
            }
        }
    }

    /// <summary>
    /// Gets the sample rate of the audio stream.
    /// </summary>
    public int SampleRate => sampleRate;

    /// <summary>
    /// Gets the number of channels of the audio stream.
    /// </summary>
    public int ChannelCount => channelCount;

    /// <summary>
    /// Gets the latency for audio processing in milliseconds.
    /// </summary>
    public int Latency => latency;

    /// <summary>
    /// Gets the block length for audio processing in sample frames.
    /// </summary>
    public int BlockLength => blockLength;

    /// <summary>
    /// Gets the current playback state of the channel.
    /// </summary>
    public PlaybackState State
    {
        get
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AudioStream));
            }

            int value;
            AL.GetSource(alSource, ALGetSourcei.SourceState, out value);

            switch ((ALSourceState)value)
            {
                case ALSourceState.Initial:
                case ALSourceState.Stopped:
                    return PlaybackState.Stopped;

                case ALSourceState.Playing:
                    return PlaybackState.Playing;

                case ALSourceState.Paused:
                    return PlaybackState.Paused;

                default:
                    throw new Exception();
            }
        }
    }
}
