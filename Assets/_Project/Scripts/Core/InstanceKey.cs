using System.Runtime.CompilerServices;

namespace Game.Core
{
    /// <summary>
    /// A small unique number for any object, for use as an id in events and the threat director.
    /// Unity 6.6 made <c>Object.GetInstanceID()</c> an error and replaced it with a struct, so we keep our own.
    /// The number lasts as long as the object and is not saved.
    /// </summary>
    public static class InstanceKey
    {
        private sealed class Box
        {
            public int Value;
        }

        private static readonly ConditionalWeakTable<object, Box> Keys = new ConditionalWeakTable<object, Box>();
        private static int _next;

        /// <summary>The same object always returns the same number. Null returns 0.</summary>
        public static int Of(object owner)
        {
            if (owner == null) return 0;
            return Keys.GetValue(owner, _ => new Box { Value = ++_next }).Value;
        }
    }
}
