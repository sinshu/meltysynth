using System;
using System.Diagnostics;
using System.Threading;

public delegate void PcmConsumer(ReadOnlySpan<short> samples, int sequence);

// Offline integration adapter, not a device driver. One producer owns the synth;
// one consumer owns a block until its callback returns. Neither retains a lease.
public sealed class BoundedAudioPipe : IDisposable
{
    private sealed class Block
    {
        internal readonly short[] Samples;
        internal int Sequence;
        internal Block(int samples) { Samples = new short[samples]; }
    }
    private readonly object gate = new();
    private readonly Block?[] free, ready;
    private int freeCount, readyCount, head, tail, producerThread, consumerThread;
    private bool completed, disposed;
    public int PeakQueueDepth { get; private set; }
    public int AvailableBlocks { get { lock (gate) return freeCount; } }
    public long ResidentPcmBytes { get; }

    public BoundedAudioPipe(int capacity, int frames)
    {
        if (capacity is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (frames is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(frames));
        free = new Block[capacity]; ready = new Block[capacity]; freeCount = capacity;
        for (var i = 0; i < capacity; i++) free[i] = new Block(frames * 2);
        ResidentPcmBytes = (long)capacity * frames * 2 * sizeof(short);
    }
    private static void Claim(ref int owner)
    {
        var current = Environment.CurrentManagedThreadId;
        var previous = Interlocked.CompareExchange(ref owner, current, 0);
        if (previous != 0 && previous != current) throw new InvalidOperationException("Audio pipe endpoints each require one owner thread.");
    }
    private void Check(CancellationToken cancellation)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellation.ThrowIfCancellationRequested();
    }
    public long Produce(int sequence, Action<short[], int> fill, CancellationToken cancellation)
    {
        Claim(ref producerThread);
        var began = Stopwatch.GetTimestamp();
        Block block;
        lock (gate)
        {
            Check(cancellation);
            while (freeCount == 0 && !completed) { Monitor.Wait(gate, 20); Check(cancellation); }
            if (completed) throw new InvalidOperationException("The audio pipe is complete.");
            block = free[--freeCount] ?? throw new InvalidOperationException("Invalid free-slot ownership."); free[freeCount] = null;
        }
        var wait = Stopwatch.GetTimestamp() - began;
        var published = false;
        try
        {
            fill(block.Samples, sequence); block.Sequence = sequence;
            began = Stopwatch.GetTimestamp();
            lock (gate)
            {
                Check(cancellation);
                if (completed) throw new InvalidOperationException("The audio pipe is complete.");
                ready[tail] = block; tail = (tail + 1) % ready.Length;
                readyCount++; PeakQueueDepth = Math.Max(PeakQueueDepth, readyCount);
                published = true; Monitor.PulseAll(gate);
            }
            return wait + Stopwatch.GetTimestamp() - began;
        }
        finally { if (!published) Return(block); }
    }
    private void Return(Block block)
    {
        lock (gate) { free[freeCount++] = block; Monitor.PulseAll(gate); }
    }
    public void Complete()
    {
        lock (gate) { ObjectDisposedException.ThrowIf(disposed, this); completed = true; Monitor.PulseAll(gate); }
    }
    public void Consume(PcmConsumer sink, CancellationToken cancellation)
    {
        Claim(ref consumerThread);
        while (true)
        {
            Block block;
            lock (gate)
            {
                Check(cancellation);
                while (readyCount == 0 && !completed) { Monitor.Wait(gate, 20); Check(cancellation); }
                if (readyCount == 0) return;
                block = ready[head] ?? throw new InvalidOperationException("Invalid ready-slot ownership.");
                ready[head] = null; head = (head + 1) % ready.Length; readyCount--;
            }
            try { sink(block.Samples, block.Sequence); }
            finally { Return(block); }
        }
    }
    // Join both owners first. Retire canceled queued blocks and release all PCM references.
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            if (freeCount + readyCount != free.Length) throw new InvalidOperationException("Join audio workers before disposing their buffers.");
            disposed = true; completed = true; Array.Clear(free); Array.Clear(ready); readyCount = 0; freeCount = 0;
            Monitor.PulseAll(gate);
        }
    }
}
