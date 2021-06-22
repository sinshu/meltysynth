using System;
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
    private int bufferSize;
    private Action<short[]> fillBuffer;

    private uint source;
    private uint[] buffers;
    private BufferFormat format;

    private short[] bufferData;
    private uint[] bufferQueue;

    private bool stopRequested;
    private Task task;

    public SoundStream(AL al, int sampleRate, int channelCount, int bufferSize, Action<short[]> fillBuffer)
    {
        this.al = al;
        this.sampleRate = sampleRate;
        this.channelCount = channelCount;
        this.bufferSize = bufferSize;
        this.fillBuffer = fillBuffer;

        var bufferCount = (int)Math.Ceiling((double)(sampleRate * latency) / (1000 * bufferSize));
        if (bufferCount < 2)
        {
            bufferCount = 2;
        }

        source = al.GenSource();

        buffers = new uint[bufferCount];
        for (var i = 0; i < buffers.Length; i++)
        {
            buffers[i] = al.GenBuffer();
        }

        format = channelCount == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16;

        bufferData = new short[channelCount * bufferSize];
        bufferQueue = new uint[1];
    }

    public void Start()
    {
        for (var i = 0; i < buffers.Length; i++)
        {
            fillBuffer(bufferData);
            al.BufferData(buffers[i], format, bufferData, sampleRate);
            bufferQueue[0] = buffers[i];
            al.SourceQueueBuffers(source, bufferQueue);
        }

        al.SourcePlay(source);

        stopRequested = false;
        task = Task.Run(Run);
    }

    private void Run()
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

        if (disposing)
        {
            if (task != null)
            {
                stopRequested = true;
                task.Wait();
                task = null;
            }
        }

        al.SourceStop(source);
        al.DeleteSource(source);

        for (var i = 0; i < buffers.Length; i++)
        {
            al.DeleteBuffer(buffers[i]);
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
