using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Trigger volume where valuables are delivered. Only valuables fully inside the box and not
    /// currently held count. Publishes <see cref="ExtractionValueChanged"/> whenever the total changes,
    /// so a smashed vase inside the zone drains the total.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ExtractionZone : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float _checkInterval = 0.2f;

        private readonly HashSet<Valuable> _candidates = new HashSet<Valuable>();
        private readonly List<Valuable> _counted = new List<Valuable>();
        private BoxCollider _box;
        private float _timer;
        private float _lastTotal = -1f;
        private int _lastCount = -1;

        public float Total { get; private set; }
        public int Count => _counted.Count;
        public IReadOnlyList<Valuable> Counted => _counted;

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        private void OnEnable() => Services.Events.Subscribe<ValuableDamaged>(OnDamaged);

        private void OnDisable()
        {
            Services.Events.Unsubscribe<ValuableDamaged>(OnDamaged);
            _candidates.Clear();
            _counted.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            var v = other.GetComponentInParent<Valuable>();
            if (v != null) _candidates.Add(v);
        }

        private void OnTriggerExit(Collider other)
        {
            var v = other.GetComponentInParent<Valuable>();
            if (v == null) return;

            // A valuable can have several colliders; only drop it once none of them overlap the zone.
            if (!StillOverlapping(v)) _candidates.Remove(v);
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            if (_timer < _checkInterval) return;
            _timer = 0f;
            Recalculate();
        }

        private void OnDamaged(ValuableDamaged evt) => Recalculate();

        public void Recalculate()
        {
            _counted.Clear();
            float total = 0f;
            var bounds = _box.bounds;

            _candidates.RemoveWhere(v => v == null);
            foreach (var v in _candidates)
            {
                if (v.IsHeld) continue;
                if (!IsFullyInside(v, bounds)) continue;

                _counted.Add(v);
                total += v.CurrentValue;
            }

            Total = total;
            if (!Mathf.Approximately(total, _lastTotal) || _counted.Count != _lastCount)
            {
                _lastTotal = total;
                _lastCount = _counted.Count;
                Services.Events.Publish(new ExtractionValueChanged(total, _counted.Count));
            }
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
