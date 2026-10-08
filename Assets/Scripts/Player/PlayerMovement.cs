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
        
        private IInputService _inputService;
        
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