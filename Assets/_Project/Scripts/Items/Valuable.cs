using Game.Core;
using Game.Interaction;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Runtime state of one valuable: current value, damage from impacts, noise from impacts.
    /// Behaviour lives here; numbers live in the <see cref="ValuableDefinition"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Valuable : MonoBehaviour
    {
        [SerializeField] private ValuableDefinition _definition;
        [Tooltip("Ignore further hits for this long after one registers, so one bounce isn't counted many times.")]
        [SerializeField, Min(0f)] private float _hitCooldown = 0.06f;

        private Rigidbody _body;
        private Grabbable _grabbable;
        private EntityIdentity _identity;
        private float _lastHitTime = -999f;

        public ValuableDefinition Definition => _definition;
        public float CurrentValue { get; private set; }
        public float BaseValue => _definition != null ? _definition.BaseValue : 0f;
        public float TotalLost => Mathf.Max(0f, BaseValue - CurrentValue);
        public bool IsBroken => _definition != null && CurrentValue <= BaseValue * _definition.BrokenFraction;
        public bool IsHeld => _grabbable != null && _grabbable.IsHeld;

        public GameEntityId Id
        {
            get
            {
                if (_identity == null) _identity = GetComponent<EntityIdentity>();
                return _identity != null ? _identity.Id : GameEntityId.None;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _grabbable = GetComponent<Grabbable>();
            Initialise(_definition);
        }

        /// <summary>Call after spawning with a definition when it wasn't assigned in the prefab.</summary>
        public void Initialise(ValuableDefinition definition)
        {
            _definition = definition;
            if (_definition == null)
            {
                Debug.LogWarning("Valuable has no definition.", this);
                return;
            }

            CurrentValue = _definition.BaseValue;
            if (_body != null) _body.mass = _definition.Mass;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_definition == null) return;
            if (Time.time - _lastHitTime < _hitCooldown) return;

            float impulse = collision.impulse.magnitude;

            // Noise first: even a hit that costs no money can still wake something up.
            float loudness = NoiseMath.ImpactLoudness(impulse, _definition.MinNoiseImpulse,
                _definition.LoudnessPerImpulse, _definition.MaxLoudness);
            if (loudness > 0f)
            {
                var point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
                Services.Events.Publish(new NoiseEmitted(point, loudness, NoiseSource.Impact));
            }

            float loss = ValueLoss.Compute(impulse, _definition.ImpactThreshold, _definition.LossPerImpulse,
                _definition.BaseValue, CurrentValue);
            if (loss <= 0f) return;

            _lastHitTime = Time.time;
            CurrentValue -= loss;

            Services.Events.Publish(new ValuableDamaged(Id, loss, CurrentValue, transform.position));
        }
    }
}
