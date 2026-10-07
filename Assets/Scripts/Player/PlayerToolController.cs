using System;
using Gameplay.Tools;
using UnityEngine;

namespace Player
{
    public class PlayerToolController : PlayerComponent
    {
        [SerializeField] private Transform _holdTransform;

        public Transform HoldTransform => _holdTransform;
        public BaseTool CurrentTool { get; private set; }
        public event Action<BaseTool, BaseTool> ToolChanged;

        public override void Initialize()
        {
            
        }

        public void SetTool(BaseTool tool)
        {
            if (CurrentTool == tool) 
                return;

            if (CurrentTool != null) 
                CurrentTool.OnDropped();
            
            var oldTool = CurrentTool;
            var newTool = tool;
            CurrentTool = newTool;
            
            if (CurrentTool != null) 
                CurrentTool.OnPickedUp();

            ToolChanged?.Invoke(oldTool, newTool);
        }
    }
}