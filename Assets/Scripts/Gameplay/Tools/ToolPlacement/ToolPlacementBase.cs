using UnityEngine;

namespace Gameplay.Tools.ToolPlacement
{
    public abstract class ToolPlacementBase : MonoBehaviour, IToolPlacement
    {
        [SerializeField] protected Transform AttachPoint;
        
        public abstract void Attach(BaseTool tool);

        public abstract void Detach();
    }
}