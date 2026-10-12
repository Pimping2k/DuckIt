using System;
using GameCycle;
using MyPackage.Runtime.ServiceLocator_Core;
using TMPro;
using UnityEngine;

namespace UI.TimeHud
{
    public class UITime : MonoBehaviour
    {
        [SerializeField] private TMP_Text _time;

        private GameCycleManager _gameCycleManager;

        private void Awake()
        {
            _gameCycleManager = ServiceLocator.Resolve<GameCycleManager>();
            _gameCycleManager.TimeUpdated += OnTimeUpdated;
        }

        private void OnDestroy()
        {
            _gameCycleManager.TimeUpdated -= OnTimeUpdated;
        }

        private void OnTimeUpdated()
        {
            _time.text = $"Hours:{_gameCycleManager.TimeData.Hour:00}:{_gameCycleManager.TimeData.Minute:00}";
        }
    }
}