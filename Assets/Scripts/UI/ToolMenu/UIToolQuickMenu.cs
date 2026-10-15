using System;
using System.Linq;
using Gameplay.Tools;
using Player;
using TMPro;
using UnityEngine;

namespace UI.ToolMenu
{
    public class UIToolQuickMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text _currentToolName;
        [SerializeField] private ToolQuickMenu _quickMenu;
        
        private PlayerToolController _toolController;

        private void Start()
        {
            _toolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();
            
            _toolController.ToolChanged += OnToolChanged;
        }

        private void OnDestroy()
        {
            _toolController.ToolChanged -= OnToolChanged;
        }

        private void OnToolChanged(BaseTool prevTool, BaseTool newTool)
        {
            var matchingToolData = _quickMenu.ToolMapData.FirstOrDefault(x => x.Prefab == newTool);
            _currentToolName.text = matchingToolData?.ToolName;
        }
    }
}