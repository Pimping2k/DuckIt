using Cysharp.Threading.Tasks;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerMovement : PlayerComponent
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Transform _cameraTransform;
        
        [SerializeField] private Vector2 _xRotationRange = new(-45f, 45f);
        [SerializeField] private float _movementSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 5f;
        
        [SerializeField] private Transform _handsRoot;
        [SerializeField, Range(0f, 15f)] private float _maxHandsLag = 6f;
        [SerializeField, Min(0.1f)] private float _handsReturnSpeed = 14f;

        private IInputService _inputService;

        private Quaternion _handsInitialRotation;
        private Vector2 _previousCameraAngles;
        private Vector2 _handsRotationOffset;
        private float _handsYawOffset;
        private bool _handsInitialized;
        
        private Vector2 _movementVector;
        private Vector2 _rotationVector;
        
        public override async void Initialize()
        {
            _inputService = ServiceLocator.Resolve<IInputService>();
            await UniTask.WaitUntil(() => _inputService.IsReady);
            ToggleSubscriptions(true);            
        }

        private void OnDestroy()
        {
            ToggleSubscriptions(false);
        }

        private void Update()
        {
            MovePlayer();
            RotatePlayer();
        }
        
        private void LateUpdate()
        {
            if (!_handsRoot || !_cameraTransform)
                return;

            Vector2 cameraAngles = new Vector2(
                _cameraTransform.eulerAngles.x,
                _cameraTransform.eulerAngles.y);

            if (!_handsInitialized)
            {
                _handsInitialRotation = _handsRoot.localRotation;
                _previousCameraAngles = cameraAngles;
                _handsInitialized = true;
            }

            if (Time.deltaTime <= 0f)
                return;

            Vector2 rotationDelta = new Vector2(
                Mathf.DeltaAngle(_previousCameraAngles.x, cameraAngles.x),
                Mathf.DeltaAngle(_previousCameraAngles.y, cameraAngles.y));

            _previousCameraAngles = cameraAngles;

            _handsRotationOffset -= rotationDelta;

            _handsRotationOffset.x = Mathf.Clamp(
                _handsRotationOffset.x, -_maxHandsLag, _maxHandsLag);

            _handsRotationOffset.y = Mathf.Clamp(
                _handsRotationOffset.y, -_maxHandsLag, _maxHandsLag);

            float blend = 1f - Mathf.Exp(
                -_handsReturnSpeed * Time.deltaTime);

            _handsRotationOffset = Vector2.Lerp(
                _handsRotationOffset, Vector2.zero, blend);

            _handsRoot.localRotation =
                Quaternion.Euler(
                    _handsRotationOffset.x,
                    _handsRotationOffset.y,
                    0f) *
                _handsInitialRotation;
        }

        private void ToggleSubscriptions(bool state)
        {
            if (state)
            {
                _inputService.Player.Move.performed += OnMovePerformed;
                _inputService.Player.Move.canceled += OnMoveCanceled;
                
                _inputService.Player.Look.performed += OnLookPerformed;
            }
            else
            {
                _inputService.Player.Move.performed -= OnMovePerformed;
                _inputService.Player.Move.canceled -= OnMoveCanceled;
                
                _inputService.Player.Look.performed -= OnLookPerformed;
            }
        }

        #region Rotation

        private void RotatePlayer()
        {
            _rotationVector.y = Mathf.Clamp(_rotationVector.y, _xRotationRange.x, _xRotationRange.y);
            _rigidbody.MoveRotation(Quaternion.Euler(0f, _rotationVector.x, 0f));
            _cameraTransform.localRotation = Quaternion.Euler(_rotationVector.y, 0f, 0f);
        }
        
        private void OnLookPerformed(InputAction.CallbackContext obj)
        {
            var lookInput = obj.ReadValue<Vector2>();
            
            _rotationVector.x += lookInput.x * _rotationSpeed; 
            _rotationVector.y -= lookInput.y * _rotationSpeed;
        }
        
        #endregion
        
        #region Movement

        private void MovePlayer()
        {
            Vector3 movement = transform.rotation * new Vector3(_movementVector.x, 0f, _movementVector.y);
            _rigidbody.linearVelocity = new Vector3(movement.x * _movementSpeed, _rigidbody.linearVelocity.y, movement.z * _movementSpeed);
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            _movementVector = ctx.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            _movementVector = Vector2.zero;
        }

        #endregion
    }
}