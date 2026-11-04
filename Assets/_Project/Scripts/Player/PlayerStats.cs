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

        [Header("Hits (GAME_DESIGN 4.4)")]
        [Tooltip("Seconds of protection after a hit lands, so one mistake is a scare and two are death.")]
        [SerializeField, Min(0f)] private float _hitProtectionSeconds = 2f;
        [Tooltip("Health restored per second while standing in a safe room. There is no regeneration anywhere else.")]
        [SerializeField, Min(0f)] private float _safeRoomRegenPerSecond = 5f;
        [Tooltip("Speed of the shove a hit gives the player.")]
        [SerializeField, Min(0f)] private float _knockbackSpeed = 6f;

        [Header("Stamina")]
        [SerializeField, Min(1f)] private float _maxStamina = 100f;
        [SerializeField, Min(0f)] private float _sprintDrainPerSecond = 18f;
        [SerializeField, Min(0f)] private float _recoverPerSecond = 14f;
        [SerializeField, Min(0f)] private float _recoverDelay = 1.2f;
        [SerializeField, Range(0f, 1f)] private float _exhaustRecoverFraction = 0.3f;

        public HealthModel Health { get; private set; }
        public HitModel Hits { get; private set; }
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
            Hits = new HitModel(_hitProtectionSeconds, _safeRoomRegenPerSecond);
            Stamina = new StaminaModel(_maxStamina, _sprintDrainPerSecond, _recoverPerSecond,
                _recoverDelay, _exhaustRecoverFraction);
        }

        private void OnEnable() => Services.Players.Register(this);

        private void OnDisable() => Services.Players.Unregister(this);

        private void Update()
        {
            if (!IsAlive) return;

            float dt = Services.Clock.DeltaTime;
            Hits.Tick(dt);

            float heal = Hits.Regen(dt, Services.SafeZones.IsSafe(transform.position));
            if (heal > 0f) Health.Heal(heal);
        }

        public void ApplyDamage(in DamageInfo info)
        {
            // Protection after a recent hit absorbs the whole blow.
            float landed = Hits.Absorb(info.Amount);
            if (landed <= 0f) return;

            if (Health.Damage(landed))
            {
                Services.Events.Publish(new PlayerDied(_playerId));
                return;
            }

            var motor = GetComponent<PlayerMotor>();
            if (motor != null && info.Direction.sqrMagnitude > 0.0001f)
                motor.Knockback(info.Direction.normalized * _knockbackSpeed);
        }

        /// <summary>
        /// Kills outright, ignoring hit protection. Used by the Pressure burst, which must never leave a player
        /// downed (GAME_DESIGN 4.11).
        /// </summary>
        public void Kill()
        {
            if (!IsAlive) return;
            if (Health.Damage(Health.Current)) Services.Events.Publish(new PlayerDied(_playerId));
        }

        /// <summary>Applied by the meta-progression layer at the start of a run.</summary>
        public void ApplyUpgrades(float maxHealth, float maxStamina)
        {
            Health.SetMax(maxHealth, refill: true);
            Stamina.SetMax(maxStamina, refill: true);
        }
    }
}
