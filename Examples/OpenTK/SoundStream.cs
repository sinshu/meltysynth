using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

/// <summary>
/// Provides a sound stream for playing a long audio signal as a sequence of short blocks.
/// </summary>
public class SoundStream : IDisposable
{
    // This value indicates the desired latency of audio processing in milliseconds.
    // Reducing the latency will improve responsiveness, but stability will be lost.
    private static readonly int latency = 200;

    private volatile bool disposed = false;

    private readonly int sampleRate;
    private readonly int channelCount;
    private readonly int blockLength;
    private readonly Action<short[]> fillBlock;

    private int source;
    private int[] buffers;
    private ALFormat format;

    private short[] bufferData;
    private int[] bufferQueue;

    private Task pollingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoundStream"/> class.
    /// </summary>
    /// <param name="sampleRate">The sample rate for the audio processing.</param>
    /// <param name="channelCount">The number of channels.</param>
    /// <param name="blockLength">The desired block length.</param>
    /// <param name="fillBlock">The callback function to fill a block.</param>
    public SoundStream(int sampleRate, int channelCount, int blockLength, Action<short[]> fillBlock)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "The sample rate must be a positive value.");
        }

        if (!(channelCount == 1 || channelCount == 2))
        {
            throw new ArgumentOutOfRangeException(nameof(channelCount), "The number of channels must be 1 or 2.");
        }

        if (blockLength < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(blockLength), "The block length must be greater than or equal to 8.");
        }

        if (fillBlock == null)
        {
            throw new ArgumentNullException(nameof(fillBlock));
        }

        this.sampleRate = sampleRate;
        this.channelCount = channelCount;
        this.blockLength = blockLength;
        this.fillBlock = fillBlock;

        try
        {
            Initialize();
        }
        catch (Exception e)
        {
            Dispose();
            ExceptionDispatchInfo.Throw(e);
        }
    }

    private void Initialize()
    {
        var bufferCount = (int)Math.Ceiling((double)(sampleRate * latency) / (1000 * blockLength));
        if (bufferCount < 2)
        {
            bufferCount = 2;
        }

        source = AL.GenSource();
        if (AL.GetError() != ALError.NoError)
        {
            throw new Exception("Failed to generate a sound source.");
        }

        buffers = new int[bufferCount];
        for (var i = 0; i < buffers.Length; i++)
        {
            buffers[i] = AL.GenBuffer();
            if (AL.GetError() != ALError.NoError)
            {
                throw new Exception("Failed to generate a sound buffer.");
            }
        }

        switch (channelCount)
        {
            case 1:
                format = ALFormat.Mono16;
                break;
            case 2:
                format = ALFormat.Stereo16;
                break;
            default:
                throw new Exception("The number of channels must be 1 or 2.");
        }

        bufferData = new short[channelCount * blockLength];
        bufferQueue = new int[1];
    }

    /// <summary>
    /// Starts playing the sound stream.
    /// </summary>
    public void Start()
    {
        if (pollingTask != null)
        {
            return;
        }

        for (var i = 0; i < buffers.Length; i++)
        {
            fillBlock(bufferData);
            AL.BufferData(buffers[i], format, bufferData, sampleRate);
            bufferQueue[0] = buffers[i];
            AL.SourceQueueBuffers(source, bufferQueue);
        }

        AL.SourcePlay(source);

        pollingTask = Task.Run(ProcessBuffers);
    }

    private void ProcessBuffers()
    {
        while (!disposed)
        {
            int processedCount;
            AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processedCount);

            for (var i = 0; i < processedCount; i++)
            {
                fillBlock(bufferData);
                AL.SourceUnqueueBuffers(source, bufferQueue);
                AL.BufferData(bufferQueue[0], format, bufferData, sampleRate);
                AL.SourceQueueBuffers(source, bufferQueue);
            }

            int state;
            AL.GetSource(source, ALGetSourcei.SourceState, out state);

            if (state == (int)ALSourceState.Stopped)
            {
                AL.SourcePlay(source);
            }

            Thread.Sleep(1);
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (disposing)
        {
            if (pollingTask != null)
            {
                pollingTask.Wait();
            }
        }

        if (source != 0)
        {
            AL.SourceStop(source);
            AL.DeleteSource(source);
        }

        if (buffers != null)
        {
            for (var i = 0; i < buffers.Length; i++)
            {
                if (buffers[i] != 0)
                {
                    AL.DeleteBuffer(buffers[i]);
                }
            }
        }
    }

    /// <summary>
    /// Stops the sound stream and disposes internal resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SoundStream()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets a value that indicates whether the sound is playing.
    /// </summary>
    public bool IsPlaying => pollingTask != null;
}
