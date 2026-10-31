using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Tuning data for one enemy type. Create via Assets > Create > Game > Enemy.</summary>
    [CreateAssetMenu(menuName = "Game/Enemy", fileName = "Enemy_New")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName = "Enemy";
        public GameObject Prefab;

        [Header("Movement")]
        [Min(0.1f)] public float PatrolSpeed = 1.8f;
        [Min(0.1f)] public float InvestigateSpeed = 2.8f;
        [Min(0.1f)] public float ChaseSpeed = 4.6f;

        [Header("Sight")]
        [Min(0f)] public float SightRange = 14f;
        [Range(5f, 180f)] public float SightHalfAngle = 55f;
        [Tooltip("Seconds of broken line of sight before a chase gives up and investigates the last known position.")]
        [Min(0f)] public float LoseSightTime = 3f;

        [Header("Hearing")]
        [Tooltip("Multiplies how far noise carries to this enemy. 0 = deaf.")]
        [Min(0f)] public float HearingSensitivity = 1f;
        [Tooltip("Noise weaker than this perceived fraction (0-1) is ignored.")]
        [Range(0f, 1f)] public float HearingThreshold = 0.1f;
        [Min(0f)] public float InvestigateTime = 5f;

        [Header("Attack")]
        [Min(0.1f)] public float AttackRange = 1.6f;
        [Min(0f)] public float AttackDamage = 25f;
        [Min(0.1f)] public float AttackCooldown = 1.4f;

        [Header("Level generation")]
        [Min(0f)] public float SpawnCost = 1f;
        [Min(0f)] public float SpawnWeight = 1f;
    }
}
