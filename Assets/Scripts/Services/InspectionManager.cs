using System;
using Cysharp.Threading.Tasks;
using Data;
using DG.Tweening;
using Gameplay.Interaction;
using Gameplay.Tools;
using Gameplay.Tools.ToolPlacement;
using MyPackage.Runtime.ServiceLocator_Core;
using Restorable;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Services
{
    public class InspectionManager : MonoBehaviour, IService
    {
        [SerializeField] private Transform _snapPoint;
        [SerializeField] private CinemachineCamera _inspectionCamera;
        [SerializeField] private InspectionPlacement _inspectionPlacement;
        
        [Header("Rotation Settings")]
        [SerializeField] private float _rotationSensitivity = 0.2f;
        [SerializeField] private float _rotationSmoothness = 12f;
        
        private IInputService _inputService;
        private CameraService _cameraService;
        private ToolManager _toolManager;

        private bool _isExiting;
        private bool _isRotating;
        private bool _isReady;
        
        private Transform _initialItemParent;
        private InspectionInteractable _currentInspectionItem;

        private Quaternion _targetRotation;

        public bool IsRotating => _isRotating;
        public event Action InspectionFinished;

        private void Awake()
        {
            _toolManager = ServiceLocator.Resolve<ToolManager>();
            _inputService = ServiceLocator.Resolve<IInputService>();
            _cameraService = ServiceLocator.Resolve<CameraService>();

            _inputService.Cleaning.SecondaryAction.performed += OnSecondaryActionPerformed;
            _inputService.Cleaning.SecondaryAction.canceled += OnSecondaryActionCanceled;
            
            _inputService.Cleaning.Back.performed += OnBackPerformed;
            _inputService.Cleaning.Rotating.performed += OnRotatingPerformed;
        }

        private void OnDestroy()
        {
            _inputService.Cleaning.SecondaryAction.performed -= OnSecondaryActionPerformed;
            _inputService.Cleaning.SecondaryAction.canceled -= OnSecondaryActionCanceled;
            
            _inputService.Cleaning.Back.performed -= OnBackPerformed;
            _inputService.Cleaning.Rotating.performed -= OnRotatingPerformed;
        }

        private void Update()
        {
            SmoothRotation();
        }

        public async UniTaskVoid StartInspection(InspectionInteractable inspectableItem)
        {
            if (inspectableItem == null || _isExiting)
                return;

            _initialItemParent = inspectableItem.transform.parent;
            _currentInspectionItem = inspectableItem;
            _targetRotation = inspectableItem.transform.rotation;
            
            Animations.SnapItem(inspectableItem.transform, _snapPoint);
            
            await _cameraService.OverlapCamera(_inspectionCamera); 
            
            _inputService.ChangeMap(MapType.Cleaning);
            _toolManager.RequestChangePlacement(_inspectionPlacement);
            _isReady = true;
        }

        public async UniTaskVoid FinishInspection(InspectionInteractable inspectionInteractable)
        {
            if (_isExiting || inspectionInteractable == null)
                return;

            _isExiting = true;
            _isReady = false;

            _inputService.ChangeMap(MapType.Gameplay);
            _toolManager.RestorePlacement();
    
            await _cameraService.RemoveOverlappingCamera();
    
            if (inspectionInteractable != null)
            {
                inspectionInteractable.transform.SetParent(_initialItemParent);

                if (_initialItemParent != null)
                {
                    Animations.SnapItem(inspectionInteractable.transform, _initialItemParent).OnComplete(ResetState);
                }
                else
                {
                    ResetState();
                }
            }
            else
            {
                ResetState();
            }

            void ResetState()
            {
                _currentInspectionItem = null;
                _initialItemParent = null;
                _isExiting = false;
                InspectionFinished?.Invoke();
            }
        }

        private void OnRotatingPerformed(InputAction.CallbackContext obj)
        {
            if (_currentInspectionItem == null || _isExiting || !_isRotating)
                return;

            Vector2 mouseDelta = obj.ReadValue<Vector2>();

            if (mouseDelta != Vector2.zero)
            {
                Transform camTransform = _cameraService.Camera ? _cameraService.Camera.transform : null;

                if (camTransform)
                {
                    Quaternion yaw = Quaternion.AngleAxis(-mouseDelta.x * _rotationSensitivity, camTransform.up);
                    Quaternion pitch = Quaternion.AngleAxis(mouseDelta.y * _rotationSensitivity, camTransform.right);

                    _targetRotation = yaw * pitch * _targetRotation;
                }
            }
        }

        private void SmoothRotation()
        {
            if (_currentInspectionItem == null || _isExiting)
                return;

            _currentInspectionItem.transform.rotation = Quaternion.Slerp(
                _currentInspectionItem.transform.rotation,
                _targetRotation,
                Time.deltaTime * _rotationSmoothness
            );
        }

        private void OnSecondaryActionPerformed(InputAction.CallbackContext obj)
        {
            _isRotating = true;
        }

        private void OnSecondaryActionCanceled(InputAction.CallbackContext obj)
        {
            _isRotating = false;
        }

        private void OnBackPerformed(InputAction.CallbackContext obj)
        {
            if(!_isReady)
                return;
            
            FinishInspection(_currentInspectionItem);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            if (_snapPoint)
                Gizmos.DrawWireSphere(_snapPoint.position, 0.1f);
        }
#endif
    }
}