using Data;
using DG.Tweening;
using Player;
using UnityEngine;

namespace Gameplay.Tools.ToolPlacement
{
    public class HandPlacement : ToolPlacementBase
    {
        [SerializeField] private PlayerHandsController _handsController;
        
        private BaseTool _currentTool;
        private Sequence _sequence;

        public override void Attach(BaseTool tool)
        {
            if (tool == null)
                return;

            _sequence?.Kill();
            _currentTool = tool;

            _sequence = Animations.HandsAnimations.GetTool(
                _handsController.RightHand,
                tool.transform,
                _handsController.ToolSocket,
                new Vector3(0f, -2f, 0f),
                _handsController.RightHomePos);
        }

        public override void Detach()
        {
            _sequence?.Kill();

            if (_currentTool != null)
            {
                _currentTool.transform.SetParent(null);
                _currentTool = null;
            }

            _handsController.RightHand
                .DOLocalMove(_handsController.RightHomePos, 0.2f)
                .SetEase(Ease.OutCubic)
                .SetLink(_handsController.RightHand.gameObject);
        }
    }
}