using System;
using System.Linq;
using Gameplay.Tools;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using TMPro;
using UnityEngine;

namespace UI.ToolMenu
{
    public class UIToolQuickMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text _currentToolName;
        [SerializeField] private ToolQuickMenu _quickMenu;
        
        private ToolManager _toolManager;

        private void Awake()
        {
            _toolManager = ServiceLocator.Resolve<ToolManager>();

            _toolManager.ToolChanged += OnToolChanged;
        }

        private void OnDestroy()
        {
            _toolManager.ToolChanged -= OnToolChanged;
        }

        private void OnToolChanged(BaseTool prevTool, BaseTool newTool)
        {
            var matchingToolData = _quickMenu.ToolMapData.FirstOrDefault(x => x.Id == newTool.ToolData.Id);
            _currentToolName.text = matchingToolData?.ToolName;
        }
    }
}