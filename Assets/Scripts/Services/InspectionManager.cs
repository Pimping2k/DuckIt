using System;
using Data;
using DG.Tweening;
using Gameplay.Interaction;
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
        [SerializeField] private float _rotationSensitivity = 0.4f;
        
        private IInputService _inputService;
        private CameraService _cameraService;

        private bool _isExiting;
        private bool _isRotating;
        
        private Transform _initialItemParent;
        private InspectionInteractable _currentInspectionItem;
        
        private void Awake()
        {
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

        public void StartInspection(InspectionInteractable inspectableItem)
        {
            _initialItemParent = _initialItemParent ? inspectableItem.transform.parent : inspectableItem.transform;
            
            _currentInspectionItem = inspectableItem;
            
            Animations.SnapItem(inspectableItem.transform, _snapPoint).OnComplete(() =>
            {
                _inputService.ChangeMap(MapType.Cleaning);
                _cameraService.OverlapCamera(_inspectionCamera);
            });
        }

        public void FinishInspection(InspectionInteractable inspectionInteractable)
        {
            _isExiting = true;
            _inputService.ChangeMap(MapType.Gameplay);
            _cameraService.RemoveOverlappingCamera();
            
            Animations.SnapItem(inspectionInteractable.transform, _initialItemParent).OnComplete(() =>
            {
                _currentInspectionItem = null;
                _isExiting = false;
            });
        }

        private void OnRotatingPerformed(InputAction.CallbackContext obj)
        {
            if(_currentInspectionItem == null || _isExiting)
                return;

            if (!_isRotating)
                return;
            
            Vector2 mouseDelta = obj.ReadValue<Vector2>();

            if (!mouseDelta.Equals(Vector2.zero))
            {
                Transform itemTransform = _currentInspectionItem.transform;
                Transform camTransform = _cameraService.Camera.transform;

                if (camTransform)
                {
                    itemTransform.Rotate(camTransform.up, -mouseDelta.x * _rotationSensitivity, Space.World);
                    itemTransform.Rotate(camTransform.right, mouseDelta.y * _rotationSensitivity, Space.World);
                }
            }
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
            FinishInspection(_currentInspectionItem);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            if(_snapPoint)
                Gizmos.DrawWireSphere(_snapPoint.position, 0.1f);
        }
#endif
    }
}