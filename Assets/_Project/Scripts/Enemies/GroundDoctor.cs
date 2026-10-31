using Game.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Enemies
{
    /// <summary>
    /// A dead doctor lying on the floor. Music wakes it; it crawls to the nearest player and holds
    /// them by the waist, slowing them. The player breaks free by pressing jump repeatedly; a thrown
    /// apple also drives it off. It then rests and goes dormant again.
    /// Not lethal on its own, but deadly together with a chasing doctor.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GroundDoctor : MonoBehaviour
    {
        [Header("Hearing")]
        [SerializeField, Min(0f)] private float _hearingSensitivity = 1f;

        [Header("Behaviour")]
        [SerializeField, Min(0f)] private float _wakeSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float _crawlSpeed = 2.2f;
        [SerializeField, Min(0.1f)] private float _grabDistance = 1.1f;
        [Tooltip("Speed multiplier applied to a held player.")]
        [SerializeField, Range(0.1f, 1f)] private float _slowMultiplier = 0.4f;
        [Tooltip("Progress added per press of the struggle button (1 frees the player).")]
        [SerializeField, Range(0.01f, 1f)] private float _struggleGain = 0.14f;
        [SerializeField, Min(0f)] private float _struggleDecayPerSecond = 0.15f;
        [SerializeField, Min(0f)] private float _cooldownSeconds = 15f;
        [SerializeField, Min(0.05f)] private float _repathInterval = 0.25f;
        [SerializeField] private Vector3 _holdOffset = new Vector3(0f, 0.9f, 0.3f);

        private NavMeshAgent _agent;
        private DormantGrabberModel _model;
        private GrabberState _lastState = GrabberState.Dormant;
        private IPlayerAvatar _victim;
        private ISpeedModifierTarget _victimMotor;
        private IPlayerInputSource _victimInput;
        private float _repathTimer;

        public GrabberState State => _model.State;
        public float StruggleProgress => _model.Progress;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _crawlSpeed;
            _model = new DormantGrabberModel(_wakeSeconds, _struggleDecayPerSecond, _cooldownSeconds);
        }

        private void OnEnable()
        {
            Services.Events.Subscribe<NoiseEmitted>(OnNoise);
            Services.Events.Subscribe<RepelBurst>(OnRepel);
        }

        private void OnDisable()
        {
            Services.Events.Unsubscribe<NoiseEmitted>(OnNoise);
            Services.Events.Unsubscribe<RepelBurst>(OnRepel);

            // Only undo the slowdown here. Re-parenting is not allowed while objects are being
            // deactivated or destroyed, and the player's own cleanup handles the rest.
            _victimMotor?.Modifiers.Remove(this);
        }

        private void OnNoise(NoiseEmitted noise)
        {
            if (noise.Source != NoiseSource.Music || _model.State != GrabberState.Dormant) return;

            float distance = Vector3.Distance(transform.position, noise.Position);
            if (NoiseMath.Perceived(noise.Loudness, distance, _hearingSensitivity) > 0f) _model.Wake();
        }

        private void OnRepel(RepelBurst burst)
        {
            if (Vector3.Distance(transform.position, burst.Position) > burst.Radius) return;
            _model.Break();
        }

        private void Update()
        {
            float dt = Services.Clock.DeltaTime;
            if (dt <= 0f) return;

            if (_model.State == GrabberState.Crawling) UpdateCrawl(dt);
            else if (_model.State == GrabberState.Holding) UpdateHold();

            _model.Tick(dt);

            if (_model.State != _lastState)
            {
                OnStateChanged(_lastState, _model.State);
                _lastState = _model.State;
            }
        }

        private void UpdateCrawl(float dt)
        {
            var target = Services.Players.Nearest(transform.position, out float distance);
            if (target == null) return;

            if (distance <= _grabDistance)
            {
                _victim = target;
                _model.Grab();
                return;
            }

            _repathTimer -= dt;
            if (_repathTimer <= 0f && _agent.enabled && _agent.isOnNavMesh)
            {
                _repathTimer = _repathInterval;
                _agent.SetDestination(target.Transform.position);
            }
        }

        private void UpdateHold()
        {
            if (_victim == null || !_victim.IsAlive)
            {
                _model.Break();
                return;
            }

            if (_victimInput != null && _victimInput.Current.JumpPressed) _model.Struggle(_struggleGain);
        }

        private void OnStateChanged(GrabberState from, GrabberState to)
        {
            if (from == GrabberState.Holding) ReleaseVictim();

            switch (to)
            {
                case GrabberState.Crawling:
                    if (_agent.enabled && _agent.isOnNavMesh)
                    {
                        _agent.speed = _crawlSpeed;
                        _agent.isStopped = false;
                    }
                    break;

                case GrabberState.Holding:
                    AttachVictim();
                    break;

                case GrabberState.Waking:
                case GrabberState.Cooldown:
                case GrabberState.Dormant:
                    if (_agent.enabled && _agent.isOnNavMesh)
                    {
                        _agent.ResetPath();
                        _agent.isStopped = true;
                    }
                    break;
            }
        }

        private void AttachVictim()
        {
            if (_victim == null) return;

            _victimMotor = _victim.Transform.GetComponentInParent<ISpeedModifierTarget>();
            _victimInput = _victim.Transform.GetComponentInParent<IPlayerInputSource>();
            _victimMotor?.Modifiers.Set(this, _slowMultiplier);

            // Ride along with the player while holding on.
            _agent.enabled = false;
            transform.SetParent(_victim.Transform, true);
            transform.localPosition = _holdOffset;

            Services.Events.Publish(new PlayerGrabbedByEnemy(_victim.PlayerId, Services.Spawner.EnsureIdentity(gameObject)));
        }

        private void ReleaseVictim()
        {
            _victimMotor?.Modifiers.Remove(this);

            transform.SetParent(null, true);
            _agent.enabled = true;
            if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
                _agent.Warp(hit.position);

            if (_victim != null)
                Services.Events.Publish(new PlayerReleasedByEnemy(_victim.PlayerId, Services.Spawner.EnsureIdentity(gameObject)));

            _victim = null;
            _victimMotor = null;
            _victimInput = null;
        }
    }
}
