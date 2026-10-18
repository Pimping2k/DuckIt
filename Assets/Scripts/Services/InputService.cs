using System;
using UnityEngine;

namespace Services
{
    public class InputService : MonoBehaviour, IInputService
    {
        public MapType MapType { get; private set; }
        public CursorLockMode CursorMode { get; private set; } = CursorLockMode.Locked;
        public InputActions InputSystem { get; private set; }
        public InputActions.UIActions UI { get; private set; }
        public InputActions.PlayerActions Player { get; private set; }
        public InputActions.GeneralActions General { get; private set; }
        public InputActions.CleaningActions Cleaning { get; private set; }
        public event Action MapChanged;
        public bool IsReady { get; private set; }

        private void Awake()
        {
            InputSystem = new InputActions();
            Cursor.lockState = CursorMode;
            
            UI = InputSystem.UI;
            Player = InputSystem.Player;
            General = InputSystem.General;
            Cleaning = InputSystem.Cleaning;
            
            IsReady = true;
            ChangeMap(MapType.Gameplay);
            General.Enable();
        }

        private void OnDestroy()
        {
            InputSystem.Disable();
            InputSystem.Dispose();
        }

        public void ChangeMap(MapType mapType)
        {
            InputSystem.Cleaning.Disable();
            InputSystem.Player.Disable();
            InputSystem.UI.Disable();
            MapType = mapType;
            
            switch (MapType)
            {
                case MapType.Gameplay:
                    InputSystem.Player.Enable();
                    CursorMode = CursorLockMode.Locked;
                    break;
            
                case MapType.Cleaning:
                    InputSystem.Cleaning.Enable();
                    CursorMode = CursorLockMode.Confined;
                    break;
                
                case MapType.UI:
                    InputSystem.UI.Enable();
                    CursorMode = CursorLockMode.None;
                    break;
            }
            
            Cursor.lockState = CursorMode;
            Cursor.visible = CursorMode == CursorLockMode.None;
            MapChanged?.Invoke();
        }
    }
}  