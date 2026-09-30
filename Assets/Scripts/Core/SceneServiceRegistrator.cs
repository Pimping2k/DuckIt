using System.Collections.Generic;
using System.Linq;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace Gameplay.Core
{
    [DefaultExecutionOrder(-1000)]
    public class SceneServiceRegistrator : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> _instances = new();
        private readonly List<System.Type> _registeredTypes = new();

        private void Awake()
        {
            foreach (var instance in _instances)
            {
                if (instance is not IService service) continue;

                var interfaces = service.GetType().GetInterfaces()
                    .Where(i => typeof(IService).IsAssignableFrom(i) && i != typeof(IService))
                    .ToList();

                if (interfaces.Count > 0)
                {
                    foreach (var iface in interfaces)
                    {
                        ServiceLocator.Register(iface, service);
                        _registeredTypes.Add(iface);
                    }
                }
                else
                {
                    ServiceLocator.Register(service.GetType(), service);
                    _registeredTypes.Add(service.GetType());
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var type in _registeredTypes)
                ServiceLocator.UnRegister(type);
        }
    }
}