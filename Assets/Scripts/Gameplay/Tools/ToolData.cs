using UnityEngine;

namespace Gameplay.Tools
{
    [CreateAssetMenu (menuName = Data.DataPaths.GAMEPLAY + "Tool Data", fileName = "TD_")]
    public class ToolData : ScriptableObject
    {
        [SerializeField] private ushort _id;
        [SerializeField] private ToolType _toolType;
        [SerializeField] private string _toolName;
        [SerializeField] private BaseTool _prefab;
        
        public ushort Id => _id;
        public ToolType ToolType => _toolType;
        public BaseTool Prefab => _prefab;
        public string ToolName => _toolName;
    }
}