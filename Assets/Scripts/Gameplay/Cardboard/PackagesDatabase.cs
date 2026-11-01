using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Data;
using Unity.Collections;
using UnityEngine;

namespace Gameplay.Cardboard
{
    [CreateAssetMenu(menuName = DataPaths.GAMEPLAY + "Package Database", fileName = "Package_Database")]
    public class PackagesDatabase : IdDatabase<PackageConfig>
    {
        [SerializeField, ReadOnly] private SerializedDictionary<ushort, PackageConfig> _packages = new();
        
        public override SerializedDictionary<ushort, PackageConfig> Data => _packages;
    }
}