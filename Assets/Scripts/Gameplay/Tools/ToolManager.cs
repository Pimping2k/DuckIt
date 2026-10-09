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
            if (!tool) 
                return;
            
            var t = tool.transform;
            t.DOKill();
            t.SetParent(_playerToolController.HoldTransform, true);
            DOTween.Sequence().SetTarget(t).SetLink(tool.gameObject)
                .Append(t.DOLocalMove(Vector3.zero, _moveDuration))
                .Join(t.DOLocalRotateQuaternion(Quaternion.identity, _moveDuration));
        }

        private void Restore(BaseTool tool)
        {
            if (!tool || !_toolsInitialPositionMap.TryGetValue(tool, out var home))
                return;
            
            var t = tool.transform;
            t.DOKill();
            t.SetParent(home, true);
            DOTween.Sequence().SetTarget(t).SetLink(tool.gameObject)
                .Append(t.DOLocalMove(Vector3.zero, _moveDuration))
                .Join(t.DOLocalRotateQuaternion(Quaternion.identity, _moveDuration));
        }
    }
}