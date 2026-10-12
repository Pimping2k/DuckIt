using Data;
using UnityEngine;

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

        public float DayDuration => _dayDuration;
        public float HourDuration => _hourDuration;
        public int MonthDaysDuration => _monthDaysDuration;
        public Season InitialSeason => _initialSeason;
    }
}