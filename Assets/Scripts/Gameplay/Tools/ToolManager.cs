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
        
        private IToolPlacement _lastPlacement;
        
        public BaseTool CurrentTool { get; private set; }
        public IToolPlacement CurrentToolPlacement { get; private set; }
        
        public event Action<BaseTool, BaseTool> ToolChanged;

        private void Awake()
        {
            RequestChangePlacement(_initialToolPlacement);
        }

        public void ChangeTool(BaseTool tool)
        {
            if (CurrentTool == tool)
                return;

            var oldTool = CurrentTool;

            CurrentToolPlacement?.Detach();
            CurrentTool = tool;

            if (CurrentTool)
                CurrentToolPlacement?.Attach(CurrentTool);

            ToolChanged?.Invoke(oldTool, CurrentTool);
        }

        public void RequestChangePlacement<T>(T toolPlacement) where T : IToolPlacement
        {
            _lastPlacement = CurrentToolPlacement;
            CurrentToolPlacement?.Detach();
            CurrentToolPlacement = toolPlacement;
            CurrentToolPlacement.Attach(CurrentTool);
        }

        public void RestorePlacement()
        {
            if (_lastPlacement == null)
                return;

            RequestChangePlacement(_lastPlacement);
        }
    }
}