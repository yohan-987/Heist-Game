using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>Health and stamina for one player, plus registration with the PlayerRegistry.</summary>
    public sealed class PlayerStats : MonoBehaviour, IPlayerAvatar, IDamageable
    {
        [Header("Identity")]
        [SerializeField] private int _playerId;
        [SerializeField] private bool _isLocal = true;
        [SerializeField] private Transform _head;

        [Header("Health")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;

        [Header("Stamina")]
        [SerializeField, Min(1f)] private float _maxStamina = 100f;
        [SerializeField, Min(0f)] private float _sprintDrainPerSecond = 18f;
        [SerializeField, Min(0f)] private float _recoverPerSecond = 14f;
        [SerializeField, Min(0f)] private float _recoverDelay = 1.2f;
        [SerializeField, Range(0f, 1f)] private float _exhaustRecoverFraction = 0.3f;

        public HealthModel Health { get; private set; }
        public StaminaModel Stamina { get; private set; }

        // IPlayerAvatar
        public int PlayerId => _playerId;
        public Transform Transform => transform;
        public Transform Head => _head != null ? _head : transform;
        public bool IsAlive => Health != null && Health.IsAlive;
        public bool IsLocal => _isLocal;

        private void Awake()
        {
            Health = new HealthModel(_maxHealth);
            Stamina = new StaminaModel(_maxStamina, _sprintDrainPerSecond, _recoverPerSecond,
                _recoverDelay, _exhaustRecoverFraction);
        }

        private void OnEnable() => Services.Players.Register(this);

        private void OnDisable() => Services.Players.Unregister(this);

        public void ApplyDamage(in DamageInfo info)
        {
            if (Health.Damage(info.Amount))
            {
                Services.Events.Publish(new PlayerDied(_playerId));
            }
        }

        /// <summary>Applied by the meta-progression layer at the start of a run.</summary>
        public void ApplyUpgrades(float maxHealth, float maxStamina)
        {
            Health.SetMax(maxHealth, refill: true);
            Stamina.SetMax(maxStamina, refill: true);
        }
    }
}
