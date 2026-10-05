using System;
using Player;
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
        
        private PlayerToolController _playerToolController;
        
        public ToolType Type => _type;
        
        private void Awake()
        {
            _playerToolController = LocalPlayer.Instance.GetPlayerComponent<PlayerToolController>();
        }

        public virtual void OnPickedUp()
        {
            _playerToolController.SetTool(this);
        }

        public virtual void OnDropped()
        {
            _playerToolController.SetTool(null);
        }
        
        public abstract void Use(Ray ray, bool pressed, float dt);
    }
}