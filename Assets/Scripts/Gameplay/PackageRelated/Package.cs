using Gameplay.Cardboard;
using Gameplay.Interaction;
using Restorable;
using UnityEngine;

namespace Gameplay.PackageRelated
{
    public class Package : Interactable
    {
        [SerializeField] private PackageStep[] _steps;
        [SerializeField] private PackageConfig _config;
        [SerializeField] private Transform _envelopePosition;
        [SerializeField] private Transform _itemPosition;
        
        private int _currentIndex;
        private bool _isBusy;
        
        public PackageStep CurrentStep => _currentIndex < _steps.Length ? _steps[_currentIndex] : null;
        public PackageStep LastStep => _steps[^1];
        public PackageConfig Config => _config;
        
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

        public void Setup(PackageConfig config)
        {
            _config = config;
        }

        protected override void OnInteracted()
        {
            if(_currentIndex >= _steps.Length)
                return;
            
            if(!CurrentStep)
                return;

            if (!CurrentStep.CanPerform())
                return;
                
            if(_isBusy)
            {
                Debug.Log($"Is busy!");
                return;
            }

            _isBusy = true;
            CurrentStep.Completed += OnCurrentStepStepCompleted;
            CurrentStep.Perform();
        }

        private void OnCurrentStepStepCompleted(PackageStep step)
        {
            step.Completed -= OnCurrentStepStepCompleted;
            _currentIndex++;
            _isBusy = false;
            Debug.Log($"Completed {step.name} {_currentIndex}/{_steps.Length}");
        }
    }
}