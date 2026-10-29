using System;
using Data;
using DG.Tweening;
using EventSub;
using EventSub.Implementation;
using Player;
using UnityEngine;
using View;

namespace Gameplay.Package.Steps
{
    public class ReadEnvelopeStep : PackageStep
    {
        private PlayerHandsController _handsController;
        
        private void Start()
        {
            _handsController = LocalPlayer.Instance.GetPlayerComponent<PlayerHandsController>();
        }

        public override void Perform()
        {
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