using UnityEngine;

namespace Gameplay.Tools
{
    [CreateAssetMenu(menuName = "Restoration/Tool Profile")]
    public class ToolProfile : ScriptableObject
    {
        [Header("Эффект на маску (R грязь, G стар. краска, B нов. краска, A лак)")]
        public Vector4 add;
        public Vector4 sub;
        public bool gateByDirt;

        [Header("Кисть")]
        [Range(0.005f, 0.2f)] public float radius = 0.04f;
        [Range(0.005f, 0.2f)] public float minRadius = 0.01f;
        [Range(0.005f, 0.2f)] public float maxRadius = 0.12f;
        [Range(0f, 0.99f)] public float hardness = 0.6f;
        public float strength = 2.5f;
    }
}