using System;
using MyPackage.Runtime.ServiceLocator_Core;
using Player;
using Services;
using UnityEngine;

namespace Gameplay.Tools
{
    public enum ToolType
    {
        None = 0,
        Knife = 1,
        Brush = 2,
        Cloth = 3,
        Scraper = 4,
        Sandpaper = 5,
        Glue = 6,
        Screwdriver = 7,
    }
    
    public abstract class BaseTool : MonoBehaviour
    {
        [SerializeField] private ToolType _type;
        
        protected PlayerToolController PlayerToolController;
        protected IInputService InputService;
        
        public ToolType Type => _type;
        
        private void Awake()
        {
            InputService = ServiceLocator.Resolve<IInputService>();
            PlayerToolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();
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