using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum GameState
    {
        Boot,
        Menu,
        Lobby,
        Loading,
        InLevel,
        Extraction,
        Results,
        GameOver
    }

    /// <summary>
    /// Owns the high-level flow and publishes <see cref="GameStateChanged"/>.
    /// Only transitions listed in the table are allowed, so a stray call can't skip the results screen.
    /// </summary>
    public sealed class GameStateMachine
    {
        private static readonly Dictionary<GameState, GameState[]> Allowed = new Dictionary<GameState, GameState[]>
        {
            { GameState.Boot,       new[] { GameState.Menu } },
            { GameState.Menu,       new[] { GameState.Lobby, GameState.Loading } },
            { GameState.Lobby,      new[] { GameState.Menu, GameState.Loading } },
            { GameState.Loading,    new[] { GameState.InLevel, GameState.Menu } },
            { GameState.InLevel,    new[] { GameState.Extraction, GameState.Results, GameState.GameOver } },
            { GameState.Extraction, new[] { GameState.InLevel, GameState.Results, GameState.GameOver } },
            { GameState.Results,    new[] { GameState.Menu, GameState.Lobby, GameState.Loading } },
            { GameState.GameOver,   new[] { GameState.Menu, GameState.Lobby } },
        };

        private readonly EventBus _events;
        private readonly StateMachine<GameState> _machine = new StateMachine<GameState>();

        public GameState Current => _machine.Current;
        public GameState Previous => _machine.Previous;

        public GameStateMachine(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _machine.Changed += (from, to) => _events.Publish(new GameStateChanged(from, to));
            _machine.Start(GameState.Boot);
        }

        public bool CanTransitionTo(GameState next)
        {
            if (!Allowed.TryGetValue(Current, out var options)) return false;
            return Array.IndexOf(options, next) >= 0;
        }

        /// <summary>Returns false (and does nothing) if the transition isn't allowed.</summary>
        public bool TryTransitionTo(GameState next)
        {
            if (!CanTransitionTo(next)) return false;
            return _machine.TransitionTo(next);
        }

        public void Update(float deltaTime) => _machine.Update(deltaTime);
    }
}
