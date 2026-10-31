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
            if (_scheduler.TryHallucination(Services.Clock.DeltaTime, _localInsanity, out int id))
                Services.Events.Publish(new Jumpscare(id, ScareKind.Hallucination));
        }

        /// <summary>A scripted scare was reached. Returns true if it fired.</summary>
        public bool TriggerScripted(int scareId)
        {
            if (!_scheduler.TryTriggerScripted(scareId)) return false;
            Services.Events.Publish(new Jumpscare(scareId, ScareKind.Scripted));
            return true;
        }

        public void ResetRun() => _scheduler.Reset();
    }
}
