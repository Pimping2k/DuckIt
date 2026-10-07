using System;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace Services
{
    public interface IInputService : IService
    {
        public MapType MapType { get; }
        public CursorLockMode CursorMode { get; }
        public InputActions InputSystem { get; }
        public InputActions.UIActions UI { get; }
        public InputActions.PlayerActions Player { get; }
        public event Action MapChanged;
        public bool IsReady { get; }
        public void ChangeMap(MapType mapType);
    }
}