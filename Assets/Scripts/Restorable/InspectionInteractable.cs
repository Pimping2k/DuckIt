using Gameplay.Interaction;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine.InputSystem;

namespace Restorable
{
    public class InspectionInteractable : Interactable
    {
        protected IInputService InputService;
        private InspectionManager _inspectionManager;
        
        protected override void Awake()
        {
            InputService = ServiceLocator.Resolve<IInputService>();
            _inspectionManager = ServiceLocator.Resolve<InspectionManager>();
        }

        protected override void OnInteracted()
        {
            _inspectionManager.StartInspection(this);
        }
    }
}