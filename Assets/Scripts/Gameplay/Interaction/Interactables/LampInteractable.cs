using UnityEngine;

namespace Gameplay.Interaction.Interactables
{
    public class LampInteractable : Interactable
    {
        [SerializeField] private Light _light;

        private bool _isActive;

        protected override void Awake()
        {
            base.Awake();
            _light.enabled = _isActive;
        }

        protected override void OnInteracted()
        {
            ManageLampState();
        }

        private void ManageLampState()
        {
            _isActive = !_isActive;
            _light.enabled = _isActive;
        }
    }
}