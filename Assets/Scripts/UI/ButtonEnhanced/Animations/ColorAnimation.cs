using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.ButtonEnhanced.Animations
{
    [Serializable]
    public class ColorAnimation : ButtonAnimation
    {
        [SerializeField] private Color _color;
        [SerializeField] private Graphic _targetGraphic;
        
        private Color _initialColor;
        
        public override void Play(GameObject target, bool state)
        {
            _initialColor = _targetGraphic.color;
            _targetGraphic.DOColor(state ? _color : _initialColor, Duration).SetEase(Ease).SetSpeedBased(IsSpeedBased);
        }
    }
}