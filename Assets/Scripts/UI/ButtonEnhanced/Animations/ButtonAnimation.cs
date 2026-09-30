using System;
using DG.Tweening;
using UnityEngine;

namespace UI.ButtonEnhanced.Animations
{
    [Serializable]
    public abstract class ButtonAnimation
    {
        [SerializeField] protected float Duration = .15f;
        [SerializeField] protected Ease Ease = Ease.OutBack;
        [SerializeField] protected bool IsSpeedBased;
        
        public abstract void Play(GameObject target, bool state);
    }
}