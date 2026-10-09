using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player
{
    public class LocalPlayer : MonoBehaviour
    {
        public static LocalPlayer Instance;
        
        private List<PlayerComponent> _playerComponents;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _playerComponents = GetComponentsInChildren<PlayerComponent>().ToList();
            foreach (var c in _playerComponents) c.Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this) 
                Instance = null;
        }

        public T GetPlayerComponent<T>() where T : PlayerComponent
        {
            var type =  typeof(T);
            
            var component = _playerComponents.FirstOrDefault(x=>x.GetType() == type);
            return component as T;
        }
    }
}