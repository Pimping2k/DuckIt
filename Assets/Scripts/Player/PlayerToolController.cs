using System;
using Gameplay.Tools;
using Gameplay.Tools.ToolPlacement;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;

namespace Player
{
    public class PlayerToolController : PlayerComponent
    {
        [SerializeField] private HandPlacement _handPlacement;

        private ToolManager _toolManager;
        private InspectionManager _inspectionManager;

        public HandPlacement HandPlacement => _handPlacement;
        
        private void Awake()
        {
            _toolManager = ServiceLocator.Resolve<ToolManager>();
            _inspectionManager = ServiceLocator.Resolve<InspectionManager>();
            
            _inspectionManager.InspectionFinished += OnInspectionFinished;
        }

        private void OnDestroy()
        {
            _inspectionManager.InspectionFinished -= OnInspectionFinished;
        }

        public override void Initialize() { }

        private void OnInspectionFinished()
        {
            _toolManager.RequestChangePlacement(_handPlacement);
        }
    }
}