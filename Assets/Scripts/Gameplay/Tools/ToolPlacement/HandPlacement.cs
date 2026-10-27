using Data;
using Player;
using UnityEngine;

namespace Gameplay.Tools.ToolPlacement
{
    public class HandPlacement : ToolPlacementBase
    {
        [SerializeField] private PlayerHandsController _handsController;
        
        private BaseTool _currentTool;
        
        public override void Attach(BaseTool tool)
        {
            if(tool == null)
                return;
            
            _currentTool = tool;
            Animations.HandsAnimations.GetTool(
                _handsController.RightHand,
                tool.transform,
                _handsController.ToolSocket,
                new Vector3(0f, -2f, 0f), _handsController.RightHomePos);
        }

        public override void Detach()
        {
            if(_currentTool == null)
                return;
            
            _currentTool.transform.SetParent(null);
            _currentTool = null;
        }
    }
}