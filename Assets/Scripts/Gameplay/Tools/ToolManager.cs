using System;
using AYellowpaper.SerializedCollections;
using DG.Tweening;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using UnityEngine;

namespace Gameplay.Tools
{
    public class ToolManager : MonoBehaviour
    {
        [SerializeField] private SerializedDictionary<BaseTool, Transform> _toolsInitialPositionMap;
        [SerializeField] private float _moveDuration = 0.5f;
        
        private PlayerToolController _playerToolController;

        private Tween _moveInHandsTween;
        private Tween _restoreTween;

        private void Start()
        {
            _playerToolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();
            _playerToolController.ToolChanged += OnToolChanged;
        }

        private void OnDestroy()
        {
            _playerToolController.ToolChanged -= OnToolChanged;
        }

        private void OnToolChanged(BaseTool prevTool, BaseTool newTool)
        {
            Restore(prevTool);
            SetInPlayerHands(newTool);
        }

        private void SetInPlayerHands(BaseTool tool)
        {
            if(!tool)
                return;
            
            _moveInHandsTween?.Kill();
            _moveInHandsTween = tool.transform.DOMove(_playerToolController.HoldTransform.position, _moveDuration, true).OnComplete(() =>
            {
                tool.transform.SetParent(_playerToolController.HoldTransform);
                tool.transform.localPosition = Vector3.zero;
            });
            _moveInHandsTween = tool.transform.DORotate(_playerToolController.HoldTransform.localEulerAngles, _moveDuration).OnComplete(()=>tool.transform.rotation = Quaternion.Euler(Vector3.zero));
        }

        private void Restore(BaseTool tool)
        {
            if(!tool || !_toolsInitialPositionMap.TryGetValue(tool, out var initialToolTransform))
                return;
    
            _restoreTween?.Kill();
            _restoreTween = tool.transform.DOMove(initialToolTransform.position, _moveDuration,true);
            _restoreTween = tool.transform.DORotate(initialToolTransform.localEulerAngles, _moveDuration);
        }
    }
}