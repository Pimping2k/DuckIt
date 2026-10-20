using System;
using System.Collections;
using Restorable;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Tools
{
    public class SurfaceTool : BaseTool
    {
        [SerializeField] protected ToolProfile _profile;
        [SerializeField] protected LayerMask _paintLayer;
        [SerializeField] private Transform _rayStartPoint; 
            
        protected float _radius;
        private readonly RaycastHit[] _hits = new RaycastHit[8];
        
        public float Radius => _radius;

        private Coroutine _useRoutine;

        protected override void Awake()
        {
            base.Awake();
            _radius = _profile.radius;

            InputService.Cleaning.ToolUse.started  += OnToolStarted;
            InputService.Cleaning.ToolUse.canceled += OnToolCanceled;
        }

        private void OnDestroy()
        {
            InputService.Cleaning.ToolUse.started  -= OnToolStarted;
            InputService.Cleaning.ToolUse.canceled -= OnToolCanceled;
        }

        private void OnDisable()
        {
            StopUse();
        }

        private void OnToolStarted(InputAction.CallbackContext ctx)
        {
            if (_useRoutine == null) 
                _useRoutine = StartCoroutine(UseLoop());
        }

        private void OnToolCanceled(InputAction.CallbackContext ctx)
        {
            StopUse();
        }

        private void StopUse()
        {
            if (_useRoutine == null) return;
            StopCoroutine(_useRoutine);
            _useRoutine = null;
        }

        private IEnumerator UseLoop()
        {
            while (true)
            {
                var ray = new Ray(_rayStartPoint.position, _rayStartPoint.forward);
                Use(ray, true, Time.deltaTime);
                yield return null;
            }
        }


        public override void Use(Ray ray, bool pressed, float dt)
        {
            if (!pressed) return;

            int n = Physics.RaycastNonAlloc(ray, _hits, 100f, _paintLayer, QueryTriggerInteraction.Ignore);
            RaycastHit best = default;
            RestorableItem item = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (h.collider is not MeshCollider || h.distance >= bestDist) 
                    continue;

                var it = h.collider.GetComponentInParent<RestorableItem>();
                if (!it)
                    continue;
                best = h; bestDist = h.distance; item = it;
            }

            if (item == null) return;

            item.Stroke(item.GetUV(best), _radius, _profile.hardness, _profile.strength * dt,
                _profile.wearSub, _profile.paintAdd, _profile.gateByWear,
                _profile.wetAdd, _profile.needWet, _profile.peel);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0.73f);
            if (_rayStartPoint)
            {
                Vector3 start = _rayStartPoint.position;
                Vector3 end = start + _rayStartPoint.forward * 100f;
                Gizmos.DrawLine(start, end);
            }
        }
#endif
    }
}
