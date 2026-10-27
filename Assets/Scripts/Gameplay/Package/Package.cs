using System;
using Gameplay.Interaction;
using UnityEngine;

namespace Gameplay.Package
{
    public class Package : Interactable
    {
        [SerializeField] private PackageStep[] _steps;
        
        private int _currentIndex;
        private bool _isBusy;
        
        public PackageStep Current => _currentIndex < _steps.Length ? _steps[_currentIndex] : null;
        
        private void Start()
        {
            foreach (var step in _steps)
            {
                step.Initialize(this);
            }
        }

        protected override void OnInteracted()
        {
            if(_currentIndex >= _steps.Length)
                return;
            
            if(!Current)
                return;
            
            if(_isBusy)
                return;

            _isBusy = true;
            Current.Completed += OnCurrentStepCompleted;
            Current.Perform();
        }

        private void OnCurrentStepCompleted(PackageStep step)
        {
            step.Completed -= OnCurrentStepCompleted;
            _currentIndex++;
            _isBusy = false;
            Debug.Log($"Completed {step.name} {_currentIndex}/{_steps.Length}");
        }
    }
}