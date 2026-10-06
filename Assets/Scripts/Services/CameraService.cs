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

        private int _mainPriority = 999;
        
        public CinemachineCamera Camera => _currentCamera;
        public CinemachineBrain CameraBrain => _cameraBrain;
        
        private void Awake()
        {
            OverlapCamera(_defaultCamera);
        }

        public void OverlapCamera(CinemachineCamera camera, float blendTime = 0f)
        {
            if(_currentCamera)
            {
                _currentCamera.Priority = -1;
                _previousCamera = _currentCamera;
            }
            
            _currentCamera = camera;
            _currentCamera.Priority = _mainPriority;
        }

        public void RemoveOverlappingCamera(float blendTime = 0f)
        {
            if(_previousCamera)
            {
                var previousCamera = _previousCamera;
                _previousCamera.Priority = -1;
                
                _currentCamera = _previousCamera;
                _previousCamera = previousCamera;
                
                _currentCamera.Priority = _mainPriority;
            }
        }
    }
}