using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Layer names from the roadmap (section 5). Create these in Project Settings > Tags and Layers
    /// with exactly these names, then set the collision matrix.
    /// </summary>
    public static class GameLayers
    {
        public const string PlayerName = "Player";
        public const string GrabbableName = "Grabbable";
        public const string HeldName = "Held";
        public const string EnemyName = "Enemy";
        public const string LevelName = "Level";
        public const string TriggerName = "Trigger";
        public const string NoClipName = "NoClip";

        public static int Player => Resolve(PlayerName);
        public static int Grabbable => Resolve(GrabbableName);
        public static int Held => Resolve(HeldName);
        public static int Enemy => Resolve(EnemyName);
        public static int Level => Resolve(LevelName);
        public static int Trigger => Resolve(TriggerName);
        public static int NoClip => Resolve(NoClipName);

        /// <summary>What a player's grab ray can hit.</summary>
        public static int GrabbableMask => 1 << Grabbable;

        /// <summary>What blocks line of sight (walls and held-through-wall checks).</summary>
        public static int SightBlockerMask => 1 << Level;

        private static int Resolve(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogError($"Layer '{layerName}' is missing. Add it in Project Settings > Tags and Layers.");
                return 0;
            }
            return layer;
        }
    }
}
