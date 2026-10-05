using UnityEngine;

namespace Gameplay.Interaction
{
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Outlinable _outlinable;
        
        public void Interact()
        {
            OnInteracted();
        }

        public void Highlight(bool isHighlighted)
        {
            _outlinable.ChangeOutline(isHighlighted);
            OnHighlighted(isHighlighted);
        }

        protected virtual void OnInteracted(){}
        protected virtual void OnHighlighted(bool isHighlighted) {}
    }
}