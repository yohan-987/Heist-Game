using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Decides when scares fire. It publishes <see cref="Jumpscare"/> events; audio and visual systems
    /// decide how a scare looks and sounds. Scripted scares come from <see cref="ScareTrigger"/>s,
    /// hallucinations are rolled here from the local player's insanity.
    /// </summary>
    public sealed class JumpscareDirector : MonoBehaviour
    {
        [SerializeField] private int _seed = 1;
        [SerializeField, Min(1)] private int _hallucinationKinds = 4;
        [SerializeField, Min(0f)] private float _minGapSeconds = 45f;

        // Hazard ids for scares sit in their own range so they never collide with enemy ids.
        private const int ScareHazardBase = 1000000;
        private const float ScareHazardSeconds = 4f;

        private JumpscareScheduler _scheduler;
        private float _localInsanity;

        private void Awake() => Initialise(_seed);

        /// <summary>Call with the level seed so scares are reproducible for a given level.</summary>
        public void Initialise(int seed)
        {
            _scheduler = new JumpscareScheduler(new DeterministicRandom(seed), _hallucinationKinds, _minGapSeconds);
        }

        private void OnEnable() => Services.Events.Subscribe<InsanityChanged>(OnInsanity);

        private void OnDisable() => Services.Events.Unsubscribe<InsanityChanged>(OnInsanity);

        private void OnInsanity(InsanityChanged evt)
        {
            var local = Services.Players.Local;
            if (local != null && local.PlayerId == evt.PlayerId) _localInsanity = evt.Value;
        }

        private void Update()
        {
            Services.Threats.Tick(Services.Clock.DeltaTime);

            if (_scheduler.TryHallucination(Services.Clock.DeltaTime, _localInsanity, out int id))
                Services.Events.Publish(new Jumpscare(id, ScareKind.Hallucination));
        }

        /// <summary>A scripted scare was reached. Returns true if it fired.</summary>
        public bool TriggerScripted(int scareId)
        {
            var local = Services.Players.Local;
            if (local != null)
            {
                // A scripted scare is a disruptive hazard: it waits while a doctor is on the player (GAME_DESIGN 4.9).
                var check = Services.Threats.Check(local.PlayerId, HazardClass.Disruptive, local.Transform.position);
                if (check != HazardCheck.Allowed) return false;
            }

            bool costsPressure;
            if (!_scheduler.TryTriggerScripted(scareId, out costsPressure)) return false;

            if (local != null)
                Services.Threats.TryBegin(local.PlayerId, ScareHazardBase + scareId, HazardClass.Disruptive,
                    local.Transform.position, ScareHazardSeconds);

            Services.Events.Publish(new Jumpscare(scareId, ScareKind.Scripted, costsPressure));
            return true;
        }

        public void ResetRun() => _scheduler.Reset();
    }
}
