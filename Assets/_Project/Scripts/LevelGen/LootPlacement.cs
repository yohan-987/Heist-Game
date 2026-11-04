using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.LevelGen
{
    public sealed class LootPlacementResult
    {
        /// <summary>
        /// For each item, a slot number: 0 to nearSlots-1 are near slots, nearSlots onward are vault slots.
        /// -1 means there was nowhere to put it.
        /// </summary>
        public int[] SlotForItem;
        public float NearValue;
        public float VaultValue;
        /// <summary>The near rooms hold at least the guaranteed value.</summary>
        public bool GuaranteeMet;
        /// <summary>Items pushed into the vault because the near rooms ran out of slots.</summary>
        public int Overflow;
        public int Unplaced;
    }

    /// <summary>
    /// Decides which loot goes in the near rooms and which in the vault (GAME_DESIGN 4.2). The guaranteed-value
    /// rule is authoritative: the nearest two thirds of the rooms hold at least 1.2 times the minimum winnable
    /// value, and the vault share is capped to make that true. Pure logic.
    /// </summary>
    public static class LootPlacement
    {
        public const float GuaranteeFactor = 1.2f;

        /// <summary>
        /// Largest share of total value the vault can hold: total = R / E, near must hold at least factor * R,
        /// so the vault holds at most 1 - factor * E. Normal (E 0.65) gives 22 percent, high (E 0.60) 28 percent.
        /// </summary>
        public static float MaxVaultShare(float efficiency, float guaranteeFactor = GuaranteeFactor)
        {
            return Mathf.Clamp01(1f - guaranteeFactor * efficiency);
        }

        /// <summary>
        /// Marks the farthest third of the non-start rooms as the vault. Rooms are ranked by distance from the
        /// start, ties broken by index, so the result is deterministic. The start room is never the vault.
        /// </summary>
        public static bool[] ClassifyVault(IList<int> distanceFromStart, int startIndex)
        {
            int count = distanceFromStart.Count;
            var isVault = new bool[count];

            var order = new List<int>();
            for (int i = 0; i < count; i++)
                if (i != startIndex) order.Add(i);
            order.Sort((a, b) =>
            {
                int c = distanceFromStart[a].CompareTo(distanceFromStart[b]);
                return c != 0 ? c : a.CompareTo(b);
            });

            int n = order.Count;
            int near = (int)Math.Ceiling(n * 2.0 / 3.0);
            for (int k = near; k < n; k++) isVault[order[k]] = true;
            return isVault;
        }

        /// <summary>
        /// Splits items between near and vault slots. The vault gets the heaviest items it may hold:
        /// at most vaultShare of the total, and never so much that the near rooms fall below the guarantee.
        /// </summary>
        public static LootPlacementResult Assign(IList<float> values, int nearSlots, int vaultSlots,
            float vaultShare, float minimumValue, float guaranteeFactor = GuaranteeFactor)
        {
            int n = values.Count;
            var result = new LootPlacementResult { SlotForItem = new int[n] };
            for (int i = 0; i < n; i++) result.SlotForItem[i] = -1;

            float total = 0f;
            for (int i = 0; i < n; i++) total += Mathf.Max(0f, values[i]);

            float required = minimumValue > 0f ? guaranteeFactor * minimumValue : 0f;
            float cap = Mathf.Min(Mathf.Clamp01(vaultShare) * total, Mathf.Max(0f, total - required));

            var order = new List<int>();
            for (int i = 0; i < n; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                int c = values[b].CompareTo(values[a]);
                return c != 0 ? c : a.CompareTo(b);
            });

            var inVault = new bool[n];
            float vault = 0f;
            int vaultCount = 0;
            foreach (int i in order)
            {
                if (vaultCount >= vaultSlots) break;
                float v = Mathf.Max(0f, values[i]);
                if (v <= 0f || vault + v > cap) continue;
                inVault[i] = true;
                vault += v;
                vaultCount++;
            }

            int nearCount = n - vaultCount;
            if (nearCount > nearSlots)
            {
                // Not enough near slots: move the cheapest near items into free vault slots, with a warning flag.
                for (int k = order.Count - 1; k >= 0 && nearCount > nearSlots && vaultCount < vaultSlots; k--)
                {
                    int i = order[k];
                    if (inVault[i]) continue;
                    inVault[i] = true;
                    vault += Mathf.Max(0f, values[i]);
                    vaultCount++;
                    nearCount--;
                    result.Overflow++;
                }
            }

            int nearCursor = 0, vaultCursor = 0;
            float near = 0f;
            for (int i = 0; i < n; i++)
            {
                if (inVault[i])
                {
                    result.SlotForItem[i] = nearSlots + vaultCursor++;
                }
                else if (nearCursor < nearSlots)
                {
                    result.SlotForItem[i] = nearCursor++;
                    near += Mathf.Max(0f, values[i]);
                }
                else
                {
                    result.Unplaced++;
                }
            }

            result.NearValue = near;
            result.VaultValue = vault;
            result.GuaranteeMet = near + 0.001f >= required;
            return result;
        }
    }
}
