using UnityEngine;

namespace Gameplay.Tools
{
    [CreateAssetMenu (menuName = Data.DataPaths.GAMEPLAY + "Tool Data", fileName = "TD_")]
    public class ToolData : ScriptableObject
    {
        [SerializeField] private string _toolName;
        [SerializeField] private BaseTool _prefab;
        
        public BaseTool Prefab => _prefab;
        public string ToolName => _toolName;
    }
}