using System;
using System.Collections.Generic;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace Gameplay.Cardboard
{
    public class BoxManager : MonoBehaviour, IService
    {
        [SerializeField] private BoxInteractable _boxPrefab;
        [SerializeField] private Transform _boxSpawnPosition;
        
        private readonly List<BoxInteractable> _boxes = new();

        private void OnDestroy()
        {
            foreach (var box in _boxes)
            {
                box.OnStateChange -= OnBoxStateChanged;
            }
            
            _boxes.Clear();
        }

        public void RequestBox()
        {
            var instance = Instantiate(_boxPrefab, _boxSpawnPosition.position, Quaternion.identity);
            instance.OnStateChange += OnBoxStateChanged;
            _boxes.Add(instance);
        }
        
        private void OnBoxStateChanged(BoxInteractable box,BoxState state)
        {
            if (state == BoxState.Opened)
            {
                _boxes.Remove(box);
                box.OnStateChange -= OnBoxStateChanged;
            }
        }
    }
}