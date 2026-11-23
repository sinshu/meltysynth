// SPDX-License-Identifier: MIT
// Copyright (C) 2026 James Carpenter
// Contributed by KilnGod (James Carpenter).
// See LICENSE.txt for the full MIT license and inherited notices.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MeltySynth;
using NUnit.Framework;

namespace MeltySynthTest
{
    public class BoundedAudioPipeTest
    {
        [TestCase(false)]
        [TestCase(true)]
        public void SingleProducerMatchesSequentialAudioAndReturnsEveryBuffer(bool effects)
        {
            const int blocks = 128, frames = 64;
            using var pipe = new BoundedAudioPipe(4, frames);
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var producerThread = 0; var consumerThread = 0; var consumed = 0; var checksum = "";
            var producer = Task.Factory.StartNew(() =>
            {
                try
                {
                    producerThread = Environment.CurrentManagedThreadId;
                    var synth = SynthesisBaselineScenario.Synth(effects); synth.NoteOn(0, 60, 100);
                    Action<short[], int> fill = (samples, _) => synth.RenderInterleavedInt16(samples);
                    for (var i = 0; i < blocks; i++) pipe.Produce(i, fill, cancel.Token);
                    pipe.Complete();
                }
                catch { cancel.Cancel(); throw; }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            var consumer = Task.Factory.StartNew(() =>
            {
                try
                {
                    consumerThread = Environment.CurrentManagedThreadId;
                    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    pipe.Consume((samples, index) =>
                    {
                        Assert.That(index, Is.EqualTo(consumed++)); hash.AppendData(MemoryMarshal.AsBytes(samples));
                    }, cancel.Token);
                    checksum = Convert.ToHexString(hash.GetHashAndReset());
                }
                catch { cancel.Cancel(); throw; }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.That(Task.WaitAll(new[] { producer, consumer }, TimeSpan.FromSeconds(6)), Is.True);
            var reference = SynthesisBaselineScenario.Synth(effects); reference.NoteOn(0, 60, 100);
            using var expected = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var pcm = new short[frames * 2];
            for (var i = 0; i < blocks; i++) { reference.RenderInterleavedInt16(pcm); expected.AppendData(MemoryMarshal.AsBytes(pcm.AsSpan())); }
            Assert.That(checksum, Is.EqualTo(Convert.ToHexString(expected.GetHashAndReset())));
            Assert.That(consumed, Is.EqualTo(blocks)); Assert.That(producerThread, Is.Not.EqualTo(consumerThread));
            Assert.That(pipe.AvailableBlocks, Is.EqualTo(4)); Assert.That(pipe.PeakQueueDepth, Is.InRange(1, 4));
            Assert.That(pipe.ResidentPcmBytes, Is.EqualTo(1024));
        }

        [Test]
        public void SaturationBackpressuresAndCancellationReturnsOwnedAndQueuedBlocks()
        {
            using var pipe = new BoundedAudioPipe(2, 64);
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var full = new ManualResetEventSlim();
            var producer = Task.Factory.StartNew(() =>
            {
                try
                {
                    pipe.Produce(0, (samples, _) => samples[0] = 1, cancel.Token);
                    pipe.Produce(1, (samples, _) => samples[0] = 2, cancel.Token);
                    full.Set();
                    Assert.Throws<OperationCanceledException>(() => pipe.Produce(2, (_, _) => { }, cancel.Token));
                }
                finally { pipe.Complete(); }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.That(full.Wait(TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(producer.Wait(30), Is.False, "Producer must wait for a returned slot.");
            Assert.That(pipe.PeakQueueDepth, Is.EqualTo(2)); Assert.That(pipe.AvailableBlocks, Is.Zero);
            cancel.Cancel(); Assert.That(producer.Wait(TimeSpan.FromSeconds(2)), Is.True);
            var consumed = 0;
            pipe.Consume((samples, index) => { Assert.That(samples[0], Is.EqualTo(index + 1)); consumed++; }, CancellationToken.None);
            Assert.That(consumed, Is.EqualTo(2)); Assert.That(pipe.AvailableBlocks, Is.EqualTo(2));
        }

        [Test]
        public void FillAndConsumerFailuresReleaseTheirLease()
        {
            using var pipe = new BoundedAudioPipe(1, 64);
            Assert.Throws<InvalidDataException>(() => pipe.Produce(0, (_, _) => throw new InvalidDataException(), CancellationToken.None));
            Assert.That(pipe.AvailableBlocks, Is.EqualTo(1));
            pipe.Produce(0, (_, _) => { }, CancellationToken.None); pipe.Complete();
            Assert.Throws<InvalidDataException>(() => pipe.Consume((_, _) => throw new InvalidDataException(), CancellationToken.None));
            Assert.That(pipe.AvailableBlocks, Is.EqualTo(1));
        }

        [Test]
        public void AnEndpointCannotSwitchOwnerThreads()
        {
            using var pipe = new BoundedAudioPipe(2, 64);
            pipe.Produce(0, (_, _) => { }, CancellationToken.None);
            var wrongOwner = Task.Factory.StartNew(() =>
                Assert.Throws<InvalidOperationException>(() => pipe.Produce(1, (_, _) => { }, CancellationToken.None)),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.That(wrongOwner.Wait(TimeSpan.FromSeconds(2)), Is.True);
            pipe.Complete(); pipe.Consume((_, _) => { }, CancellationToken.None);
            Assert.That(pipe.AvailableBlocks, Is.EqualTo(2));
        }

        [Test]
        public void RepeatedCompletedAndCanceledShutdownReleasesTheBoundedPool()
        {
            for (var i = 0; i < 20; i++)
            {
                using var pipe = new BoundedAudioPipe(2, 64);
                pipe.Produce(0, (_, _) => { }, CancellationToken.None); pipe.Complete();
                if (i % 2 == 0) pipe.Consume((_, _) => { }, CancellationToken.None);
                // Otherwise disposal retires the queued block after its owner has stopped.
            }
        }

        [Test]
        public void ConsumerCancellationDoesNotRetainAnInFlightBlock()
        {
            using var pipe = new BoundedAudioPipe(1, 64);
            using var cancel = new CancellationTokenSource();
            pipe.Produce(0, (_, _) => { }, CancellationToken.None);
            Assert.Throws<OperationCanceledException>(() => pipe.Consume((_, _) => { cancel.Cancel(); cancel.Token.ThrowIfCancellationRequested(); }, cancel.Token));
            Assert.That(pipe.AvailableBlocks, Is.EqualTo(1)); pipe.Complete();
        }
    }
}
