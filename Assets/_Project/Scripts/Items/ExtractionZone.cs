using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// The compounder's intake. A valuable that is fully inside, not held and settled for a moment is
    /// accepted: its current value is committed permanently (<see cref="ValuableDelivered"/>) and the
    /// object is consumed. Damage before acceptance costs value; nothing can change it afterwards.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ExtractionZone : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float _checkInterval = 0.2f;
        [Tooltip("How long a valuable must lie still inside before it is accepted.")]
        [SerializeField, Min(0f)] private float _settleSeconds = 0.5f;
        [Tooltip("Faster than this (m/s) counts as still moving.")]
        [SerializeField, Min(0f)] private float _maxSettleSpeed = 0.6f;

        private readonly HashSet<Valuable> _candidates = new HashSet<Valuable>();
        private readonly Dictionary<Valuable, float> _settled = new Dictionary<Valuable, float>();
        private readonly List<Valuable> _scratch = new List<Valuable>();
        private BoxCollider _box;
        private float _timer;

        /// <summary>Total value this intake has accepted.</summary>
        public float CommittedTotal { get; private set; }
        public int AcceptedCount { get; private set; }

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        private void OnDisable()
        {
            _candidates.Clear();
            _settled.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            var v = other.GetComponentInParent<Valuable>();
            if (v != null && !v.IsDelivered) _candidates.Add(v);
        }

        private void OnTriggerExit(Collider other)
        {
            var v = other.GetComponentInParent<Valuable>();
            if (v == null) return;

            // A valuable can have several colliders; only drop it once none of them overlap the zone.
            if (!StillOverlapping(v))
            {
                _candidates.Remove(v);
                _settled.Remove(v);
            }
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            if (_timer < _checkInterval) return;

            float elapsed = _timer;
            _timer = 0f;
            Process(elapsed);
        }

        private void Process(float elapsed)
        {
            var bounds = _box.bounds;

            _scratch.Clear();
            _scratch.AddRange(_candidates);

            foreach (var v in _scratch)
            {
                if (v == null || v.IsDelivered)
                {
                    _candidates.Remove(v);
                    _settled.Remove(v);
                    continue;
                }

                bool ready = !v.IsHeld && v.Speed <= _maxSettleSpeed && IsFullyInside(v, bounds);
                if (!ready)
                {
                    _settled[v] = 0f;
                    continue;
                }

                _settled.TryGetValue(v, out float time);
                time += elapsed;
                _settled[v] = time;

                if (time >= _settleSeconds) Accept(v);
            }
        }

        private void Accept(Valuable v)
        {
            float value = v.CurrentValue;
            var id = Services.Spawner.EnsureIdentity(v.gameObject);

            v.MarkDelivered();
            _candidates.Remove(v);
            _settled.Remove(v);

            CommittedTotal += value;
            AcceptedCount++;

            Services.Events.Publish(new ValuableDelivered(id, value));
            Services.Events.Publish(new ExtractionValueChanged(CommittedTotal, AcceptedCount));

            // Deactivate first: Destroy is deferred, and a half-gone object must not keep colliding.
            v.gameObject.SetActive(false);
            Services.Spawner.Despawn(v.gameObject);
        }

        private static bool IsFullyInside(Valuable v, Bounds zone)
        {
            var colliders = v.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0) return false;

            for (int i = 0; i < colliders.Length; i++)
            {
                var b = colliders[i].bounds;
                if (!zone.Contains(b.min) || !zone.Contains(b.max)) return false;
            }
            return true;
        }

        private bool StillOverlapping(Valuable v)
        {
            var bounds = _box.bounds;
            foreach (var c in v.GetComponentsInChildren<Collider>())
                if (bounds.Intersects(c.bounds)) return true;
            return false;
        }

        private void OnDrawGizmos()
        {
            var box = _box != null ? _box : GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
