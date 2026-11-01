using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using DefaultNamespace;
using MyPackage.Runtime.ServiceLocator_Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Data
{
    public abstract class IdDatabase<T> : ScriptableObject, IConfig where T : IdConfig
    {
#if UNITY_EDITOR
        [EditorButton(nameof(FillDatabase))]
#endif
        [SerializeField] private int _fillDb;
        
        public abstract SerializedDictionary<ushort, T> Data { get; }

        public IReadOnlyDictionary<ushort, T> Items => Data;

        public bool TryGet(ushort id, out T item) => Data.TryGetValue(id, out item);

#if UNITY_EDITOR
        public void FillDatabase()
        {
            Undo.RecordObject(this, $"Fill {name}");

            var configs = new List<T>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var config = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (config != null) configs.Add(config);
            }

            configs.Sort((a, b) =>
            {
                int c = a.Id.CompareTo(b.Id);
                return c != 0 ? c : string.CompareOrdinal(a.name, b.name);
            });

            var used = new HashSet<ushort>();
            var needId = new List<T>();
            ushort maxId = 0;

            foreach (var config in configs)
            {
                if (config.Id != 0 && used.Add(config.Id))
                    maxId = (ushort)Mathf.Max(maxId, config.Id);
                else
                    needId.Add(config);
            }

            foreach (var config in needId)
            {
                if (maxId == ushort.MaxValue)
                {
                    Debug.LogError($"Закончились id для {typeof(T).Name}", this);
                    break;
                }

                config.SetId(++maxId);
            }

            Data.Clear();
            foreach (var config in configs)
                Data[config.Id] = config;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"{name}: {Data.Count} configs", this);
        }
#endif
    }
}