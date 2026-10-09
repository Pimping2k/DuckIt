using System;
using System.Collections.Generic;
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
        Finish,
    }
    
    [Serializable]
    public struct FlapPose
    {
        public Transform flap;
        public Vector3 openEuler;
        public float delay;
    }

    public class BoxInteractable : Interactable
    {
        [SerializeField] private GameObject _tape;
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField] private FlapPose[] _flaps;
        
        private bool _isBusy;
        private BoxState _state;

        private BoxState State
        {
            get => _state;
            set
            {
                _state = value;
                OnStateChange?.Invoke(this, value);
            }
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
            if (_isBusy)
                return;

            switch (State)
            {
                case BoxState.Initial:
                    PerformMove();
                    break;
                case BoxState.Moved:
                    PerformOpenBox();
                    break;
            }
        }

        private void PerformMove()
        {
            _isBusy = true;
            var targetPosition = _placementManager.Positions.GetRandomValue();
            var targetObject = transform;

            Animations.LaunchBox(targetObject, targetPosition).OnComplete(() =>
            {
                _isBusy = false;
                State = BoxState.Moved;
            });
        }

        private void PerformOpenBox()
        {
            var toolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();

            if (!toolController.CurrentTool)
                return;

            if (toolController.CurrentTool.Type != ToolType.Knife)
                return;

            _isBusy = true;
            Animations.OpenBoxWithKnife(toolController.CurrentTool.transform, _startPoint, _endPoint, toolController.HoldTransform.transform).OnComplete(() =>
            {
                PerformOpenFlaps();
                _tape.SetActive(false);
                State = BoxState.Opened;
            });
        }

        private void PerformOpenFlaps()
        {
            _isBusy = true;
            Animations.OpenBoxFlaps(gameObject, _flaps).OnComplete(() =>
            {
                _isBusy = false;
                _state = BoxState.Finish;
            });
        }

#if UNITY_EDITOR

        [ContextMenu("Capture open pose from current")]
        private void CaptureOpenPose()
        {
            UnityEditor.Undo.RecordObject(this, "Capture open pose");
            for (int i = 0; i < _flaps.Length; i++)
                if (_flaps[i].flap) _flaps[i].openEuler = _flaps[i].flap.localEulerAngles;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (_endPoint != null)
            {
                Gizmos.DrawWireSphere(_endPoint.position, 0.05f);
            }

            if (_startPoint != null)
            {
                Gizmos.DrawWireSphere(_startPoint.position, 0.05f);
            }
        }

#endif
    }
}