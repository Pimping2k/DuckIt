using UnityEngine;

namespace Gameplay.Tools
{
    [CreateAssetMenu(menuName = "Restoration/Tool Profile")]
    public class ToolProfile : ScriptableObject
    {
        [Header("Снимает износ: R пыль, G грязь, B старая краска, A ржавчина (0..1)")]
        public Vector4 wearSub;
        [Header("Наносит: R новая краска, G лак (0..1)")]
        public Vector2 paintAdd;
        public bool gateByWear;

        [Header("Влага и снятие покрытия")]
        [Min(0f), Tooltip("Сколько влаги оставляет инструмент: для губки около 3")]
        public float wetAdd;
        [Tooltip("Каким слоям нужна влага: грязь = (0,1,0,0)")]
        public Vector4 needWet;
        [Tooltip("Снимать покрытие только с открытого края (пинцет)")]
        public bool peel;

        [Header("Brushes")]
        [Range(0.005f, 0.2f)] public float radius = 0.04f;
        [Range(0.005f, 0.2f)] public float minRadius = 0.01f;
        [Range(0.005f, 0.2f)] public float maxRadius = 0.12f;
        [Range(0f, 0.99f)] public float hardness = 0.6f;
        public float strength = 2.5f;
    }
}
