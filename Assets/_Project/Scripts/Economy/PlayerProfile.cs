using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Economy
{
    public enum UpgradeStat
    {
        MaxHealth,
        MaxStamina,
        GrabStrength,
        SprintSpeed
    }

    [Serializable]
    public struct UpgradeLevel
    {
        public string Id;
        public int Level;
    }

    /// <summary>What gets saved. JsonUtility-friendly (no dictionaries).</summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public float Money;
        public List<UpgradeLevel> Upgrades = new List<UpgradeLevel>();
        public int HighestLevelReached;

        public int GetLevel(string id)
        {
            for (int i = 0; i < Upgrades.Count; i++)
                if (Upgrades[i].Id == id) return Upgrades[i].Level;
            return 0;
        }

        public void SetLevel(string id, int level)
        {
            for (int i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades[i].Id != id) continue;
                Upgrades[i] = new UpgradeLevel { Id = id, Level = level };
                return;
            }
            Upgrades.Add(new UpgradeLevel { Id = id, Level = level });
        }
    }

    /// <summary>Pure shop rules.</summary>
    public static class UpgradeShop
    {
        /// <summary>Cost to buy the next level: base * growth^currentLevel, rounded to a whole number.</summary>
        public static float CostFor(float baseCost, float growth, int currentLevel)
        {
            return Mathf.Round(baseCost * Mathf.Pow(Mathf.Max(1f, growth), Mathf.Max(0, currentLevel)));
        }

        public static bool CanBuy(PlayerProfile profile, UpgradeDefinition def)
        {
            int level = profile.GetLevel(def.Id);
            return level < def.MaxLevel && profile.Money >= def.CostAtLevel(level);
        }

        public static bool TryBuy(PlayerProfile profile, UpgradeDefinition def)
        {
            if (!CanBuy(profile, def)) return false;
            int level = profile.GetLevel(def.Id);
            profile.Money -= def.CostAtLevel(level);
            profile.SetLevel(def.Id, level + 1);
            return true;
        }

        /// <summary>Total bonus to a stat from every owned upgrade that targets it.</summary>
        public static float TotalBonus(PlayerProfile profile, IEnumerable<UpgradeDefinition> all, UpgradeStat stat)
        {
            float total = 0f;
            foreach (var def in all)
            {
                if (def == null || def.Stat != stat) continue;
                total += def.BonusAtLevel(profile.GetLevel(def.Id));
            }
            return total;
        }
    }
}
