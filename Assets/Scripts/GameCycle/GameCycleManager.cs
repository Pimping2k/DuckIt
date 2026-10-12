using System;
using Cysharp.Threading.Tasks;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace GameCycle
{
    [Serializable]
    public struct TimeData
    {
        public float Hour;
        public int Minute;

        public TimeData(float hour, int minute)
        {
            Hour = hour;
            Minute = minute;
        }
    }
    
    public class GameCycleManager : MonoBehaviour, IService
    {
        [SerializeField] private GameCycleConfig _config;

        private float _currentHour;
        private int _currentMinutes;
        private float _minuteTiming;
       
        public TimeData TimeData { get; private set; }
        
        public event Action<Season> SeasonChanged;
        public event Action<float> HourChanged;
        public event Action<float> MinuteChanged;
        public event Action<int> DayChanged;
        public event Action TimeUpdated;
        
        private void Awake()
        {
            CalculateTimings();
        }

        private async void Start()
        {
            await RunGameLoopAsync();
        }

        private async UniTask RunGameLoopAsync()
        {
            var token = this.GetCancellationTokenOnDestroy();
            
            while (_currentHour < _config.TimeRange.y && !token.IsCancellationRequested)
            {
                await UniTask.WaitForSeconds(_minuteTiming, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
                
                if(token.IsCancellationRequested)
                    return;
                
                _currentMinutes++;

                if (_currentMinutes >= 60)
                {
                    _currentMinutes = 0;
                    _currentHour++;
                    HourChanged?.Invoke(_currentHour);
                }

                MinuteChanged?.Invoke(_currentMinutes);
                TimeData = new TimeData(_currentHour, _currentMinutes);
                TimeUpdated?.Invoke();
            }
        }

        private void CalculateTimings()
        {
            _currentHour = _config.TimeRange.x;
            var totalDayDuration = _config.TimeRange.y - _config.TimeRange.x;
            var minuteDuration = totalDayDuration / 60f;
            _minuteTiming = minuteDuration;
            TimeData = new TimeData(_currentHour, _currentMinutes);
        }
    }
}