using System;
using System.Collections.Generic;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Tools
{
    public class ToolQuickMenu : MonoBehaviour
    {
        [SerializeField] private ToolData[] _toolMapData;
        
        private IInputService _inputService;
        private PlayerToolController _playerToolController;
        
        private BaseTool _currentTool;
        private int _currentToolIndex;

        public ToolData[] ToolMapData => _toolMapData;
        
        private void Awake()
        {
            _inputService = ServiceLocator.Resolve<IInputService>();
            
            ToggleSubscriptions(true);
        }

        private void Start()
        {
            _playerToolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();
        }

        private void OnDestroy()
        {
            ToggleSubscriptions(false);
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
            int length = _toolMapData.Length;
            _currentToolIndex = (index % length + length) % length;

            if (_currentTool != null)
            {
                Destroy(_currentTool.gameObject);
                _currentTool = null;
            }

            ToolData toolData = _toolMapData[_currentToolIndex];
            _currentTool = Instantiate(toolData.Prefab);

            _playerToolController.SetTool(_currentTool);
        }
    }
}