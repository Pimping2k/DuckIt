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
    
    public abstract class BaseTool : MonoBehaviour
    {
        [SerializeField] private ToolData _toolData;
        [SerializeField] private ToolType _type;
        [SerializeField] private RestoreTool _restoreToolType;
        
        protected PlayerToolController PlayerToolController;
        protected IInputService InputService;
        
        private Collider[] _colliders;
        
        public ToolData ToolData => _toolData;
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

        public abstract void Use(Ray ray, bool pressed, float dt);
    }
}