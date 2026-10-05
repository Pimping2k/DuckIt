using System;
using Gameplay.Tools;
using UnityEngine;

namespace Player
{
    public class PlayerToolController : PlayerComponent
    {
        public BaseTool CurrentTool { get; private set; }
        public event Action<BaseTool> ToolChanged;

        public override void Initialize()
        {
            
        }

        public void SetTool(BaseTool tool)
        {
            if (CurrentTool == tool) return;

            if (CurrentTool != null) CurrentTool.OnDropped();
            CurrentTool = tool;
            if (CurrentTool != null) CurrentTool.OnPickedUp();

            ToolChanged?.Invoke(tool);
        }
    }
}