using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Single registry for the handful of global services. Defaults are created on first access,
    /// so the Sandbox scene works without going through Boot; <see cref="Bootstrap"/> resets them
    /// at startup to guarantee a clean slate (also covers "Enter Play Mode without domain reload").
    /// </summary>
    public static class Services
    {
        private static EventBus _events;
        private static GameClock _clock;
        private static PlayerRegistry _players;
        private static GameStateMachine _state;
        private static EntityIdProvider _ids;
        private static Spawner _spawner;
        private static DifficultyProfile _difficulty;
        private static ThreatDirector _threats;
        private static SafeZoneRegistry _safeZones;

        /// <summary>Current difficulty numbers. Assign a new profile from the menu or level setup.</summary>
        public static DifficultyProfile Difficulty
        {
            get => _difficulty ??= DifficultyProfile.For(DifficultyLevel.Normal);
            set => _difficulty = value ?? DifficultyProfile.For(DifficultyLevel.Normal);
        }

        public static EventBus Events => _events ??= new EventBus();
        public static GameClock Clock => _clock ??= new GameClock();
        public static PlayerRegistry Players => _players ??= new PlayerRegistry();
        public static GameStateMachine State => _state ??= new GameStateMachine(Events);
        public static EntityIdProvider Ids => _ids ??= new EntityIdProvider();
        public static Spawner Spawner => _spawner ??= new Spawner(Ids);
        public static ThreatDirector Threats => _threats ??= new ThreatDirector();
        public static SafeZoneRegistry SafeZones => _safeZones ??= new SafeZoneRegistry();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            _events?.Clear();
            _events = null;
            _clock = null;
            _players = null;
            _state = null;
            _ids = null;
            _spawner = null;
            _difficulty = null;
            _threats = null;
            _safeZones = null;
        }
    }
}
