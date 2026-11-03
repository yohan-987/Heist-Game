using Game.Core;
using Game.Interaction;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// A throwable that scares doctors away. Throwing it arms it; the first hit sets off a
    /// <see cref="RepelBurst"/> and adds to the thrower's insanity, then the apple is used up.
    /// On high difficulty it rots over time and loses its effect.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public sealed class Apple : MonoBehaviour
    {
        [Header("Effect")]
        [SerializeField, Min(0f)] private float _repelRadius = 9f;
        [SerializeField, Min(0f)] private float _repelSeconds = 6f;
        [SerializeField, Min(0f)] private float _insanityOnThrow = 10f;
        [Tooltip("A thrown apple only counts if it hits something within this many seconds.")]
        [SerializeField, Min(0.5f)] private float _armedSeconds = 4f;
        [Tooltip("The splat is heard this far. Keep it larger than the repel radius so doctors just outside it investigate.")]
        [SerializeField, Min(0f)] private float _splatNoiseRadius = 12f;

        [Header("Rot (high difficulty only)")]
        [SerializeField, Min(1f)] private float _rotSeconds = 90f;
        [SerializeField, Range(0f, 1f)] private float _rottenThreshold = 0.15f;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _freshColor = new Color(0.8f, 0.1f, 0.1f);
        [SerializeField] private Color _rottenColor = new Color(0.25f, 0.18f, 0.08f);

        private Grabbable _grabbable;
        private RotModel _rot;
        private Material _material;
        private bool _armed;
        private int _throwerId;
        private float _armedAt;

        public float Freshness => _rot != null ? _rot.Freshness : 1f;
        public bool IsRotten => _rot != null && _rot.IsRotten;

        private void Awake()
        {
            _grabbable = GetComponent<Grabbable>();
            _rot = new RotModel(Services.Difficulty.ApplesRot, _rotSeconds, _rottenThreshold);
            if (_renderer != null) _material = _renderer.material;
        }

        private void OnEnable() => Services.Events.Subscribe<ObjectThrown>(OnThrown);

        private void OnDisable() => Services.Events.Unsubscribe<ObjectThrown>(OnThrown);

        private void Update()
        {
            float dt = Services.Clock.DeltaTime;
            if (dt <= 0f) return;

            _rot.Tick(dt);
            if (_material != null && _rot.Rots)
                _material.color = Color.Lerp(_rottenColor, _freshColor, _rot.Freshness);
        }

        private void OnThrown(ObjectThrown evt)
        {
            if (!evt.Id.IsValid || evt.Id != _grabbable.Id) return;
            _armed = true;
            _throwerId = evt.PlayerId;
            _armedAt = Time.time;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_armed) return;

            if (Time.time - _armedAt > _armedSeconds)
            {
                _armed = false; // too late, it was just lying around
                return;
            }

            Splat();
        }

        private void Splat()
        {
            _armed = false;

            float effect = _rot.Effectiveness;
            if (effect > 0f)
            {
                Services.Events.Publish(new RepelBurst(transform.position, _repelRadius * effect, _repelSeconds * effect));
                Services.Events.Publish(new InsanityAdded(_throwerId, _insanityOnThrow));
            }

            // Never free: the splat itself is a noise, whatever the apple's condition.
            if (_splatNoiseRadius > 0f)
                Services.Events.Publish(new NoiseEmitted(transform.position, _splatNoiseRadius, NoiseSource.Impact));

            Services.Spawner.Despawn(gameObject);
        }
    }
}
