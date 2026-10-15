using AYellowpaper.SerializedCollections;
using DG.Tweening;
using Player;
using UnityEngine;

namespace Gameplay.Tools
{
    public class ToolManager : MonoBehaviour
    {
        
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
            
        }
    }
}