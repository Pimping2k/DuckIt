using System.Collections.Generic;
using MyPackage.Runtime.ServiceLocator_Core;
using NUnit.Framework;
using UnityEngine;

namespace Gameplay.Cardboard
{
    public class PlacementManager : MonoBehaviour, IService
    {
        [SerializeField] private List<Transform> _positions = new();

        public List<Transform> Positions => _positions;
    }
}