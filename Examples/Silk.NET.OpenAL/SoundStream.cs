using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Silk.NET.OpenAL;

public class SoundStream : IDisposable
{
    private static readonly int latency = 200;

    private bool disposed = false;

    private AL al;
    private int sampleRate;
    private int channelCount;
    private int bufferLength;
    private Action<short[]> fillBuffer;

    private uint source;
    private uint[] buffers;
    private BufferFormat format;

    private short[] bufferData;
    private uint[] bufferQueue;

    private bool stopRequested;
    private Task task;

    public SoundStream(AL al, int sampleRate, int channelCount, int bufferLength, Action<short[]> fillBuffer)
    {
        this.al = al;
        this.sampleRate = sampleRate;
        this.channelCount = channelCount;
        this.bufferLength = bufferLength;
        this.fillBuffer = fillBuffer;

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
        var bufferCount = (int)Math.Ceiling((double)(sampleRate * latency) / (1000 * bufferLength));
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

        bufferData = new short[channelCount * bufferLength];
        bufferQueue = new uint[1];
    }

    public void Start()
    {
        if (task != null)
        {
            return;
        }

        for (var i = 0; i < buffers.Length; i++)
        {
            fillBuffer(bufferData);
            al.BufferData(buffers[i], format, bufferData, sampleRate);
            bufferQueue[0] = buffers[i];
            al.SourceQueueBuffers(source, bufferQueue);
        }

        al.SourcePlay(source);

        stopRequested = false;
        task = Task.Run(ProcessBuffers);
    }

    private void ProcessBuffers()
    {
        while (!stopRequested)
        {
            int processedCount;
            al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out processedCount);

            for (var i = 0; i < processedCount; i++)
            {
                fillBuffer(bufferData);
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

        stopRequested = true;

        if (disposing)
        {
            if (task != null)
            {
                task.Wait();
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

        disposed = true;
    }

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
