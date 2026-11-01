using System.Collections.Generic;
using System.Linq;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace MyPackage.Runtime.Registrators
{
    [DefaultExecutionOrder(-1500)]
    public class ServiceRegistrator : MonoBehaviour
    {
        private static ServiceRegistrator _instance;
        
        [SerializeField] private List<MonoBehaviour> _instances = new();
        [SerializeField] private List<ScriptableObject> _configs = new();
        
        private void Awake()
        {
            if (_instance)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            
            DontDestroyOnLoad(gameObject);
            InitializeServices();
            RegisterConfigs();
        }

        private void Start()
        {
            foreach (var kvp in ServiceLocator.Services)
            {
                Debug.Log($"Service type: {kvp.Key.Name}");
            }

            foreach (var config in _configs)
            {
                Debug.Log($"Config type: {config.GetType().Name}");
            }
        }
        
        private void InitializeServices()
        {
            foreach (var instance in _instances)
            {
                if (instance is IService service)
                {
                    if (instance.gameObject != gameObject)
                        DontDestroyOnLoad(instance.gameObject);
                
                    var serviceType = service.GetType();
                    var interfaces = serviceType.GetInterfaces()
                        .Where(i => typeof(IService).IsAssignableFrom(i) && i != typeof(IService))
                        .ToList();

                    if (interfaces.Count > 0)
                    {
                        foreach (var iface in interfaces)
                        {
                            ServiceLocator.RegisterService(iface, service);
                        }
                    }
                    else
                    {
                        ServiceLocator.RegisterService(serviceType, service);
                    }
                }
            }
        }

        private void RegisterConfigs()
        {
            foreach (var config in _configs)
            {
                if (config == null)
                {
                    Debug.LogWarning("[ServiceRegistrator] Empty slot in _configs", this);
                    continue;
                }

                if (config is not IConfig)
                {
                    Debug.LogWarning($"[ServiceRegistrator] {config.name} does not implement IConfig", config);
                    continue;
                }

                var type = config.GetType();

                ServiceLocator.RegisterConfig(type, config);
            }
        }
    }
}
