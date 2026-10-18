using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        private Camera _mainCamera;
        
        public CinemachineCamera Camera => _currentCamera;
        public CinemachineBrain CameraBrain => _cameraBrain;
        public Camera MainCamera
        {
            get
            {
                if (!_mainCamera)
                    _mainCamera = CameraBrain.GetComponent<Camera>();
                return _mainCamera;
            }
        }
        
        private void Awake()
        {
            OverlapCamera(_defaultCamera);
        }

        public async UniTask OverlapCamera(CinemachineCamera overlapCamera, float blendTime = 0f)
        {
            if (_currentCamera) 
                _currentCamera.Priority = -1;
            
            _stack.Push(overlapCamera);
            _currentCamera = overlapCamera;
            _currentCamera.Priority = _mainPriority;
            await UniTask.WaitForSeconds(CameraBrain.DefaultBlend.BlendTime);
        }

        public async UniTask RemoveOverlappingCamera(float blendTime = 0f)
        {
            if (_stack.Count <= 1)
            {
                await UniTask.WaitForSeconds(CameraBrain.DefaultBlend.BlendTime);
                return;
            }
            
            _currentCamera.Priority = -1;
            _stack.Pop();
            _currentCamera = _stack.Peek();
            _currentCamera.Priority = _mainPriority;
            await UniTask.WaitForSeconds(CameraBrain.DefaultBlend.BlendTime);
        }
    }
}