using UnityEngine;

namespace Game.Economy
{
    /// <summary>One purchasable upgrade. Create via Assets > Create > Game > Upgrade.</summary>
    [CreateAssetMenu(menuName = "Game/Upgrade", fileName = "Upgrade_New")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [Tooltip("Stable id used in save files. Never change it after release.")]
        public string Id = "upgrade_id";
        public string DisplayName = "Upgrade";
        [TextArea] public string Description;
        public UpgradeStat Stat = UpgradeStat.MaxHealth;

        [Min(0f)] public float BaseCost = 500f;
        [Min(1f)] public float CostGrowth = 1.5f;
        [Min(1)] public int MaxLevel = 5;

        [Tooltip("Added to the stat for each level bought.")]
        public float ValuePerLevel = 10f;

        public float CostAtLevel(int currentLevel) => UpgradeShop.CostFor(BaseCost, CostGrowth, currentLevel);
        public float BonusAtLevel(int level) => ValuePerLevel * Mathf.Max(0, level);
    }
}
