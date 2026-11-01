using Data;
using Gameplay.Tools.Implementation;
using DG.Tweening;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using Restorable;
using Services;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Tools.ToolPlacement
{
    public class InspectionPlacement : ToolPlacementBase
    {
        [SerializeField] private PlayerHandsController _handsController;
        [SerializeField] private AimReticle _reticle;

        [Header("Aim")]
        [SerializeField] private LayerMask _aimMask = ~0;
        [SerializeField] private float _maxDistance = 5f;
        [SerializeField] private bool _useMousePosition = true;
        [SerializeField] private float _hoverHeight = 0.05f;

        [Header("Follow")]
        [SerializeField] private float _smoothSpeed = 15f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private Vector3 _toolRotationOffset;

        private InspectionManager _inspectionManager;
        private CameraService _cameraService;
        private IInputService _inputService;

        private BaseTool _currentTool;
        private Sequence _attachSequence;
        private bool _isReady;
        private bool _isPressed;
        private float _aimDepth;

        private Collider _lastCollider;
        private bool _lastColliderIsItem;

        private Transform Hand => _handsController.RightHand;

        public bool IsWorking { get; private set; }
        public event Action<bool> WorkingChanged;

        public float AimMaxDistance => _maxDistance;

        public Ray GetAimRay()
        {
            var camera = _cameraService.MainCamera;
            return _useMousePosition && Mouse.current != null
                ? camera.ScreenPointToRay(Mouse.current.position.ReadValue())
                : camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }


        private void Awake()
        {
            _inspectionManager = ServiceLocator.Resolve<InspectionManager>();
            _cameraService = ServiceLocator.Resolve<CameraService>();
            _inputService = ServiceLocator.Resolve<IInputService>();

            _inputService.Cleaning.MainAction.started += OnMainStarted;
            _inputService.Cleaning.MainAction.canceled += OnMainCanceled;
        }

        private void OnDestroy()
        {
            _inputService.Cleaning.MainAction.started -= OnMainStarted;
            _inputService.Cleaning.MainAction.canceled -= OnMainCanceled;
        }

        public override void Attach(BaseTool tool)
        {
            if (tool == null)
                return;

            _attachSequence?.Kill();
            _currentTool = tool;
            _aimDepth = 0f;
            _isReady = false;

            _attachSequence = Animations.HandsAnimations.GetTool(
                Hand,
                tool.transform,
                _handsController.ToolSocket,
                new Vector3(0f, -2f, 0f),
                _handsController.RightHomePos);

            _attachSequence.OnComplete(() => _isReady = true);
        }

        public override void Detach()
        {
            _attachSequence?.Kill();
            _isReady = false;
            SetWorking(false);
            if (_reticle) _reticle.Hide();

            if (_currentTool != null)
            {
                _currentTool.transform.SetParent(null);
                _currentTool = null;
            }

            Hand.DOKill();
            Hand.DOLocalMove(_handsController.RightHomePos, 0.25f)
                .SetEase(Ease.OutCubic).SetLink(Hand.gameObject);
            Hand.DOLocalRotateQuaternion(_handsController.RightHomeRot, 0.25f)
                .SetEase(Ease.OutCubic).SetLink(Hand.gameObject);
        }

        #region Control

        private void OnMainStarted(InputAction.CallbackContext ctx) => _isPressed = true;
        private void OnMainCanceled(InputAction.CallbackContext ctx) => _isPressed = false;

        private void Update()
        {
            if (!_isReady || _currentTool == null || _inspectionManager.IsRotating)
            {
                SetWorking(false);
                if (_reticle) _reticle.Hide();
                return;
            }

            var cam = _cameraService.MainCamera;
            Ray ray = GetAimRay();

            bool hasHit = Physics.Raycast(ray, out var hit, _maxDistance, _aimMask, QueryTriggerInteraction.Ignore);
            bool onItem = hasHit && IsItem(hit.collider);

            if (_reticle)
            {
                if (onItem) 
                    _reticle.Show(hit.point, hit.normal);
                else 
                    _reticle.Hide();
            }

            if (_currentTool.IsLocked)
            {
                SetWorking(false);
                return;
            }

            SetWorking(_isPressed && onItem);

            Vector3 aimPoint;
            Vector3 aimNormal;
            if (hasHit)
            {
                aimPoint = hit.point;
                aimNormal = hit.normal;
                _aimDepth = Mathf.Max(cam.nearClipPlane + 0.1f,
                    Vector3.Dot(hit.point - cam.transform.position, cam.transform.forward));
            }
            else
            {
                aimPoint = GetFreeAimPosition(ray, cam);
                aimNormal = -cam.transform.forward;
            }

            if (_currentTool is SurfaceTool surfaceTool &&
                surfaceTool.FollowAim(aimPoint, aimNormal, hasHit ? _hoverHeight : 0f,
                    _smoothSpeed, _rotationSpeed, Time.deltaTime))
                return;

            Vector3 targetPos = aimPoint + (hasHit ? aimNormal * _hoverHeight : Vector3.zero);

            Quaternion targetRot = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f)
                                   * Quaternion.Euler(_toolRotationOffset);

            var toolT = _currentTool.transform;
            Vector3 newPos = Vector3.Lerp(toolT.position, targetPos, Time.deltaTime * _smoothSpeed);
            Quaternion newRot = Quaternion.Slerp(toolT.rotation, targetRot, Time.deltaTime * _rotationSpeed);

            _handsController.MoveToolTo(newPos, newRot);
        }

        private Vector3 GetFreeAimPosition(Ray ray, Camera camera)
        {
            if (_aimDepth <= 0f)
            {
                _aimDepth = Mathf.Max(0.5f, camera.nearClipPlane + 0.1f,
                    Vector3.Dot(AttachPoint.position - camera.transform.position, camera.transform.forward));
            }

            var plane = new Plane(camera.transform.forward,
                camera.transform.position + camera.transform.forward * _aimDepth);
            return plane.Raycast(ray, out float distance)
                ? ray.GetPoint(distance)
                : ray.GetPoint(_aimDepth);
        }

        private bool IsItem(Collider col)
        {
            if (col != _lastCollider)
            {
                _lastCollider = col;
                _lastColliderIsItem = col.GetComponentInParent<RestorableItem>();
            }
            return _lastColliderIsItem;
        }

        private void SetWorking(bool state)
        {
            if (IsWorking == state) 
                return;
            
            IsWorking = state;
            WorkingChanged?.Invoke(state);
        }

        #endregion
    }
}