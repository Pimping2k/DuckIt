using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyPackage.Runtime.ServiceLocator_Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();
        private static readonly Dictionary<Type, object> _configs = new();
        
        public static IReadOnlyDictionary<Type, object> Services => _services;
        public static IReadOnlyDictionary<Type, object> Configs => _configs;
        
        public static void RegisterService(Type type, object instance)
        {
            if (_services.TryAdd(type, instance))
            {
                Debug.Log($"[ServiceLocator] Registered service: {type.Name}");
            }
            else
            {
                _services[type] = instance;
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} was overwritten.");
            }
        }

        public static void RegisterConfig(Type type, object instance)
        {
            if (_configs.TryAdd(type, instance))
            {
                Debug.Log($"[ServiceLocator] Registered config: {type.Name}");
            }
            else
            {
                _configs[type] = instance;
                Debug.LogWarning($"[ServiceLocator] Config {type.Name} was overwritten.");
            }
        }

        public static void Register<T>(T instance) where T : class
        {
            RegisterService(typeof(T), instance);
        }
        
        public static bool UnRegister(Type service)
        {
            return _services.Remove(service);
        }

        public static T ResolveConfig<T>() where T : IConfig
        {
            if (_configs.TryGetValue(typeof(T), out var config))
                return (T)config;
            
            Debug.LogError($"[ServiceLocator] Config {typeof(T).Name} not found!");
            return default;
        }
        
        public static T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var instance))
                return (T)instance;

            Debug.LogError($"[ServiceLocator] Service of type {typeof(T).Name} not found!");
            return null;
        }

        public static bool TryGetService<T>(out T instance) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
            {
                instance = (T)obj;
                return true;
            }
            
            instance = null;
            return false;
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}