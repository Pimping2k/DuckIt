using System;
using Gameplay.Interaction;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using Restorable;
using Services;
using UnityEngine;

namespace Gameplay.Tools
{
    public enum ToolType
    {
        None = 0,
        Knife = 1,
        Brush = 2,
        Sponge = 3,
        Scraper = 4,
        Sandpaper = 5,
        Glue = 6,
        Screwdriver = 7,
        Empty = 8,
    }
    
    public abstract class BaseTool : MonoBehaviour
    {
        [SerializeField] protected ToolData _toolData;
        [SerializeField] protected RestoreTool _restoreToolType;
        
        protected PlayerToolController PlayerToolController;
        protected IInputService InputService;
        
        private Collider[] _colliders;
        
        public ToolData ToolData => _toolData;
        public bool IsLocked { get; protected set; }
        
        protected virtual void Awake()
        {
            InputService = ServiceLocator.Resolve<IInputService>();
            _colliders = GetComponentsInChildren<Collider>();
        }

        private void Start()
        {
            PlayerToolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();
        }

        public abstract void Use(Ray ray, bool pressed, float dt);
    }
}