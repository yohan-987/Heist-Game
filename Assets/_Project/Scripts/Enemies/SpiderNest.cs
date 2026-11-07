using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// A dormant nest of spiders. When music is heard and a player is close, spiders crawl onto that
    /// player's screen (see the UI crawler overlay). Harmless apart from blocking vision briefly.
    /// </summary>
    public sealed class SpiderNest : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _hearingSensitivity = 1f;
        [Tooltip("A player must be within this distance of the nest to be attacked.")]
        [SerializeField, Min(0.5f)] private float _attackRange = 8f;
        [SerializeField, Min(1)] private int _minSpiders = 2;
        [SerializeField, Min(1)] private int _maxSpiders = 4;
        [SerializeField, Min(0f)] private float _cooldownSeconds = 20f;
        [Tooltip("How long a swarm counts as an active hazard for the threat director. Matches the overlay's cap of about 8 seconds.")]
        [SerializeField, Min(0f)] private float _hazardSeconds = 8f;

        private float _cooldown;

        private void OnEnable() => Services.Events.Subscribe<NoiseEmitted>(OnNoise);

        private void OnDisable() => Services.Events.Unsubscribe<NoiseEmitted>(OnNoise);

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Services.Clock.DeltaTime;
        }

        private void OnNoise(NoiseEmitted noise)
        {
            if (noise.Source != NoiseSource.Music || _cooldown > 0f) return;

            float distance = Vector3.Distance(transform.position, noise.Position);
            if (NoiseMath.Perceived(noise.Loudness, distance, _hearingSensitivity) <= 0f) return;

            var target = Services.Players.Nearest(transform.position, out float playerDistance);
            if (target == null || playerDistance > _attackRange) return;

            // Disruptive hazard: waits while a doctor is on or near the player, or two hazards are already active.
            if (!Services.Threats.TryBegin(target.PlayerId, InstanceKey.Of(this), HazardClass.Disruptive,
                    target.Transform.position, _hazardSeconds)) return;

            int max = Mathf.Max(_minSpiders, _maxSpiders);
            int count = Random.Range(_minSpiders, max + 1);
            Services.Events.Publish(new SpiderAttack(target.PlayerId, count));
            _cooldown = _cooldownSeconds;
        }
    }
}
