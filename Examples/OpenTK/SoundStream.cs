using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

public class SoundStream : IDisposable
{
    private static readonly int latency = 200;

    private bool disposed = false;

    private int sampleRate;
    private int channelCount;
    private int bufferSize;
    private Action<short[]> fillBuffer;

    private int source;
    private int[] buffers;
    private ALFormat format;

    private short[] bufferData;
    private int[] bufferQueue;

    private bool stopRequested;
    private Task task;

    public SoundStream(int sampleRate, int channelCount, int bufferSize, Action<short[]> fillBuffer)
    {
        this.sampleRate = sampleRate;
        this.channelCount = channelCount;
        this.bufferSize = bufferSize;
        this.fillBuffer = fillBuffer;

        var bufferCount = (int)Math.Ceiling((double)(sampleRate * latency) / (1000 * bufferSize));
        if (bufferCount < 2)
        {
            bufferCount = 2;
        }

        source = AL.GenSource();

        buffers = new int[bufferCount];
        for (var i = 0; i < buffers.Length; i++)
        {
            buffers[i] = AL.GenBuffer();
        }

        format = channelCount == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;

        bufferData = new short[channelCount * bufferSize];
        bufferQueue = new int[1];
    }

    public void Start()
    {
        for (var i = 0; i < buffers.Length; i++)
        {
            fillBuffer(bufferData);
            AL.BufferData(buffers[i], format, bufferData, sampleRate);
            bufferQueue[0] = buffers[i];
            AL.SourceQueueBuffers(source, bufferQueue);
        }

        AL.SourcePlay(source);

        stopRequested = false;
        task = Task.Run(Run);
    }

    private void Run()
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

        if (disposing)
        {
            if (task != null)
            {
                stopRequested = true;
                task.Wait();
                task = null;
            }
        }

        AL.SourceStop(source);
        AL.DeleteSource(source);

        for (var i = 0; i < buffers.Length; i++)
        {
            AL.DeleteBuffer(buffers[i]);
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
