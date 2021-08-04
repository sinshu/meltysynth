using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

public class SoundStream : IDisposable
{
    private static readonly int latency = 200;

    private bool disposed = false;

    private int sampleRate;
    private int channelCount;
    private int bufferLength;
    private Action<short[]> fillBuffer;

    private int source;
    private int[] buffers;
    private ALFormat format;

    private short[] bufferData;
    private int[] bufferQueue;

    private bool stopRequested;
    private Task task;

    public SoundStream(int sampleRate, int channelCount, int bufferLength, Action<short[]> fillBuffer)
    {
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

        bufferData = new short[channelCount * bufferLength];
        bufferQueue = new int[1];
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
            AL.BufferData(buffers[i], format, bufferData, sampleRate);
            bufferQueue[0] = buffers[i];
            AL.SourceQueueBuffers(source, bufferQueue);
        }

        AL.SourcePlay(source);

        stopRequested = false;
        task = Task.Run(ProcessBuffers);
    }

    private void ProcessBuffers()
    {
        while (!stopRequested)
        {
            int processedCount;
            AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processedCount);

            for (var i = 0; i < processedCount; i++)
            {
                fillBuffer(bufferData);
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
