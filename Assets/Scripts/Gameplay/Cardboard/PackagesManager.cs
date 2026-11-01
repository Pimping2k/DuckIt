using System.Collections.Generic;
using System.Linq;
using Data;
using DG.Tweening;
using Extensions;
using GameCycle;
using Gameplay.PackageRelated;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace Gameplay.Cardboard
{
    public class PackagesManager : MonoBehaviour, IService
    {
        [SerializeField] private Package _packagePrefab;
        [SerializeField] private Transform _boxSpawnPosition;
        [SerializeField] private Transform _boxThrowAway;
        [SerializeField] private Transform _packageTargetPosition;
        [SerializeField] private Transform[] _pipePath;
        
        private readonly List<Package> _packages = new();
        
        private PackagesDatabase _packagesDatabase;
        private GameCycleManager _gameCycleManager;
        
        private void Awake()
        {
            _packagesDatabase = ServiceLocator.ResolveConfig<PackagesDatabase>();
            _gameCycleManager = ServiceLocator.Resolve<GameCycleManager>();
            
            _gameCycleManager.HourChanged += OnHourChanged;
        }

        private void OnDestroy()
        {
            _gameCycleManager.HourChanged -= OnHourChanged;
            
            foreach (var box in _packages)
                box.LastStep.Completed -= OnLastStepCompleted;
            
            _packages.Clear();
        }

        private void OnHourChanged(float time)
        {
            if(Mathf.Approximately(_gameCycleManager.TimeData.Hour, 7f))
                RequestBox();
        }

        public void RequestBox()
        {
            var instance = Instantiate(_packagePrefab, _boxSpawnPosition.position, Quaternion.identity);
            instance.Setup(_packagesDatabase.Data.Values.ToList().GetRandomValue());
            instance.LastStep.Completed += OnLastStepCompleted;
            _packages.Add(instance);
            Animations.DropBoxFromPipe(instance.transform, _pipePath, _packageTargetPosition, pipeDuration: 1.5f);
        }

        private void OnLastStepCompleted(PackageStep step)
        {
            Animations.ThrowBoxOffTable(step.Package.transform, _boxThrowAway).OnComplete(() =>
            {
                _packages.Remove(step.Package);
                Destroy(step.Package.gameObject);
            });
        }
    }
}