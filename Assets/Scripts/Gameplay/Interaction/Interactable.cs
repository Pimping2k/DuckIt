using System;
using UnityEngine;

namespace Gameplay.Interaction
{
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Outlinable _outlinable;

        public bool IsInteractable { get; private set; } = true;
        
        protected virtual void Awake() { }

        public void Interact()
        {
            if(!IsInteractable)
                return;
            
            OnInteracted();
        }

        public void Highlight(bool isHighlighted)
        {
            _outlinable.ChangeOutline(isHighlighted);
            OnHighlighted(isHighlighted);
        }

        public void ToggleInteractableState(bool state)
        {
            IsInteractable = state;
        }
        
        protected virtual void OnInteracted(){}
        protected virtual void OnHighlighted(bool isHighlighted) {}
    }
}