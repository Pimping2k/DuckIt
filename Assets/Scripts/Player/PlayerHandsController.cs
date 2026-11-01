using System;
using Data;
using DG.Tweening;
using Gameplay.Tools;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace Player
{
    public class PlayerHandsController : PlayerComponent
    {
        [SerializeField] private Transform _toolSocket;
        [SerializeField] private Transform _envelopeSocket;
        
        [SerializeField] private Animator _leftHandAnimator;
        [SerializeField] private Animator _rightHandAnimator;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;
        
        private ToolManager _toolManager;
        
        private Transform _leftHomeParent, _rightHomeParent;
        private Quaternion _leftHomeRot, _rightHomeRot;

        public Vector3 LeftHomePos { get; private set; }
        public Vector3 RightHomePos { get; private set; }
        
        public Quaternion LeftHomeRot { get; private set; }
        public Quaternion RightHomeRot { get; private set; }
        
        public Transform LeftHand => _leftHand;
        public Transform RightHand => _rightHand;
        
        public Transform ToolSocket => _toolSocket;
        public Transform EnvelopeSocket => _envelopeSocket;
        
        public override void Initialize()
        {
            _toolManager = ServiceLocator.Resolve<ToolManager>();
            
            _toolManager.ToolChanged += OnToolChanged;
            
            _leftHomeParent = _leftHand.parent;
            LeftHomePos = _leftHand.localPosition;
            _leftHomeRot = _leftHand.localRotation;
            
            _rightHomeParent = _rightHand.parent;
            RightHomePos = _rightHand.localPosition;
            _rightHomeRot = _rightHand.localRotation;
        }

        private void OnDestroy()
        {
            _toolManager.ToolChanged -= OnToolChanged;
        }

        private void OnToolChanged(BaseTool newTool)
        {
            if (!newTool || newTool.ToolData.ToolType == ToolType.Empty)
                OpenHands();
            else
                CloseHands();
        }

        public void OpenHands()
        {
            _rightHandAnimator.SetFloat(AnimationCache.PlayerHands.GRIP_PARAMETER, 0.7f);
            _leftHandAnimator.SetFloat(AnimationCache.PlayerHands.GRIP_PARAMETER, 0.7f);
        }

        public void CloseHands()
        {
            _rightHandAnimator.SetFloat(AnimationCache.PlayerHands.GRIP_PARAMETER, 1);
            _leftHandAnimator.SetFloat(AnimationCache.PlayerHands.GRIP_PARAMETER, 1);
        }

        public void PlayAnimation(int animationHash, int layer = 0)
        {
            _rightHandAnimator.Play(animationHash, layer);
        }
        
        #region Utility
        
        public Sequence ReachWithHand(bool rightHand, Vector3 targetWorldPos,
            Quaternion? targetWorldRot = null, Action onReached = null,
            float reachDuration = 0.25f, float holdDuration = 0.05f, float returnDuration = 0.3f)
        {
            var hand = rightHand ? _rightHand : _leftHand;
            var homePos = rightHand ? RightHomePos : LeftHomePos;
            var homeRot = rightHand ? _rightHomeRot : _leftHomeRot;

            hand.DOKill();

            return Animations.HandsAnimations.ReachAndReturn(
                hand, homePos, homeRot, targetWorldPos, targetWorldRot,
                reachDuration, holdDuration, returnDuration, onReached);
        }
        
        public void MoveToolTo(Vector3 pos, Quaternion rot) => MoveHandSoThat(ToolSocket, pos, rot);

        public void MoveHandSoThat(Transform attached, Vector3 worldPos, Quaternion worldRot)
        {
            Quaternion inv = Quaternion.Inverse(_rightHand.rotation);
            Quaternion rotOffset = inv * attached.rotation;
            Vector3 posOffset = inv * (attached.position - _rightHand.position);

            Quaternion handRot = worldRot * Quaternion.Inverse(rotOffset);
            
            _rightHand.SetPositionAndRotation(worldPos - handRot * posOffset, handRot);
        }

        public Sequence ReturnHands(float duration = 0.4f)
        {
            _leftHand.DOKill();
            _rightHand.DOKill();

            _leftHand.SetParent(_leftHomeParent, true);
            _rightHand.SetParent(_rightHomeParent, true);

            OpenHands();

            return DOTween.Sequence().SetLink(gameObject)
                .Append(_leftHand.DOLocalMove(LeftHomePos, duration).SetEase(Ease.OutCubic))
                .Join(_leftHand.DOLocalRotateQuaternion(_leftHomeRot, duration).SetEase(Ease.OutCubic))
                .Join(_rightHand.DOLocalMove(RightHomePos, duration).SetEase(Ease.OutCubic))
                .Join(_rightHand.DOLocalRotateQuaternion(_rightHomeRot, duration).SetEase(Ease.OutCubic));
        }

        #endregion
    }
}