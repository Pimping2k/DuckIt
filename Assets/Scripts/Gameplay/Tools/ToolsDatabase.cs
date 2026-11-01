using AYellowpaper.SerializedCollections;
using Data;
using UnityEngine;

namespace Gameplay.Tools
{
    [CreateAssetMenu(fileName = "Tools_Database", menuName = DataPaths.GAMEPLAY + "ToolsDatabase")]
    public class ToolsDatabase : IdDatabase<ToolData>
    {
        [SerializeField] private SerializedDictionary<ushort, ToolData> _toolsMap;
        [SerializeField] private ToolData _initialTool;
        
        public override SerializedDictionary<ushort, ToolData> Data => _toolsMap;
        public ToolData InitialTool => _initialTool;
    }
}