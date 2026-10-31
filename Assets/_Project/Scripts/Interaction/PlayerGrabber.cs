using Game.Core;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// The player's hands. Finds a Grabbable with a sphere cast from the head, then drives its
    /// Rigidbody toward a hold point in FixedUpdate (velocity, not parenting) so held objects still
    /// collide with the world. Input is sampled in Update, physics happens in FixedUpdate.
    /// </summary>
    public sealed class PlayerGrabber : MonoBehaviour, ICarrySource
    {
        [Header("Reach")]
        [SerializeField, Min(0.5f)] private float _reach = 3.2f;
        [SerializeField, Min(0.01f)] private float _castRadius = 0.18f;

        [Header("Hold")]
        [SerializeField, Min(0.3f)] private float _defaultHoldDistance = 1.8f;
        [SerializeField, Min(0.3f)] private float _minHoldDistance = 0.9f;
        [SerializeField, Min(0.3f)] private float _maxHoldDistance = 3.0f;
        [SerializeField, Min(0f)] private float _scrollStep = 0.25f;
        [SerializeField, Min(0f)] private float _rotateStepDegrees = 15f;
        [SerializeField, Min(0.1f)] private float _holdGain = 14f;
        [SerializeField, Min(0.1f)] private float _maxHoldSpeed = 14f;
        [SerializeField, Min(0.1f)] private float _autoReleaseDistance = 2.2f;

        [Header("Weight")]
        [SerializeField, Min(0f)] private float _lightMass = 2f;
        [SerializeField, Min(0.1f)] private float _heavyMass = 25f;
        [SerializeField, Range(0.05f, 1f)] private float _heaviestSpeedMultiplier = 0.4f;
        [SerializeField, Min(0f)] private float _maxCarryMass = 60f;

        [Header("Throw")]
        [SerializeField, Min(0f)] private float _throwSpeed = 11f;
        [SerializeField, Min(0f)] private float _maxThrowSpeed = 16f;

        private IPlayerInputSource _input;
        private IPlayerAvatar _avatar;
        private Transform _head;
        private Grabbable _held;
        private float _holdDistance;
        private float _pendingYawDegrees;

        public bool IsHolding => _held != null;
        public Grabbable Held => _held;
        public int PlayerId => _avatar != null ? _avatar.PlayerId : 0;

        // ICarrySource
        public float HeldMass => _held != null ? _held.Mass : 0f;
        public float SpeedMultiplier => _held == null
            ? 1f
            : GrabMath.MassSpeedMultiplier(_held.Mass, _lightMass, _heavyMass, _heaviestSpeedMultiplier);

        private void Awake()
        {
            _input = GetComponent<IPlayerInputSource>();
            _avatar = GetComponent<IPlayerAvatar>();
            _head = _avatar != null ? _avatar.Head : transform;
            _holdDistance = _defaultHoldDistance;
        }

        private void Update()
        {
            if (_input == null || Services.Clock.Paused) return;

            if (_avatar != null && !_avatar.IsAlive)
            {
                ForceRelease();
                return;
            }

            var input = _input.Current;

            if (_held == null)
            {
                if (input.GrabPressed) TryGrab();
                return;
            }

            if (!input.GrabHeld)
            {
                Release();
                return;
            }

            if (input.ThrowPressed)
            {
                Throw();
                return;
            }

            if (input.RotateHeld)
                _pendingYawDegrees += input.Scroll * _rotateStepDegrees;
            else
                _holdDistance = GrabMath.AdjustHoldDistance(_holdDistance, input.Scroll, _scrollStep,
                    _minHoldDistance, _maxHoldDistance);
        }

        private void FixedUpdate()
        {
            if (_held == null) return;

            var body = _held.Body;
            Vector3 target = _head.position + _head.forward * _holdDistance;

            float distance = Vector3.Distance(body.position, target);
            if (GrabMath.ShouldAutoRelease(distance, _autoReleaseDistance))
            {
                Release();
                return;
            }

            body.linearVelocity = GrabMath.HoldVelocity(body.position, target, _holdGain, _maxHoldSpeed);
            body.angularVelocity = Vector3.zero; // keep it from spinning wildly in the hand

            if (!Mathf.Approximately(_pendingYawDegrees, 0f))
            {
                body.MoveRotation(Quaternion.AngleAxis(_pendingYawDegrees, _head.up) * body.rotation);
                _pendingYawDegrees = 0f;
            }
        }

        public bool TryGrab()
        {
            if (_held != null) return false;

            // Cast against grabbables and walls together so you can't grab through a wall.
            int mask = GameLayers.GrabbableMask | GameLayers.SightBlockerMask;
            if (!Physics.SphereCast(_head.position, _castRadius, _head.forward, out var hit, _reach, mask,
                    QueryTriggerInteraction.Ignore))
                return false;

            var body = hit.collider.attachedRigidbody;
            if (body == null) return false;

            var grabbable = body.GetComponent<Grabbable>();
            if (grabbable == null || grabbable.Mass > _maxCarryMass) return false;

            if (!grabbable.RequestGrab(this)) return false;

            _held = grabbable;
            _holdDistance = Mathf.Clamp(
                Vector3.Distance(_head.position, body.position), _minHoldDistance, _maxHoldDistance);
            _pendingYawDegrees = 0f;
            return true;
        }

        public void Release()
        {
            if (_held == null) return;
            var g = _held;
            _held = null;
            g.Release(this);
        }

        public void Throw()
        {
            if (_held == null) return;
            var g = _held;
            var body = g.Body;
            Release();

            float speed = GrabMath.ThrowSpeed(body.mass, _throwSpeed, _maxThrowSpeed);
            body.linearVelocity = _head.forward * speed;
        }

        /// <summary>Called when the held object is disabled/destroyed or the player dies.</summary>
        public void ForceRelease()
        {
            if (_held == null) return;
            var g = _held;
            _held = null;
            g.Release(this);
        }

        private void OnDisable() => ForceRelease();

        private void OnDrawGizmosSelected()
        {
            var head = _head != null ? _head : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(head.position + head.forward * _holdDistance, _castRadius);
        }
    }
}
