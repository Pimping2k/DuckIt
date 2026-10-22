using Restorable;
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
        [Header("Влага и снятие покрытия")] [Min(0f), Tooltip("Сколько влаги оставляет инструмент: для губки около 3")]
        public float wetAdd;
        [Tooltip("Каким слоям нужна влага: грязь = (0,1,0,0)")]
        public Vector4 needWet;
        [Tooltip("Снимать покрытие только с открытого края (пинцет)")]
        public bool peel;
        [Header("Быстрая настройка")] 
        [SerializeField, Tooltip("Выбери и вызови в меню компонента «Apply preset»")]
        private RestoreTool _preset;

        public SurfaceEffect Effect => new SurfaceEffect
        {
            wearSub = wearSub,
            paintAdd = paintAdd,
            gateByWear = gateByWear,
            wetAdd = wetAdd,
            needWet = needWet,
            peel = peel
        };

        [ContextMenu("Apply preset")]
        private void ApplyPreset()
        {
            var e = SurfaceEffect.Of(_preset);
            wearSub = e.wearSub;
            paintAdd = e.paintAdd;
            gateByWear = e.gateByWear;
            wetAdd = e.wetAdd;
            needWet = e.needWet;
            peel = e.peel;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
