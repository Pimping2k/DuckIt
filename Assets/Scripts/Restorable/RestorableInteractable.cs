using UnityEngine;
using UnityEngine.InputSystem;

namespace Restorable
{
    public class RestorableInteractable : InspectionInteractable
    {
        [SerializeField] private RestorableItem _restorableComponent;
        
        private bool _isCleaning;

        protected override void Awake()
        {
            base.Awake();
            InputService.Cleaning.MainAction.started += OnMainActionPerformed;
            InputService.Cleaning.MainAction.canceled += OnMainActionCanceled;
        }

        private void OnDestroy()
        {
            InputService.Cleaning.MainAction.started -= OnMainActionPerformed;
            InputService.Cleaning.MainAction.canceled -= OnMainActionCanceled;
        }
        
        private void OnMainActionPerformed(InputAction.CallbackContext ctx)
        {
            _isCleaning = true;
        }

        private void OnMainActionCanceled(InputAction.CallbackContext ctx)
        {
            _isCleaning = false;
        }
    }
}