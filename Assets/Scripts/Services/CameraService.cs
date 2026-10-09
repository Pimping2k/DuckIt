using System.Collections.Generic;
using MyPackage.Runtime.ServiceLocator_Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Services
{
    public class CameraService : MonoBehaviour, IService
    {
        [SerializeField] private CinemachineBrain _cameraBrain;
        [SerializeField] private CinemachineCamera _defaultCamera;
        
        private CinemachineCamera _currentCamera;
        private CinemachineCamera _previousCamera;
        private readonly Stack<CinemachineCamera> _stack = new();
        
        private int _mainPriority = 999;
        
        public CinemachineCamera Camera => _currentCamera;
        public CinemachineBrain CameraBrain => _cameraBrain;
        
        private void Awake()
        {
            OverlapCamera(_defaultCamera);
        }

        public void OverlapCamera(CinemachineCamera overlapCamera, float blendTime = 0f)
        {
            if (_currentCamera) 
                _currentCamera.Priority = -1;
            
            _stack.Push(overlapCamera);
            _currentCamera = overlapCamera;
            _currentCamera.Priority = _mainPriority;
        }

        public void RemoveOverlappingCamera(float blendTime = 0f)
        {
            if (_stack.Count <= 1) 
                return;
            
            _currentCamera.Priority = -1;
            _stack.Pop();
            _currentCamera = _stack.Peek();
            _currentCamera.Priority = _mainPriority;
        }
    }
}