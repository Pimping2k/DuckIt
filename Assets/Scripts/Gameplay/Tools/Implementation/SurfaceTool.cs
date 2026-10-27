using MyPackage.Runtime.ServiceLocator_Core;
using Restorable;
using Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Tools.Implementation
{
    public class SurfaceTool : BaseTool
    {
        [SerializeField] protected LayerMask _paintLayer;
        [SerializeField] private Transform _rayStartPoint;
        [SerializeField] private float _breakDistance = 0.1f;

        [SerializeField] private float _hoverOffset = 0.01f;
        [SerializeField] private float _snapSpeed = 20f;
        [SerializeField] private Transform _toolVisual;
        [SerializeField] private Transform _tip;

        [SerializeField] private float _radiusStep = 0.1f;
        [SerializeField] private float _radiusBase = 0.2f;
        [SerializeField] private float _maxRadius;
        [SerializeField] private float _minRadius;

        protected SurfaceEffect Effect;
        private readonly RaycastHit[] _hits = new RaycastHit[8];
        
        private CameraService _cameraService;
        private float _radius;

        private bool _found;
        private bool _isFinding;
        private RestorableItem _currentItem;
        private RaycastHit _currentHit;

        private Vector3 _restLocalPos, _tipLocalPos;
        private Quaternion _restLocalRot;

        public float Radius => _radius;

        protected override void Awake()
        {
            base.Awake();
            _cameraService = ServiceLocator.Resolve<CameraService>();

            InputService.Cleaning.ChangeSize.performed += OnChangeSizePerformed;
            InputService.Cleaning.ToolUse.started += OnToolStarted;
            InputService.Cleaning.ToolUse.canceled += OnToolCanceled;

            _radius = _radiusBase;

            _restLocalPos = _toolVisual.localPosition;
            _restLocalRot = _toolVisual.localRotation;
            _tipLocalPos = _toolVisual.InverseTransformPoint(_tip.position);
            
            Effect = SurfaceEffect.Of(_restoreToolType);
        }

        private void OnDestroy()
        {
            InputService.Cleaning.ChangeSize.performed -= OnChangeSizePerformed;
            InputService.Cleaning.ToolUse.started -= OnToolStarted;
            InputService.Cleaning.ToolUse.canceled -= OnToolCanceled;
        }

        private void OnDisable()
        {
            _isFinding = false;
            ResetLock();
        }

        private void Update()
        {
            if (!_isFinding)
                return;

            Ray ray = _cameraService.MainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!_found)
            {
                TryLockOn(ray);
                return;
            }

            if (_currentHit.collider.Raycast(ray, out var stickHit, 100f))
            {
                _currentHit = stickHit;
                Use(ray, true, Time.deltaTime);
            }
            else if (DistanceFromRay(ray, _currentHit.point) > _breakDistance)
            {
                ResetLock();
            }
        }

        private void LateUpdate()
        {
            float t = 1f - Mathf.Exp(-_snapSpeed * Time.deltaTime);

            if (_found && _currentHit.collider != null)
            {
                Vector3 n = _currentHit.normal;
                Quaternion rot = Quaternion.LookRotation(-n, _cameraService.MainCamera.transform.up);

                Vector3 tipOffset = rot * Vector3.Scale(_tipLocalPos, _toolVisual.lossyScale);
                Vector3 pos = _currentHit.point + n * _hoverOffset - tipOffset;

                _toolVisual.SetPositionAndRotation(
                    Vector3.Lerp(_toolVisual.position, pos, t),
                    Quaternion.Slerp(_toolVisual.rotation, rot, t));
            }
            else
            {
                _toolVisual.localPosition = Vector3.Lerp(_toolVisual.localPosition, _restLocalPos, t);
                _toolVisual.localRotation = Quaternion.Slerp(_toolVisual.localRotation, _restLocalRot, t);
            }
        }

        public void SetRadius(float radius)
        {
            _radius = Mathf.Clamp(radius, _minRadius, _maxRadius);
        }

        public override void Use(Ray ray, bool pressed, float dt)
        {
            if (!pressed || _currentItem == null || _tip == null)
                return;

            var tipRay = new Ray(_tip.position - _tip.forward * 0.05f, _tip.forward);
            if (!_currentHit.collider.Raycast(tipRay, out var paintHit, 0.2f))
                return;

            OnSurfaceHit(_currentItem, paintHit, dt);
        }

        protected virtual void OnSurfaceHit(RestorableItem item, RaycastHit hit, float dt) { }

        #region Input

        private void OnChangeSizePerformed(InputAction.CallbackContext ctx)
            => SetRadius(_radius + ctx.ReadValue<Vector2>().y * _radiusStep);

        private void OnToolStarted(InputAction.CallbackContext ctx) => _isFinding = true;

        private void OnToolCanceled(InputAction.CallbackContext ctx)
        {
            _isFinding = false;
            ResetLock();
        }

        #endregion

        #region Target lock

        private void TryLockOn(Ray ray)
        {
            if (!TryFindTarget(ray, out var hit, out var item))
                return;

            _currentHit = hit;
            _currentItem = item;
            _found = true;
            IsLocked = true;
        }

        private void ResetLock()
        {
            _found = false;
            IsLocked = false;
            _currentItem = null;
            _currentHit = default;
        }

        private bool TryFindTarget(Ray ray, out RaycastHit best, out RestorableItem item)
        {
            best = default;
            item = null;

            int n = Physics.RaycastNonAlloc(ray, _hits, 100f, _paintLayer, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;

            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (h.collider is not MeshCollider || h.distance >= bestDist)
                    continue;

                var it = h.collider.GetComponentInParent<RestorableItem>();
                if (!it)
                    continue;

                best = h;
                bestDist = h.distance;
                item = it;
            }

            return item != null;
        }

        private float DistanceFromRay(Ray ray, Vector3 point)
        {
            return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
        }

        #endregion


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_rayStartPoint) return;
            Gizmos.color = new Color(1f, 0f, 0.73f);
            Vector3 start = _rayStartPoint.position;
            Gizmos.DrawLine(start, start + _rayStartPoint.forward * 100f);
        }
#endif
    }
}