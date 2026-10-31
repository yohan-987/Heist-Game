using UnityEngine;

namespace Game.Items
{
    /// <summary>Tuning data for one kind of valuable. Create via Assets > Create > Game > Valuable.</summary>
    [CreateAssetMenu(menuName = "Game/Valuable", fileName = "Valuable_New")]
    public sealed class ValuableDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName = "Valuable";
        public GameObject Prefab;

        [Header("Economy")]
        [Min(1f)] public float BaseValue = 500f;

        [Header("Physics")]
        [Min(0.05f)] public float Mass = 3f;

        [Header("Damage")]
        [Tooltip("Collision impulse (N*s) below this does nothing. Resting it gently costs zero.")]
        [Min(0f)] public float ImpactThreshold = 6f;

        [Tooltip("Fraction of BaseValue lost per 1 N*s of impulse above the threshold. Higher = more fragile.")]
        [Min(0f)] public float LossPerImpulse = 0.01f;

        [Tooltip("Value below this fraction of BaseValue counts as broken.")]
        [Range(0f, 1f)] public float BrokenFraction = 0.05f;

        [Header("Noise")]
        [Min(0f)] public float MinNoiseImpulse = 3f;
        [Min(0f)] public float LoudnessPerImpulse = 0.8f;
        [Min(0f)] public float MaxLoudness = 25f;

        [Header("Level generation")]
        [Min(0f)] public float SpawnWeight = 1f;
    }
}
