using Restorable;
using UnityEngine;

namespace Gameplay.Tools
{
    public class SurfaceTool : BaseTool
    {
        [SerializeField] protected ToolProfile _profile;
        [SerializeField] protected LayerMask _paintLayer;
        protected float _radius;
        public float Radius => _radius;

        protected override void Awake()
        {
            base.Awake();
            _radius = _profile.radius;
        }

        public override void Use(Ray ray, bool pressed, float dt)
        {
            if (!pressed)
                return;
            
            if (!Physics.Raycast(ray, out var hit, 100f, _paintLayer,QueryTriggerInteraction.Ignore))
                return;

            var item = hit.collider.GetComponentInParent<RestorableItem>();
            if (item == null)
                return;

            item.Stroke(item.GetUV(hit), _radius, _profile.hardness, _profile.strength * dt,
                _profile.wearSub, _profile.paintAdd, _profile.gateByWear);
        }
    }
}