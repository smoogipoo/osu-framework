﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// A key-value store which supports cleaning up items after a specified expiry time.
    /// </summary>
    public class TimedExpiryCache<TKey, TValue> : IDisposable
    {
        private readonly ConcurrentDictionary<TKey, TimedObject<TValue>> dictionary = new ConcurrentDictionary<TKey, TimedObject<TValue>>();

        /// <summary>
        /// Time in milliseconds after last access after which items will be cleaned up.
        /// </summary>
        public int ExpiryTime = 5000;

        /// <summary>
        /// The interval in milliseconds between checks for expiry.
        /// </summary>
        public readonly int CheckPeriod = 5000;

        private readonly Timer timer;

        public TimedExpiryCache()
        {
            timer = new Timer(checkExpiry, null, 0, CheckPeriod);
        }

        internal void Add(TKey key, TValue value)
        {
            dictionary.TryAdd(key, new TimedObject<TValue>(value));
        }

        private void checkExpiry(object state)
        {
            var now = DateTimeOffset.Now;

            foreach (var v in dictionary)
            {
                if ((now - v.Value.LastAccessTime).TotalMilliseconds > ExpiryTime)
                    dictionary.TryRemove(v.Key, out _);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            TimedObject<TValue> timed;

            if (!dictionary.TryGetValue(key, out timed))
            {
                value = default(TValue);
                return false;
            }

            value = timed.Value;
            return true;
        }

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                timer.Dispose();
            }
        }

        ~TimedExpiryCache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private class TimedObject<T>
        {
            public DateTimeOffset LastAccessTime;

            private readonly T value;

            public T Value
            {
                get
                {
                    updateAccessTime();
                    return value;
                }
            }

            public TimedObject(T value)
            {
                this.value = value;
                updateAccessTime();
            }

            private void updateAccessTime()
            {
                LastAccessTime = DateTimeOffset.Now;
            }
        }
    }
}
