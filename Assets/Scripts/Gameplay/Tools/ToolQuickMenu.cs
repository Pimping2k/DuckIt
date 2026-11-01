using System;
using System.Linq;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Tools
{
    public class ToolQuickMenu : MonoBehaviour
    {
        [SerializeField] private ToolData[] _toolMapData;
        
        private ToolManager _toolManager;
        private IInputService _inputService;
        private ToolsDatabase _toolsDatabase;
        
        private BaseTool _currentTool;
        private int _currentToolIndex;

        public ToolData[] ToolMapData => _toolMapData;
        
        private void Awake()
        {
            _toolsDatabase = ServiceLocator.ResolveConfig<ToolsDatabase>();
            _inputService = ServiceLocator.Resolve<IInputService>();
            _toolManager = ServiceLocator.Resolve<ToolManager>();
            
            ToggleSubscriptions(true);

            foreach (var (key,toolData) in _toolsDatabase.Data)
            {
                _toolMapData[key-1] = toolData;
            }
        }

        private void Start()
        {
            SetInitialTool();
        }

        private void OnDestroy()
        {
            ToggleSubscriptions(false);
        }
        
        private void SetInitialTool()
        {
            var initialTool = Array.IndexOf(_toolMapData,_toolMapData.FirstOrDefault(x => x.ToolType == ToolType.Empty));
            CreateTool(initialTool);
        }
        
        private void ToggleSubscriptions(bool state)
        {
            if (state)
            {
                _inputService.General.JumpRight.performed += OnJumpRightPerformed;
                _inputService.General.JumpLeft.performed += OnJumpLeftPerformed;
            }
            else
            {
                _inputService.General.JumpRight.performed -= OnJumpRightPerformed;
                _inputService.General.JumpLeft.performed -= OnJumpLeftPerformed;
            }
        }

        private void OnJumpRightPerformed(InputAction.CallbackContext ctx)
        {
            CreateTool(_currentToolIndex + 1);
        }

        private void OnJumpLeftPerformed(InputAction.CallbackContext ctx)
        {
            CreateTool(_currentToolIndex - 1);
        }

        private void CreateTool(int index)
        {
            if(_toolManager.IsLocked)
                return;
            
            int length = _toolMapData.Length;
            _currentToolIndex = (index % length + length) % length;

            if (_currentTool != null)
            {
                Destroy(_currentTool.gameObject);
                _currentTool = null;
            }

            ToolData toolData = _toolMapData[_currentToolIndex];
            if(toolData.Prefab)
                _currentTool = Instantiate(toolData.Prefab);

            _toolManager.ChangeTool(_currentTool);
        }
    }
}