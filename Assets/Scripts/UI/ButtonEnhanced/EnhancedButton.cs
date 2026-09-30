using System;
using System.Collections.Generic;
using UI.ButtonEnhanced.Animations;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.ButtonEnhanced
{
    public class EnhancedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeReference] private List<ButtonAnimation> onHighlight = new();
        [SerializeReference] private List<ButtonAnimation> onClick = new();
        
        public event Action<bool> OnHighlight;
        public event Action OnClick;

        private void Awake()
        {
            
        }

        private void OnDestroy()
        {
            RemoveAllListeners();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Play(onHighlight, true);
            OnHighlight?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Play(onHighlight, false);
            OnHighlight?.Invoke(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Play(onClick, true);
            OnClick?.Invoke();
        }

        private void RemoveAllListeners()
        {
            OnHighlight = null;
            OnClick = null;
        }
        
        private void Play(List<ButtonAnimation> animations, bool state)
        {
            foreach (var animation in animations)
                animation.Play(gameObject, state);
        }
    }
}