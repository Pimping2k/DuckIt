using UnityEngine;

namespace Data
{
    public static class AnimationCache
    {
        public static class PlayerHands
        {
            public static int GRIP_PARAMETER = Animator.StringToHash("Grip");
        }

        public static class Box
        {
            public static int CLOSED_ACTION = Animator.StringToHash("Closed");
            public static int TEAR_ACTION = Animator.StringToHash("Tear");
            public static int OPEN_ACTION = Animator.StringToHash("Open");
        }
    }
}