using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Minimal table-driven state machine. Used for game flow and enemy AI.
    /// States are registered with optional enter/update/exit callbacks.
    /// </summary>
    public sealed class StateMachine<TState> where TState : struct, Enum
    {
        private sealed class Callbacks
        {
            public Action<TState> OnEnter;   // receives the previous state
            public Action<float> OnUpdate;   // receives delta time
            public Action<TState> OnExit;    // receives the next state
        }

        private readonly Dictionary<TState, Callbacks> _states = new Dictionary<TState, Callbacks>();
        private bool _hasState;

        public TState Current { get; private set; }
        public TState Previous { get; private set; }
        public float TimeInState { get; private set; }

        /// <summary>Fires after a transition completes (previous, current).</summary>
        public event Action<TState, TState> Changed;

        public StateMachine<TState> Add(
            TState state,
            Action<TState> onEnter = null,
            Action<float> onUpdate = null,
            Action<TState> onExit = null)
        {
            _states[state] = new Callbacks { OnEnter = onEnter, OnUpdate = onUpdate, OnExit = onExit };
            return this;
        }

        /// <summary>Sets the first state without running exit logic.</summary>
        public void Start(TState initial)
        {
            Previous = initial;
            Current = initial;
            TimeInState = 0f;
            _hasState = true;
            if (_states.TryGetValue(initial, out var cb)) cb.OnEnter?.Invoke(initial);
        }

        public bool TransitionTo(TState next)
        {
            if (!_hasState)
            {
                Start(next);
                return true;
            }

            if (EqualityComparer<TState>.Default.Equals(Current, next)) return false;

            var from = Current;
            if (_states.TryGetValue(from, out var oldCb)) oldCb.OnExit?.Invoke(next);

            Previous = from;
            Current = next;
            TimeInState = 0f;

            if (_states.TryGetValue(next, out var newCb)) newCb.OnEnter?.Invoke(from);
            Changed?.Invoke(from, next);
            return true;
        }

        public void Update(float deltaTime)
        {
            if (!_hasState) return;
            TimeInState += deltaTime;
            if (_states.TryGetValue(Current, out var cb)) cb.OnUpdate?.Invoke(deltaTime);
        }
    }
}
