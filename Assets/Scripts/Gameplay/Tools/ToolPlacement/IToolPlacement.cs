using UnityEngine;

namespace Gameplay.Tools.ToolPlacement
{
    public interface IToolPlacement
    {
        public void Attach(BaseTool tool);
        public void Detach();
    }
}