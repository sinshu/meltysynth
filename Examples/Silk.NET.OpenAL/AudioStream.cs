using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Silk.NET.OpenAL;

/// <summary>
/// Provides the functionalities for streaming audio.
/// </summary>
public sealed class AudioStream : IDisposable
{
    private static readonly int defaultLatency = 200;
    private static readonly int defaultBlockLength = 2048;

    private AL al;
    private int sampleRate;
    private int channelCount;
    private int latency;
    private int blockLength;

    private uint[] alBuffers;
    private BufferFormat format;

    private uint alSource;

    private short[] blockData;
    private uint[] alBufferQueue;

    private Action<short[]> fillBlock;
    private CancellationTokenSource pollingCts;
    private Task pollingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStream"/> class.
    /// </summary>
    /// <param name="al">The <see cref="AL"/> object for handling OpenAL.</param>
    /// <param name="sampleRate">The sample rate of the audio stream.</param>
    /// <param name="channelCount">The number of channels of the audio stream. This value must be 1 or 2.</param>
    /// <param name="latency">The desired latency for audio processing in milliseconds.</param>
    /// <param name="blockLength">The desired block length for audio processing in sample frames.</param>
    public AudioStream(AL al, int sampleRate, int channelCount, int latency, int blockLength)
    {
        try
        {
            if (al == null)
            {
                throw new ArgumentNullException(nameof(al));
            }

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

            this.al = al;
            this.sampleRate = sampleRate;
            this.channelCount = channelCount;
            this.latency = latency;
            this.blockLength = blockLength;

            var bufferCount = (int)Math.Ceiling((double)(sampleRate * latency) / (1000 * blockLength));
            if (bufferCount < 2)
            {
                bufferCount = 2;
            }

            alBuffers = new uint[bufferCount];
            for (var i = 0; i < alBuffers.Length; i++)
            {
                alBuffers[i] = al.GenBuffer();
                if (al.GetError() != AudioError.NoError)
                {
                    throw new Exception("Failed to generate an audio buffer.");
                }
            }

            format = channelCount == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;

            alSource = al.GenSource();
            if (al.GetError() != AudioError.NoError)
            {
                throw new Exception("Failed to generate an audio source.");
            }

            blockData = new short[channelCount * blockLength];
            alBufferQueue = new uint[1];
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
    /// <param name="al">The <see cref="AL"/> object for handling OpenAL.</param>
    /// <param name="sampleRate">The sample rate of the audio stream.</param>
    /// <param name="channelCount">The number of channels of the audio stream. This value must be 1 or 2.</param>
    public AudioStream(AL al, int sampleRate, int channelCount)
        : this(al, sampleRate, channelCount, defaultLatency, defaultBlockLength)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStream"/> class.
    /// </summary>
    /// <param name="al">The <see cref="AL"/> object for handling OpenAL.</param>
    /// <param name="sampleRate">The sample rate of the audio stream.</param>
    /// <param name="channelCount">The number of channels of the audio stream. This value must be 1 or 2.</param>
    /// <param name="latency">The desired latency for audio processing in milliseconds.</param>
    public AudioStream(AL al, int sampleRate, int channelCount, int latency)
        : this(al, sampleRate, channelCount, latency, defaultBlockLength)
    {
    }

    /// <summary>
    /// Disposes the resources held by the <see cref="AudioStream"/>.
    /// </summary>
    public void Dispose()
    {
        if (al == null)
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
            al.SourceStop(alSource);
            al.DeleteSource(alSource);
            alSource = 0;
        }

        if (alBuffers != null)
        {
            for (var i = 0; i < alBuffers.Length; i++)
            {
                if (alBuffers[i] != 0)
                {
                    al.DeleteBuffer(alBuffers[i]);
                    alBuffers[i] = 0;
                }
            }
        }
    }

    /// <summary>
    /// Plays a sound from the wave data generated by the specified callback function.
    /// </summary>
    /// <param name="fillBlock">The callback function to generate the wave data.</param>
    public void Play(Action<short[]> fillBlock)
    {
        if (al == null)
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
            al.BufferData(alBuffers[i], format, blockData, sampleRate);
            alBufferQueue[0] = alBuffers[i];
            al.SourceQueueBuffers(alSource, alBufferQueue);
        }

        al.SourcePlay(alSource);

        pollingCts = new CancellationTokenSource();
        pollingTask = Task.Run(() => PollingLoop(pollingCts.Token));
    }

    /// <summary>
    /// Stops playing sound.
    /// </summary>
    public void Stop()
    {
        if (al == null)
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
            al.GetSourceProperty(alSource, GetSourceInteger.BuffersProcessed, out processedCount);
            for (var i = 0; i < processedCount; i++)
            {
                fillBlock(blockData);
                al.SourceUnqueueBuffers(alSource, alBufferQueue);
                al.BufferData(alBufferQueue[0], format, blockData, sampleRate);
                al.SourceQueueBuffers(alSource, alBufferQueue);
            }

            int value;
            al.GetSourceProperty(alSource, GetSourceInteger.SourceState, out value);
            if (value == (int)SourceState.Stopped)
            {
                al.SourcePlay(alSource);
            }

            Thread.Sleep(1);
        }

        al.SourceStop(alSource);

        {
            // We have to unqueue remaining buffers for next playback.
            int processedCount;
            al.GetSourceProperty(alSource, GetSourceInteger.BuffersProcessed, out processedCount);
            for (var i = 0; i < processedCount; i++)
            {
                al.SourceUnqueueBuffers(alSource, alBufferQueue);
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
            if (al == null)
            {
                throw new ObjectDisposedException(nameof(AudioStream));
            }

            int value;
            al.GetSourceProperty(alSource, GetSourceInteger.SourceState, out value);

            switch ((SourceState)value)
            {
                case SourceState.Initial:
                case SourceState.Stopped:
                    return PlaybackState.Stopped;

                case SourceState.Playing:
                    return PlaybackState.Playing;

                case SourceState.Paused:
                    return PlaybackState.Paused;

                default:
                    throw new Exception();
            }
        }
    }
}
