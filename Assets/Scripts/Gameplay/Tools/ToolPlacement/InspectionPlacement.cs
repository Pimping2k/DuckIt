using Data;
using DG.Tweening;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;

namespace Gameplay.Tools.ToolPlacement
{
    public class InspectionPlacement : ToolPlacementBase
    {
        [SerializeField] private float _smoothSpeed = 15f;
        [SerializeField] private float _rotationSpeed = 10f;
        
        [Header("Horizontal Rotation Settings (3 Zones)")]
        [SerializeField] private Vector3 _leftSideRotation = new(0f, -55f, 0f);
        [SerializeField] private Vector3 _centerSideRotation = new(0f, -90f, 0f);
        [SerializeField] private Vector3 _rightSideRotation = new(0f, -135f, 0f);
        
        [Header("Vertical Pitch Settings (2 Zones)")]
        [SerializeField] private float _topSidePitch = -30f;
        [SerializeField] private float _bottomSidePitch = 30f;
        
        private InspectionManager _inspectionManager;
        private CameraService _cameraService;

        private BaseTool _currentTool;
        private bool _isReady;
        private bool _isInspecting;
        
        private Vector3 _lastPosition;
        private float _currentYaw;

        private void Awake()
        {
            _inspectionManager = ServiceLocator.Resolve<InspectionManager>();
            _cameraService = ServiceLocator.Resolve<CameraService>();
            
            _inspectionManager.RotationStarted += OnInspectionStarted;
            _inspectionManager.RotationFinished += OnInspectionFinished;
        }

        private void OnDestroy()
        {
            _inspectionManager.RotationStarted -= OnInspectionStarted;
            _inspectionManager.RotationFinished -= OnInspectionFinished;
        }

        public override void Attach(BaseTool tool)
        {
            _currentTool = tool;
            _isReady = false;
            
            Animations.SnapTool(_currentTool.transform, AttachPoint).OnComplete(() =>
            {
                _isReady = true;
                _currentYaw = _centerSideRotation.y;
            });
        }

        public override void Detach()
        {
            if (_currentTool != null)
            {
                _currentTool.transform.SetParent(null);
            }
            _currentTool = null;
            _isReady = false;
            _isInspecting = false;
        }

        private void Update()
        {
            if (!_isReady || _currentTool == null || _isInspecting)
                return;
    
            if (_inspectionManager.IsRotating)
                return;
            
            if(_currentTool.IsLocked)
                return;
            
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = _cameraService.MainCamera.WorldToScreenPoint(AttachPoint.position).z;
            Vector3 targetWorldPos = _cameraService.MainCamera.ScreenToWorldPoint(mouseScreenPos);

            _currentTool.transform.position = Vector3.Lerp(
                _currentTool.transform.position, 
                targetWorldPos, 
                Time.deltaTime * _smoothSpeed
            );

            float thirdWidth = Screen.width / 3f;
            float targetYaw;

            if (mouseScreenPos.x < thirdWidth)
            {
                targetYaw = _leftSideRotation.y;
            }
            else if (mouseScreenPos.x <= thirdWidth * 2f)
            {
                targetYaw = _centerSideRotation.y;
            }
            else
            {
                targetYaw = _rightSideRotation.y;
            }

            _currentYaw = Mathf.Lerp(_currentYaw, targetYaw, Time.deltaTime * 12f);

            bool isTopSide = mouseScreenPos.y > Screen.height / 2f;
            float targetPitch = isTopSide ? _topSidePitch : _bottomSidePitch;

            Quaternion targetRotation = Quaternion.Euler(targetPitch, _currentYaw, 0f);

            _currentTool.transform.rotation = Quaternion.Slerp(
                _currentTool.transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
        
        private void OnInspectionStarted()
        {
            // if (!_currentTool) 
            //     return;
            //
            // _lastPosition = _currentTool.transform.position;
            //
            // _isInspecting = true;
            // _isReady = false;
            //
            // Animations.SnapTool(_currentTool.transform, _idlePlace, 0.1f);
        }

        private void OnInspectionFinished()
        {
            if (_currentTool == null) return;

            _isInspecting = true;
            _isReady = false;

            _currentTool.transform.DOKill();
            _currentTool.transform.SetParent(AttachPoint, true);

            DOTween.Sequence()
                .SetTarget(_currentTool.transform)
                .SetLink(_currentTool.gameObject)
                .Append(_currentTool.transform.DOMove(_lastPosition, 0.2f).SetEase(Ease.OutCubic))
                .Join(_currentTool.transform.DORotateQuaternion(AttachPoint.rotation, 0.2f).SetEase(Ease.OutSine))
                .OnComplete(() =>
                {
                    _isInspecting = false;
                    _isReady = true;
                    _currentYaw = _centerSideRotation.y;
                });
        }
    }
}