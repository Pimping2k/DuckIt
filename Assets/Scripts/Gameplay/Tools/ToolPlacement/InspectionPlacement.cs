using System;
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
        [SerializeField] private Vector3 _leftSideRotation = new Vector3(0f, -55f, 0f);
        [SerializeField] private Vector3 _rightSideRotation = new Vector3(0f, -135f, 0f);
        [SerializeField] private float _topSidePitch = -30f;
        [SerializeField] private float _bottomSidePitch = 30f;
        
        private InspectionManager _inspectionManager;
        private CameraService _cameraService;
        
        private BaseTool _currentTool;
        private bool _isReady;

        private void Awake()
        {
            _inspectionManager = ServiceLocator.Resolve<InspectionManager>();
            _cameraService = ServiceLocator.Resolve<CameraService>();
        }

        public override void Attach(BaseTool tool)
        {
            _currentTool = tool;
            Animations.SnapTool(_currentTool.transform, AttachPoint).OnComplete(() =>
            {
                _isReady = true;
            });
        }

        public override void Detach()
        {
            _currentTool.transform.SetParent(null);
            _currentTool = null;
            _isReady = false;
        }

        private void Update()
        {
            if (!_isReady || _currentTool == null)
                return;
    
            if (_inspectionManager.IsRotating)
                return;
    
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = _cameraService.MainCamera.WorldToScreenPoint(AttachPoint.position).z;
            Vector3 targetWorldPos = _cameraService.MainCamera.ScreenToWorldPoint(mouseScreenPos);

            _currentTool.transform.position = Vector3.Lerp(
                _currentTool.transform.position, 
                targetWorldPos, 
                Time.deltaTime * _smoothSpeed
            );

            bool isRightSide = mouseScreenPos.x > Screen.width / 2f;
            bool isTopSide = mouseScreenPos.y > Screen.height / 2f;

            Vector3 targetEuler = isRightSide ? _rightSideRotation : _leftSideRotation;

            targetEuler.x = isTopSide ? _topSidePitch : _bottomSidePitch;

            Quaternion targetRotation = Quaternion.Euler(targetEuler);

            _currentTool.transform.rotation = Quaternion.Slerp(
                _currentTool.transform.rotation,
                targetRotation,
                Time.deltaTime * _rotationSpeed
            );
        }
    }
}