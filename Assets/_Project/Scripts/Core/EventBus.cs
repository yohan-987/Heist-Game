using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Typed publish/subscribe. Systems talk through events so UI and audio can react
    /// without gameplay code ever referencing them.
    /// </summary>
    public sealed class EventBus
    {
        private interface IChannel { }

        private sealed class Channel<T> : IChannel where T : struct
        {
            public readonly List<Action<T>> Handlers = new List<Action<T>>();
            // Copy-on-publish buffer so handlers can (un)subscribe while an event is firing.
            public Action<T>[] Snapshot = Array.Empty<Action<T>>();
            public bool Dirty;
        }

        private readonly Dictionary<Type, IChannel> _channels = new Dictionary<Type, IChannel>();

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var channel = GetChannel<T>();
            channel.Handlers.Add(handler);
            channel.Dirty = true;
            return new Subscription(() => Unsubscribe(handler));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            if (!_channels.TryGetValue(typeof(T), out var raw)) return;
            var channel = (Channel<T>)raw;
            if (channel.Handlers.Remove(handler)) channel.Dirty = true;
        }

        public void Publish<T>(T evt) where T : struct
        {
            if (!_channels.TryGetValue(typeof(T), out var raw)) return;
            var channel = (Channel<T>)raw;
            if (channel.Dirty)
            {
                channel.Snapshot = channel.Handlers.ToArray();
                channel.Dirty = false;
            }

            var snapshot = channel.Snapshot;
            for (int i = 0; i < snapshot.Length; i++) snapshot[i](evt);
        }

        public int SubscriberCount<T>() where T : struct
        {
            return _channels.TryGetValue(typeof(T), out var raw) ? ((Channel<T>)raw).Handlers.Count : 0;
        }

        public void Clear() => _channels.Clear();

        private Channel<T> GetChannel<T>() where T : struct
        {
            if (!_channels.TryGetValue(typeof(T), out var raw))
            {
                raw = new Channel<T>();
                _channels[typeof(T)] = raw;
            }
            return (Channel<T>)raw;
        }

        private sealed class Subscription : IDisposable
        {
            private Action _onDispose;
            public Subscription(Action onDispose) => _onDispose = onDispose;

            public void Dispose()
            {
                _onDispose?.Invoke();
                _onDispose = null;
            }
        }
    }
}
