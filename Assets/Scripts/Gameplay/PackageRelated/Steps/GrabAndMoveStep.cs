using Data;
using DG.Tweening;
using Extensions;
using Gameplay.Cardboard;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using UnityEngine;

namespace Gameplay.PackageRelated.Steps
{
    public class GrabAndMoveStep : PackageStep
    {
        [SerializeField] private Transform _leftHandPosition;
        [SerializeField] private Transform _rightHandPosition;
        [Header("Debug")]
        [SerializeField] private Mesh _leftHandMesh;
        [SerializeField] private Mesh _rightHandMesh;
        
        private PlacementManager _placementManager;
        private PlayerHandsController _playerHandsController;
        
        private Transform _leftHand;
        private Transform _rightHand;

        protected override void OnInitialized()
        {
            _placementManager = ServiceLocator.Resolve<PlacementManager>();
            _playerHandsController = LocalPlayer.Instance.GetPlayerComponent<PlayerHandsController>();

            _leftHand = _playerHandsController.LeftHand;
            _rightHand = _playerHandsController.RightHand;
        }

        public override void Perform()
        {
            var box = Package.transform;
            var place = _placementManager.BoxPositions.GetRandomValue();

            _playerHandsController.CloseHands();

            DOTween.Sequence().SetLink(gameObject)
                .Append(Animations.HandsAnimations.GrabBoxWithHands(_leftHand, _rightHand,
                    _leftHandPosition, _rightHandPosition))
                .Append(Animations.HandsAnimations.MoveBoxToPlace(box, place))
                .OnComplete(() => _playerHandsController.ReturnHands().OnComplete(Complete));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            if(_leftHandPosition && _leftHandMesh)
                Gizmos.DrawMesh(_leftHandMesh, -1, _leftHandPosition.position, _leftHandPosition.rotation);
            if(_rightHandPosition && _rightHandMesh)
                Gizmos.DrawMesh(_rightHandMesh, -1, _rightHandPosition.position, _rightHandPosition.rotation);
        }
#endif
    }
}