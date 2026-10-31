using Game.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Enemies
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Investigate,
        Chase,
        Attack,
        Cooldown
    }

    /// <summary>
    /// NavMeshAgent + state machine: Idle -> Patrol -> Investigate(noise) -> Chase -> Attack -> Cooldown.
    /// Hears noise through the event bus, sees through a cone plus a line-of-sight ray.
    /// Intended to run on the host only once networking exists.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyBrain : MonoBehaviour, INoiseListener
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private Transform _eyes;
        [Tooltip("Optional fixed route. If empty the enemy wanders near where it spawned.")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField, Min(1f)] private float _wanderRadius = 12f;
        [SerializeField, Min(0f)] private float _idleTimeBetweenPatrols = 1.5f;
        [SerializeField, Min(0.05f)] private float _repathInterval = 0.2f;
        [SerializeField, Min(0.05f)] private float _senseInterval = 0.1f;

        private NavMeshAgent _agent;
        private readonly StateMachine<EnemyState> _fsm = new StateMachine<EnemyState>();

        private Vector3 _home;
        private int _waypointIndex;
        private Vector3 _investigatePoint;
        private float _investigateTimer;
        private float _timeSinceSeen;
        private float _repathTimer;
        private float _senseTimer;
        private float _attackTimer;
        private IPlayerAvatar _target;
        private Vector3 _lastKnownTargetPosition;

        public EnemyState State => _fsm.Current;
        public EnemyDefinition Definition => _definition;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_eyes == null) _eyes = transform;
            _home = transform.position;

            _fsm
                .Add(EnemyState.Idle, onEnter: _ => { _agent.ResetPath(); }, onUpdate: UpdateIdle)
                .Add(EnemyState.Patrol, onEnter: _ => BeginPatrol(), onUpdate: UpdatePatrol)
                .Add(EnemyState.Investigate, onEnter: _ => BeginInvestigate(), onUpdate: UpdateInvestigate)
                .Add(EnemyState.Chase, onEnter: _ => BeginChase(), onUpdate: UpdateChase)
                .Add(EnemyState.Attack, onEnter: _ => { _agent.ResetPath(); _attackTimer = 0f; }, onUpdate: UpdateAttack)
                .Add(EnemyState.Cooldown, onEnter: _ => _agent.ResetPath(), onUpdate: UpdateCooldown);
        }

        private void Start()
        {
            if (_definition == null)
            {
                Debug.LogError("EnemyBrain has no definition.", this);
                enabled = false;
                return;
            }
            _fsm.Start(EnemyState.Idle);
        }

        private void OnEnable() => Services.Events.Subscribe<NoiseEmitted>(HandleNoiseEvent);

        private void OnDisable() => Services.Events.Unsubscribe<NoiseEmitted>(HandleNoiseEvent);

        private void Update()
        {
            float dt = Services.Clock.DeltaTime;
            if (dt <= 0f) return;

            _senseTimer -= dt;
            if (_senseTimer <= 0f)
            {
                _senseTimer = _senseInterval;
                Sense();
            }

            _fsm.Update(dt);
        }

        // ---------- Perception ----------

        private void HandleNoiseEvent(NoiseEmitted noise) => OnNoiseHeard(noise);

        public void OnNoiseHeard(in NoiseEmitted noise)
        {
            if (_definition == null) return;
            // Already hunting or recovering: noise doesn't override that.
            var s = _fsm.Current;
            if (s == EnemyState.Chase || s == EnemyState.Attack || s == EnemyState.Cooldown) return;

            float distance = Vector3.Distance(transform.position, noise.Position);
            float perceived = NoiseMath.Perceived(noise.Loudness, distance, _definition.HearingSensitivity);
            if (!EnemySenses.ShouldReactToNoise(perceived, _definition.HearingThreshold)) return;

            _investigatePoint = noise.Position;
            if (s == EnemyState.Investigate) BeginInvestigate(); // retarget
            else _fsm.TransitionTo(EnemyState.Investigate);
        }

        private void Sense()
        {
            var s = _fsm.Current;
            var seen = FindVisiblePlayer();

            if (seen != null)
            {
                _target = seen;
                _lastKnownTargetPosition = seen.Transform.position;
                _timeSinceSeen = 0f;
                if (s != EnemyState.Chase && s != EnemyState.Attack && s != EnemyState.Cooldown)
                    _fsm.TransitionTo(EnemyState.Chase);
            }
            else
            {
                _timeSinceSeen += _senseInterval;
            }
        }

        private IPlayerAvatar FindVisiblePlayer()
        {
            var players = Services.Players.All;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (!p.IsAlive) continue;

                Vector3 eye = _eyes.position;
                Vector3 target = p.Head.position;

                if (!EnemySenses.InVisionCone(eye, _eyes.forward, target, _definition.SightRange, _definition.SightHalfAngle))
                    continue;

                if (Physics.Linecast(eye, target, GameLayers.SightBlockerMask, QueryTriggerInteraction.Ignore))
                    continue;

                return p;
            }
            return null;
        }

        // ---------- States ----------

        private void UpdateIdle(float dt)
        {
            if (_fsm.TimeInState >= _idleTimeBetweenPatrols) _fsm.TransitionTo(EnemyState.Patrol);
        }

        private void BeginPatrol()
        {
            _agent.speed = _definition.PatrolSpeed;
            _agent.isStopped = false;
            if (!TryPickPatrolPoint(out var point))
            {
                _fsm.TransitionTo(EnemyState.Idle);
                return;
            }
            _agent.SetDestination(point);
        }

        private void UpdatePatrol(float dt)
        {
            if (Arrived()) _fsm.TransitionTo(EnemyState.Idle);
        }

        private bool TryPickPatrolPoint(out Vector3 point)
        {
            if (_waypoints != null && _waypoints.Length > 0)
            {
                _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
                point = _waypoints[_waypointIndex].position;
                return true;
            }

            Vector2 offset = Random.insideUnitCircle * _wanderRadius;
            Vector3 candidate = _home + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(candidate, out var hit, 4f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
            point = _home;
            return false;
        }

        private void BeginInvestigate()
        {
            _agent.speed = _definition.InvestigateSpeed;
            _agent.isStopped = false;
            _investigateTimer = 0f;
            if (NavMesh.SamplePosition(_investigatePoint, out var hit, 4f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
            else
                _agent.SetDestination(_investigatePoint);
        }

        private void UpdateInvestigate(float dt)
        {
            if (!Arrived()) return;

            // Linger at the spot for a moment, then give up.
            _investigateTimer += dt;
            if (_investigateTimer >= _definition.InvestigateTime) _fsm.TransitionTo(EnemyState.Idle);
        }

        private void BeginChase()
        {
            _agent.speed = _definition.ChaseSpeed;
            _agent.isStopped = false;
            _repathTimer = 0f;
        }

        private void UpdateChase(float dt)
        {
            if (_target == null || !_target.IsAlive)
            {
                _fsm.TransitionTo(EnemyState.Idle);
                return;
            }

            Vector3 goal = _timeSinceSeen < 0.5f ? _target.Transform.position : _lastKnownTargetPosition;

            _repathTimer -= dt;
            if (_repathTimer <= 0f)
            {
                _repathTimer = _repathInterval;
                _agent.SetDestination(goal);
            }

            if (_timeSinceSeen <= 0.5f &&
                Vector3.Distance(transform.position, _target.Transform.position) <= _definition.AttackRange)
            {
                _fsm.TransitionTo(EnemyState.Attack);
                return;
            }

            if (_timeSinceSeen >= _definition.LoseSightTime)
            {
                _investigatePoint = _lastKnownTargetPosition;
                _fsm.TransitionTo(EnemyState.Investigate);
            }
        }

        private void UpdateAttack(float dt)
        {
            if (_target == null || !_target.IsAlive)
            {
                _fsm.TransitionTo(EnemyState.Idle);
                return;
            }

            // Face the target while winding up.
            Vector3 flat = _target.Transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 10f * dt);

            _attackTimer += dt;
            if (_attackTimer < 0.35f) return; // short wind-up so players can react

            if (Vector3.Distance(transform.position, _target.Transform.position) <= _definition.AttackRange * 1.25f)
            {
                var damageable = _target.Transform.GetComponentInParent<IDamageable>();
                damageable?.ApplyDamage(new DamageInfo(_definition.AttackDamage, _target.Transform.position,
                    flat.normalized, this));
            }

            _fsm.TransitionTo(EnemyState.Cooldown);
        }

        private void UpdateCooldown(float dt)
        {
            if (_fsm.TimeInState < _definition.AttackCooldown) return;
            _fsm.TransitionTo(_timeSinceSeen < 1f ? EnemyState.Chase : EnemyState.Idle);
        }

        private bool Arrived()
        {
            if (_agent.pathPending) return false;
            return _agent.remainingDistance <= _agent.stoppingDistance + 0.2f;
        }
    }
}
