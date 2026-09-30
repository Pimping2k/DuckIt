using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.ButtonEnhanced.Animations
{
    [Serializable]
    public class ImageFadeAnimation : ButtonAnimation
    {
        [SerializeField] private Image _image;
        
        public override void Play(GameObject target, bool state)
        {
            _image.DOFade(state ? 1 : 0, Duration)
                .SetEase(Ease)
                .SetSpeedBased(IsSpeedBased);
        }
    }
}