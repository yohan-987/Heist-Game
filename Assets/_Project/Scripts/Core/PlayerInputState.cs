using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One frame of player intent. Gameplay reads this and never touches an input API directly,
    /// which is what lets a network client send the same struct to the host.
    /// </summary>
    public struct PlayerInputState
    {
        public Vector2 Move;          // x = strafe, y = forward
        public Vector2 Look;          // delta this frame, already scaled for sensitivity by the source
        public bool Sprint;           // held
        public bool Crouch;           // held
        public bool JumpPressed;      // edge, true for one frame
        public bool GrabHeld;         // held
        public bool GrabPressed;      // edge
        public bool ThrowPressed;     // edge
        public bool RotateHeld;       // held: scroll rotates instead of changing distance
        public float Scroll;          // wheel delta this frame

        public static PlayerInputState None => default;
    }

    public interface IPlayerInputSource
    {
        /// <summary>Latest state. Edge flags are cleared by the source after the frame they were read.</summary>
        PlayerInputState Current { get; }
    }
}
