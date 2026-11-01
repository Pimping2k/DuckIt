using System;
using Gameplay.Tools.ToolPlacement;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using UnityEngine;

namespace Gameplay.Tools
{
    public class ToolManager : MonoBehaviour, IService
    {
        [SerializeField] private ToolPlacementBase _initialToolPlacement;
        [SerializeField] private ToolPlacementBase _debugPlacement;

        private ToolsDatabase _toolsDatabase;
        private IToolPlacement _lastPlacement;
        private PlayerHandsController _handsController;

        public bool IsLocked { get; private set; }
        public BaseTool CurrentTool { get; private set; }
        public IToolPlacement CurrentToolPlacement { get; private set; }
        
        public event Action<BaseTool> ToolChanged;

        private void Awake()
        {
            RequestChangePlacement(_initialToolPlacement);
        }
        
        public void ChangeTool(BaseTool tool)
        {
            if(IsLocked)
                return;

            CurrentToolPlacement?.Detach();
            CurrentTool = tool ? tool : null;

            if (CurrentTool)
                CurrentToolPlacement?.Attach(CurrentTool);

            ToolChanged?.Invoke(CurrentTool);
        }

        public void RequestChangePlacement<T>(T toolPlacement) where T : IToolPlacement
        {
            _lastPlacement = CurrentToolPlacement;
            CurrentToolPlacement?.Detach();
            CurrentToolPlacement = toolPlacement;
            
            if (CurrentTool)
                CurrentToolPlacement?.Attach(CurrentTool);
        }

        public void RestorePlacement()
        {
            if (_lastPlacement == null)
                return;

            RequestChangePlacement(_lastPlacement);
        }

        public void ChangeLockedState(bool state)
        {
            IsLocked = state;
        }

#if UNITY_EDITOR
        [ContextMenu("Change Placement")]
        private void ChangePlacementDebug()
        {
            RequestChangePlacement(_debugPlacement);
        }
#endif
    }
}