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
        [SerializeField] private float _dayDuration;
        [SerializeField] private float _hourDuration;
        [SerializeField] private int _monthDaysDuration;
        [SerializeField] private Season _initialSeason;
        [SerializeField] private Vector2 _timeRange;

        public Vector2 TimeRange => _timeRange;
        public float DayDuration => _dayDuration;
        public float HourDuration => _hourDuration;
        public int MonthDaysDuration => _monthDaysDuration;
        public Season InitialSeason => _initialSeason;
    }
}