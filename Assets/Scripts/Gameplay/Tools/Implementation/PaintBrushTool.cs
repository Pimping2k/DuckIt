using Restorable;
using UnityEngine;

namespace Gameplay.Tools.Implementation
{
    public class PaintBrushTool : SurfaceTool
    {
        [SerializeField] private float _strength = 2.5f;
        [SerializeField, Range(0, 1)] private float _hardness = 0.6f;

        protected override void OnSurfaceHit(RestorableItem item, RaycastHit hit, float dt)
        {
            item.Apply(hit, Effect, new Brush(Radius, _strength, _hardness), dt);
        }
    }
}