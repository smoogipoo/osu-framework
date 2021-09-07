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
        public void TestPlaybackDoesNotExceedConcurrency()
        {
            bass.RunOnAudioThread(() => sample.PlaybackConcurrency.Value = 2);

            var channel1 = sample.GetChannel();
            var channel2 = sample.GetChannel();
            var channel3 = sample.GetChannel();

            channel1.Looping = true;
            channel2.Looping = true;
            channel3.Looping = true;

            channel1.Play();
            channel2.Play();
            bass.Update();

            Assert.That(channel1.Playing);
            Assert.That(channel2.Playing);
            Assert.That(!channel3.Playing);

            channel3.Play();
            bass.Update();

            Assert.That(!channel1.Playing);
            Assert.That(channel2.Playing);
            Assert.That(channel3.Playing);
        }
    }
}
