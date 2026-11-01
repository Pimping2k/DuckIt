using System.Numerics;
using Data;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace GameCycle
{
    public enum Season
    {
        Summer,
        Autumn,
        Winter,
        Spring,
    }
    
	[CreateAssetMenu(menuName =  DataPaths.GAMEPLAY +"Game Cycle", fileName = "GameCycle_Config")]
    public class GameCycleConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _hourDurationRealMinutes = 1f;
        [SerializeField] private int _monthDaysDuration;
        [SerializeField] private Season _initialSeason;
        [SerializeField] private Vector2 _timeRange = new(8f, 20f);

        public Vector2 TimeRange => _timeRange;
        public float HourDurationRealMinutes => _hourDurationRealMinutes;
        public float HourDurationRealSeconds => _hourDurationRealMinutes * 60f;
        public float MinuteDurationRealSeconds => HourDurationRealSeconds / 60f;
        public float DayDurationRealMinutes => (_timeRange.y - _timeRange.x) * _hourDurationRealMinutes;
    }
}