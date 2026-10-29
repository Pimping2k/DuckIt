using Data;
using DG.Tweening;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using Services;
using UnityEngine;

namespace Gameplay.Tools.ToolPlacement
{
    public class InspectionPlacement : ToolPlacementBase
    {
        [SerializeField] private PlayerHandsController _handsController;
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
        private Sequence _attachSequence;
        private bool _isReady;
        private float _currentYaw;

        private Transform Hand => _handsController.RightHand;

        private void Awake()
        {
            _inspectionManager = ServiceLocator.Resolve<InspectionManager>();
            _cameraService = ServiceLocator.Resolve<CameraService>();
        }

        public override void Attach(BaseTool tool)
        {
            if (tool == null)
                return;

            _attachSequence?.Kill();
            _currentTool = tool;
            _isReady = false;

            _attachSequence = Animations.HandsAnimations.GetTool(
                Hand,
                tool.transform,
                _handsController.ToolSocket,
                new Vector3(0f, -2f, 0f),
                _handsController.RightHomePos);

            _attachSequence.OnComplete(() =>
            {
                _currentYaw = _centerSideRotation.y;
                _isReady = true;
            });
        }

        public override void Detach()
        {
            _attachSequence?.Kill();
            _isReady = false;

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

        private void Update()
        {
            if (!_isReady || _currentTool == null)
                return;

            if (_inspectionManager.IsRotating)
                return;

            if (_currentTool.IsLocked)
                return;

            var toolT = _currentTool.transform;

            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = _cameraService.MainCamera.WorldToScreenPoint(AttachPoint.position).z;
            Vector3 targetWorldPos = _cameraService.MainCamera.ScreenToWorldPoint(mouseScreenPos);

            float thirdWidth = Screen.width / 3f;
            float targetYaw = mouseScreenPos.x < thirdWidth ? _leftSideRotation.y
                            : mouseScreenPos.x <= thirdWidth * 2f ? _centerSideRotation.y
                            : _rightSideRotation.y;

            _currentYaw = Mathf.Lerp(_currentYaw, targetYaw, Time.deltaTime * 12f);

            float targetPitch = mouseScreenPos.y > Screen.height / 2f ? _topSidePitch : _bottomSidePitch;
            Quaternion targetRotation = Quaternion.Euler(targetPitch, _currentYaw, 0f);

            Vector3 newPos = Vector3.Lerp(toolT.position, targetWorldPos, Time.deltaTime * _smoothSpeed);
            Quaternion newRot = Quaternion.Slerp(toolT.rotation, targetRotation, Time.deltaTime * _rotationSpeed);

            _handsController.MoveToolTo(newPos, newRot);
        }
    }
}