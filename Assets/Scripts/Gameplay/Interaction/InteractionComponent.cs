using System;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Interaction
{
    public class InteractionComponent : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private LayerMask _interactLayer;

        private IInputService _inputService;
        
        private RaycastHit[] _hits = new RaycastHit[10];
        private IInteractable _lastInteractable;

        private void Awake()
        {
            _inputService = ServiceLocator.Resolve<IInputService>();

            ToggleSubscription(true);
        }

        private void OnDestroy()
        {
            ToggleSubscription(false);
        }

        private void Update()
        {
            TryFindInteractable();
        }

        private void ToggleSubscription(bool state)
        {
            if (state)
            {
                _inputService.Player.Interact.performed += OnInteractPerformed;
            }
            else
            {
                _inputService.Player.Interact.performed -= OnInteractPerformed;
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            _lastInteractable?.Interact();
        }

        private void TryFindInteractable()
        {
            _lastInteractable = null;

            var ray = new Ray(_startPoint.position, _startPoint.forward);
            var size = Physics.RaycastNonAlloc(ray, _hits, Mathf.Infinity, _interactLayer);

            for (var i = 0; i < size; i++)
            {
                if (!_hits[i].collider)
                    continue;

                if (!_hits[i].collider.TryGetComponent(out IInteractable interactable))
                    continue;

                _lastInteractable = interactable;
                break;
            }
        }
    }
}