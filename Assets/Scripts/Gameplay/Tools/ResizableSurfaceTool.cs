using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Tools
{
    public class ResizableSurfaceTool : SurfaceTool
    {
        [SerializeField] private float _scrollStep = 0.01f;
        
        public event Action<float> RadiusChanged;

        protected override void Awake()
        {
            base.Awake();
            InputService.Player.ChangeSize.performed += OnChangeSizePerformed;
        }

        private void OnDestroy()
        {
            InputService.Player.ChangeSize.performed -= OnChangeSizePerformed;
        }

        private void OnChangeSizePerformed(InputAction.CallbackContext ctx)
        {
            var size = ctx.ReadValue<Vector2>();

            OnScroll(size.sqrMagnitude);
        }

        public void OnScroll(float delta)
        {
            _radius = Mathf.Clamp(_radius + delta * _scrollStep, _profile.minRadius, _profile.maxRadius);
            RadiusChanged?.Invoke(_radius);
            Debug.Log("Radius: " + _radius);
        }
    }
}