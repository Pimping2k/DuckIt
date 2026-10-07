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
        
        public ToolType Type => _type;
        
        private void Awake()
        {
            InputService = ServiceLocator.Resolve<IInputService>();
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

        public virtual void OnPickedUp()
        {
            PlayerToolController.SetTool(this);
        }

        public virtual void OnDropped()
        {
            PlayerToolController.SetTool(null);
        }

        public abstract void Use(Ray ray, bool pressed, float dt);
    }
}