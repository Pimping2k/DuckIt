using System;
using DG.Tweening;
using UnityEngine;

namespace UI.ButtonEnhanced.Animations
{
    [Serializable]
    public class ScaleAnimation : ButtonAnimation
    {
        [SerializeField] private Vector3 highlightedScale = Vector3.one * 1.1f;
        
        public override void Play(GameObject target, bool state)
        {
            target.transform.DOScale(
                    state ? highlightedScale : Vector3.one,
                    Duration)
                .SetEase(Ease)
                .SetSpeedBased(IsSpeedBased);
        }
    }
}