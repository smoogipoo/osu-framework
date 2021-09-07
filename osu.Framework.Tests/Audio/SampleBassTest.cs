// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassTest
    {
        private BassTestComponents bass;
        private Sample sample;
        private SampleChannel channel;

        [SetUp]
        public void Setup()
        {
            bass = new BassTestComponents();
            sample = bass.GetSample();

            bass.Update();
        }

        [TearDown]
        public void Teardown()
        {
            bass?.Dispose();
        }

        [Test]
        public void TestGetChannelOnDisposed()
        {
            sample.Dispose();

            sample.Update();

            Assert.Throws<ObjectDisposedException>(() => sample.GetChannel());
            Assert.Throws<ObjectDisposedException>(() => sample.Play());
        }

        [Test]
        public void TestStart()
        {
            channel = sample.Play();
            bass.Update();

            Thread.Sleep(50);

            bass.Update();

            Assert.IsTrue(channel.Playing);
        }

        [Test]
        public void TestStop()
        {
            channel = sample.Play();
            bass.Update();

            channel.Stop();
            bass.Update();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStopBeforeLoadFinished()
        {
            channel = sample.Play();
            channel.Stop();

            bass.Update();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStopsWhenFactoryDisposed()
        {
            channel = sample.Play();
            bass.Update();

            bass.SampleStore.Dispose();
            bass.Update();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestSampleFactoryDefaultConcurrencyPassedToSamples()
        {
            bass.SampleStore.DefaultPlaybackConcurrency = 1;
            bass.Update();

            var sample1 = bass.GetSample();
            Assert.That(sample1.PlaybackConcurrency, Is.EqualTo(1));

            bass.SampleStore.DefaultPlaybackConcurrency = 2;
            bass.Update();

            var sample2 = bass.GetSample();
            Assert.That(sample1.PlaybackConcurrency, Is.EqualTo(1)); // Ensure the first sample keeps the previous concurrency.
            Assert.That(sample2.PlaybackConcurrency, Is.EqualTo(2));
        }

        [Test]
        public void TestOldSamplesStoppedWhenConcurrencyLimitExceeded()
        {
            sample.PlaybackConcurrency = 2;
            bass.Update();

            var channel1 = sample.GetChannel();
            var channel2 = sample.GetChannel();
            var channel3 = sample.GetChannel();
            var channel4 = sample.GetChannel();

            channel1.Looping = true;
            channel2.Looping = true;
            channel3.Looping = true;
            channel4.Looping = true;

            channel1.Play();
            channel2.Play();
            bass.Update();

            Assert.That(channel1.Playing, Is.True);
            Assert.That(channel2.Playing, Is.True);
            Assert.That(channel3.Playing, Is.False);
            Assert.That(channel4.Playing, Is.False);

            channel3.Play();
            channel4.Play();
            bass.Update();

            Assert.That(channel1.Playing, Is.False);
            Assert.That(channel2.Playing, Is.False);
            Assert.That(channel3.Playing, Is.True);
            Assert.That(channel4.Playing, Is.True);
        }

        [Test]
        public void TestStoppedConcurrencyConsidersStoppedSamples()
        {
            sample.PlaybackConcurrency = 2;
            bass.Update();

            var channel1 = sample.GetChannel();
            var channel2 = sample.GetChannel();
            var channel3 = sample.GetChannel();

            channel1.Looping = true;
            channel2.Looping = true;
            channel3.Looping = true;

            channel1.Play();
            channel2.Play();
            bass.Update();

            Assert.That(channel1.Playing, Is.True);
            Assert.That(channel2.Playing, Is.True);
            Assert.That(channel3.Playing, Is.False);

            channel2.Stop();
            channel3.Play();
            bass.Update();

            Assert.That(channel1.Playing, Is.True);
            Assert.That(channel2.Playing, Is.False);
            Assert.That(channel3.Playing, Is.True);
        }

        [Test]
        public void TestSettingSampleConcurrencyStopsSamplesExceedingConcurrency()
        {
            sample.PlaybackConcurrency = 2;
            bass.Update();

            var channel1 = sample.GetChannel();
            var channel2 = sample.GetChannel();

            channel1.Looping = true;
            channel2.Looping = true;

            channel1.Play();
            channel2.Play();
            bass.Update();

            Assert.That(channel1.Playing, Is.True);
            Assert.That(channel2.Playing, Is.True);

            sample.PlaybackConcurrency = 1;
            bass.Update();

            Assert.That(channel1.Playing, Is.False);
            Assert.That(channel2.Playing, Is.True);
        }
    }
}
