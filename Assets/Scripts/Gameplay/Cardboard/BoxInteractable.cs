using System;
using Cysharp.Threading.Tasks;
using Data;
using DG.Tweening;
using Extensions;
using Gameplay.Interaction;
using Gameplay.Tools;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using UnityEngine;

namespace Gameplay.Cardboard
{
    public enum BoxState
    {
        Initial,
        Moved,
        Opened,
    }
    
    public class BoxInteractable : Interactable
    {
        private BoxState _state;
        private BoxState State
        {
            get => _state;
            set { _state = value; OnStateChange?.Invoke(this, value); }
        }

        private PlacementManager _placementManager;
        public event Action<BoxInteractable, BoxState> OnStateChange; 
        
        private void Awake()
        {
            _placementManager = ServiceLocator.Resolve<PlacementManager>();
        }

        protected override void OnInteracted()
        {
            PerformInteractionByState();
        }

        private void PerformInteractionByState()
        {
            switch (State)
            {
                case BoxState.Initial:
                    PerformMove();
                    State = BoxState.Moved;
                    break;
                case BoxState.Moved:
                    PerformOpenBox();
                    State = BoxState.Opened;
                    break;
                case BoxState.Opened:
                    break;
            }
        }
        
        private void PerformMove()
        {
            var targetPosition = _placementManager.Positions.GetRandomValue();
            var targetObject = transform;

            Animations.LaunchBox(targetObject, targetPosition);
        }

        private void PerformOpenBox()
        {
            if(LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>().CurrentTool?.Type != ToolType.Knife)
                return;
        }
    }
}