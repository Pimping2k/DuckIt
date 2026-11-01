using Data;
using DG.Tweening;
using EventSub;
using EventSub.Implementation;
using Gameplay.Tools;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using UnityEngine;
using View;

namespace Gameplay.PackageRelated.Steps
{
    public class ReadEnvelopeStep : PackageStep
    {
        private PlayerHandsController _handsController;
        private ToolManager _toolManager;
        
        public override bool CanPerform()
        {
            var tool = _toolManager.CurrentTool;
            return !tool || tool.ToolData.ToolType == ToolType.Empty;
        }
        
        private void Start()
        {
            _toolManager = ServiceLocator.Resolve<ToolManager>();
            _handsController = LocalPlayer.Instance.GetPlayerComponent<PlayerHandsController>();
        }
        
        public override void Perform()
        {
            if(_toolManager.CurrentTool.ToolData.ToolType != ToolType.Empty)
                return;
            
            _handsController.ReachWithHand(true, Package.EnvelopePosition.position, onReached: OnHandReachedEnvelope)
                .OnComplete(OnHandCompletedReturn);
        }

        private void OnHandCompletedReturn()
        {
            EventBus.Publish(new RequestViewEvent(ViewType.BoxNote, Package.Config));
            EventBus.Subscribe<NoteViewHideEvent>(OnNoteHidden);
        }

        private void OnNoteHidden(NoteViewHideEvent args)
        {
            EventBus.Unsubscribe<NoteViewHideEvent>(OnNoteHidden);
            Destroy(Package.EnvelopePosition.gameObject);
            _handsController.PlayAnimation(AnimationCache.PlayerHands.ACTION_NONE);
            Complete();
        }

        private void OnHandReachedEnvelope()
        {
            Package.EnvelopePosition.SetParent(_handsController.EnvelopeSocket);
            Package.EnvelopePosition.localPosition = Vector3.zero;
            Package.EnvelopePosition.localRotation = Quaternion.identity;
            _handsController.PlayAnimation(AnimationCache.PlayerHands.HOLD_NOTE_ACTION);
        }
    }
}