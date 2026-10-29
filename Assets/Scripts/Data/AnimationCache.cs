using UnityEngine;

namespace Data
{
    public static class AnimationCache
    {
        public static class PlayerHands
        {
            public static int ACTION_NONE = Animator.StringToHash("Hand_Grip");
            public static int GRIP_PARAMETER = Animator.StringToHash("Grip");
            public static int HOLD_NOTE_ACTION = Animator.StringToHash("HoldNote");
        }

        public static class Box
        {
            public static int CLOSED_ACTION = Animator.StringToHash("Closed");
            public static int TEAR_ACTION = Animator.StringToHash("Tear");
            public static int OPEN_ACTION = Animator.StringToHash("Open");
            public static int OPEN_TRIGGER = Animator.StringToHash("OpenBox");
        }
    }
}