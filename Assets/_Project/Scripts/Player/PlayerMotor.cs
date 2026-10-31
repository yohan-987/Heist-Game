using Game.Core;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Character-controller movement driven entirely by <see cref="PlayerInputState"/>.
    /// Walk, sprint, crouch, jump, stamina, carry slowdown and footstep noise.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Speeds (m/s)")]
        [SerializeField, Min(0f)] private float _walkSpeed = 3.5f;
        [SerializeField, Min(0f)] private float _sprintSpeed = 6f;
        [SerializeField, Min(0f)] private float _crouchSpeed = 1.6f;
        [SerializeField, Min(0f)] private float _acceleration = 40f;

        [Header("Jump and gravity")]
        [SerializeField, Min(0f)] private float _jumpHeight = 1.0f;
        [SerializeField] private float _gravity = -22f;
        [SerializeField, Min(0f)] private float _jumpStaminaCost = 8f;

        [Header("Crouch")]
        [SerializeField, Min(0.5f)] private float _standHeight = 1.8f;
        [SerializeField, Min(0.5f)] private float _crouchHeight = 1.1f;
        [SerializeField, Min(0f)] private float _crouchLerp = 12f;
        [SerializeField] private Transform _head;
        [SerializeField, Min(0.1f)] private float _eyeInset = 0.15f;

        [Header("Carry")]
        [SerializeField, Min(0f)] private float _carryDrainPerKg = 0.8f;

        [Header("Footstep noise")]
        [SerializeField, Min(0.1f)] private float _strideLength = 2.2f;
        [SerializeField, Min(0f)] private float _walkNoise = 3f;
        [SerializeField, Min(0f)] private float _sprintNoise = 11f;
        [SerializeField, Min(0f)] private float _crouchNoise = 0.8f;

        private CharacterController _controller;
        private PlayerStats _stats;
        private IPlayerInputSource _input;
        private ICarrySource _carry;
        private NoiseEmitter _noise;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _strideAccumulator;
        private bool _crouching;

        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public bool IsSprinting { get; private set; }
        public bool IsCrouching => _crouching;
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stats = GetComponent<PlayerStats>();
            _input = GetComponent<IPlayerInputSource>();
            _carry = GetComponent<ICarrySource>();
            _noise = GetComponent<NoiseEmitter>();
            if (_head == null) _head = _stats.Head;
            ApplyHeight(_standHeight);
        }

        private void Update()
        {
            if (_input == null || !_stats.IsAlive) return;

            float dt = Services.Clock.DeltaTime;
            if (dt <= 0f) return;

            var input = _input.Current;

            UpdateCrouch(input, dt);
            Vector3 wish = ComputeWish(input, out bool moving);
            UpdateSprintAndStamina(input, moving, dt);
            UpdateHorizontal(wish, dt);
            UpdateVertical(input, dt);

            var motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * dt;
            _controller.Move(motion);
            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f; // keep grounded

            UpdateFootsteps(dt, moving);
        }

        private Vector3 ComputeWish(in PlayerInputState input, out bool moving)
        {
            var move = input.Move;
            moving = move.sqrMagnitude > 0.0001f;
            return (transform.right * move.x + transform.forward * move.y);
        }

        private void UpdateCrouch(in PlayerInputState input, float dt)
        {
            // Only stand up if there is headroom.
            bool wantCrouch = input.Crouch;
            if (!wantCrouch && _crouching && !HasHeadroom()) wantCrouch = true;
            _crouching = wantCrouch;

            float target = _crouching ? _crouchHeight : _standHeight;
            ApplyHeight(Mathf.Lerp(_controller.height, target, 1f - Mathf.Exp(-_crouchLerp * dt)));
        }

        private void ApplyHeight(float height)
        {
            _controller.height = height;
            _controller.center = new Vector3(0f, height * 0.5f, 0f);
            if (_head != null)
            {
                var p = _head.localPosition;
                p.y = height - _eyeInset;
                _head.localPosition = p;
            }
        }

        private bool HasHeadroom()
        {
            float radius = _controller.radius * 0.95f;
            Vector3 origin = transform.position + Vector3.up * _controller.height;
            float needed = _standHeight - _controller.height;
            if (needed <= 0.01f) return true;
            return !Physics.SphereCast(origin - Vector3.up * radius, radius, Vector3.up, out _,
                needed, GameLayers.SightBlockerMask, QueryTriggerInteraction.Ignore);
        }

        private void UpdateSprintAndStamina(in PlayerInputState input, bool moving, float dt)
        {
            IsSprinting = input.Sprint && moving && !_crouching && _stats.Stamina.CanSprint;

            float extraDrain = _carry != null ? _carry.HeldMass * _carryDrainPerKg : 0f;
            _stats.Stamina.Tick(dt, IsSprinting, extraDrain);
        }

        private void UpdateHorizontal(Vector3 wish, float dt)
        {
            float speed = _crouching ? _crouchSpeed : (IsSprinting ? _sprintSpeed : _walkSpeed);
            if (_carry != null) speed *= Mathf.Clamp01(_carry.SpeedMultiplier);

            Vector3 target = wish * speed;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, _acceleration * dt);
        }

        private void UpdateVertical(in PlayerInputState input, float dt)
        {
            if (_controller.isGrounded && input.JumpPressed && !_crouching && _stats.Stamina.TrySpend(_jumpStaminaCost))
            {
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
            else
            {
                _verticalVelocity += _gravity * dt;
            }
        }

        private void UpdateFootsteps(float dt, bool moving)
        {
            if (_noise == null || !_controller.isGrounded || !moving) return;

            _strideAccumulator += new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z).magnitude * dt;
            if (_strideAccumulator < _strideLength) return;
            _strideAccumulator = 0f;

            if (_crouching) _noise.Emit(_crouchNoise, NoiseSource.Footstep);
            else if (IsSprinting) _noise.Emit(_sprintNoise, NoiseSource.Sprint);
            else _noise.Emit(_walkNoise, NoiseSource.Footstep);
        }
    }
}
