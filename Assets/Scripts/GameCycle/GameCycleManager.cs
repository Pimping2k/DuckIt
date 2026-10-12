using System;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace GameCycle
{
    public class GameCycleManager : MonoBehaviour, IService
    {
        [SerializeField] private GameCycleConfig _config;
        
        public event Action<Season> SeasonChanged;
        public event Action<float> HourChanged;
        public event Action<int> DayChanged;
        
        private void Update()
        {
            
        }
    }
}