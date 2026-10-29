using System;
using Gameplay.Cardboard;
using Gameplay.Interaction;
using Restorable;
using UnityEngine;

namespace Gameplay.Package
{
    public class Package : Interactable
    {
        [SerializeField] private PackageStep[] _steps;
        [SerializeField] private BoxConfig _config;
        [SerializeField] private Transform _envelopePosition;
        [SerializeField] private Transform _itemPosition;
        
        private int _currentIndex;
        private bool _isBusy;
        
        public PackageStep Current => _currentIndex < _steps.Length ? _steps[_currentIndex] : null;
        public BoxConfig Config => _config;
        
        public RestorableInteractable Item { get; private set; }
        
        public Transform EnvelopePosition => _envelopePosition;
        public Transform ItemPosition => _itemPosition;

        protected override void Awake()
        {
            Item = Instantiate(_config.ItemPrefab, _itemPosition);
        }

        private void Start()
        {
            foreach (var step in _steps)
            {
                step.Initialize(this);
            }
        }

        public void Setup(BoxConfig config)
        {
            _config = config;
        }

        protected override void OnInteracted()
        {
            if(_currentIndex >= _steps.Length)
                return;
            
            if(!Current)
                return;
            
            if(_isBusy)
            {
                Debug.Log($"Is busy!");
                return;
            }

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