using System.Collections.Generic;
using MyPackage.Runtime.ServiceLocator_Core;
using NUnit.Framework;
using UnityEngine;

namespace Gameplay.Cardboard
{
    public class PlacementManager : MonoBehaviour, IService
    {
        [SerializeField] private List<Transform> _boxPositions = new();
        [SerializeField] private List<Transform> _itemPositions = new();
        
        public List<Transform> BoxPositions => _boxPositions;
        public List<Transform> ItemPositions => _itemPositions;
    }
}