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
            _playerComponents = GetComponentsInChildren<PlayerComponent>().ToList();
            
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            foreach (var playerComponent in _playerComponents)
            {
                playerComponent.Initialize();
            }
        }

        public T GetPlayerComponent<T>() where T : PlayerComponent
        {
            var type =  typeof(T);
            
            var component = _playerComponents.FirstOrDefault(x=>x.GetType() == type);
            return component as T;
        }
    }
}