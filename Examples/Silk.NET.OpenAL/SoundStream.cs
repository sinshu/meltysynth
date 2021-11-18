using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Silk.NET.OpenAL;

/// <summary>
/// Provides a sound stream for playing a long audio signal as a sequence of short blocks.
/// </summary>
public class SoundStream : IDisposable
{
    // This value indicates the desired latency of audio processing in milliseconds.
    // Reducing the latency will improve responsiveness, but stability will be lost.
    private static readonly int latency = 200;

    private bool disposed = false;

    private readonly AL al;
    private readonly int sampleRate;
    private readonly int channelCount;
    private readonly int blockLength;
    private readonly Action<short[]> fillBlock;

    private uint source;
    private uint[] buffers;
    private BufferFormat format;

    private short[] bufferData;
    private uint[] bufferQueue;

    private Task pollingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoundStream"/> class.
    /// </summary>
    /// <param name="al">The <see cref="AL"/> object.</param>
    /// <param name="sampleRate">The sample rate for the audio processing.</param>
    /// <param name="channelCount">The number of channels.</param>
    /// <param name="blockLength">The desired block length.</param>
    /// <param name="fillBlock">The callback function to fill a block.</param>
    public SoundStream(AL al, int sampleRate, int channelCount, int blockLength, Action<short[]> fillBlock)
    {
        if (al == null)
        {
            throw new ArgumentNullException(nameof(al));
        }

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

        this.al = al;
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

        source = al.GenSource();
        if (al.GetError() != AudioError.NoError)
        {
            throw new Exception("Failed to generate a sound source.");
        }

        buffers = new uint[bufferCount];
        for (var i = 0; i < buffers.Length; i++)
        {
            buffers[i] = al.GenBuffer();
            if (al.GetError() != AudioError.NoError)
            {
                throw new Exception("Failed to generate a sound buffer.");
            }
        }

        switch (channelCount)
        {
            case 1:
                format = BufferFormat.Mono16;
                break;
            case 2:
                format = BufferFormat.Stereo16;
                break;
            default:
                throw new Exception("The number of channels must be 1 or 2.");
        }

        bufferData = new short[channelCount * blockLength];
        bufferQueue = new uint[1];
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
            al.BufferData(buffers[i], format, bufferData, sampleRate);
            bufferQueue[0] = buffers[i];
            al.SourceQueueBuffers(source, bufferQueue);
        }

        al.SourcePlay(source);

        pollingTask = Task.Run(ProcessBuffers);
    }

    private void ProcessBuffers()
    {
        while (!disposed)
        {
            int processedCount;
            al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out processedCount);

            for (var i = 0; i < processedCount; i++)
            {
                fillBlock(bufferData);
                al.SourceUnqueueBuffers(source, bufferQueue);
                al.BufferData(bufferQueue[0], format, bufferData, sampleRate);
                al.SourceQueueBuffers(source, bufferQueue);
            }

            int state;
            al.GetSourceProperty(source, GetSourceInteger.SourceState, out state);

            if (state == (int)SourceState.Stopped)
            {
                al.SourcePlay(source);
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
            al.SourceStop(source);
            al.DeleteSource(source);
        }

        if (buffers != null)
        {
            for (var i = 0; i < buffers.Length; i++)
            {
                if (buffers[i] != 0)
                {
                    al.DeleteBuffer(buffers[i]);
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
}
