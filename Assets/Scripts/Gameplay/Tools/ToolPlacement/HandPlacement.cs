using Data;

namespace Gameplay.Tools.ToolPlacement
{
    public class HandPlacement : ToolPlacementBase
    {
        private BaseTool _currentTool;
        
        public override void Attach(BaseTool tool)
        {
            if(tool == null)
                return;
            
            _currentTool = tool;
            Animations.SnapTool(_currentTool.transform, AttachPoint);
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