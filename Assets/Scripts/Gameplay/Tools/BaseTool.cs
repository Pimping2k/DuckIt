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
    }
    
    public abstract class BaseTool : Interactable
    {
        [SerializeField] private ToolType _type;
        [SerializeField] private RestoreTool _restoreToolType;
        
        protected PlayerToolController PlayerToolController;
        protected IInputService InputService;
        
        private Collider[] _colliders;
        
        public ToolType Type => _type;
        
        protected virtual void Awake()
        {
            InputService = ServiceLocator.Resolve<IInputService>();
            _colliders = GetComponentsInChildren<Collider>();
        }

        private void Start()
        {
            PlayerToolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();
        }

        protected override void OnInteracted()
        {
            if(PlayerToolController.CurrentTool?.Type == _type)
                return;
            
            PlayerToolController.CurrentTool?.OnDropped();
            OnPickedUp();
        }

        private void SetPhysicsActive(bool active)
        {
            foreach (var c in _colliders) 
                c.enabled = active;
        }

        public virtual void OnPickedUp()
        {
            SetPhysicsActive(false);
            PlayerToolController.SetTool(this);
        }

        public virtual void OnDropped()
        {
            PlayerToolController.SetTool(null);
            SetPhysicsActive(true);
        }

        public abstract void Use(Ray ray, bool pressed, float dt);
    }
}