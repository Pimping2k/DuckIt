using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Interaction
{
    public class Outlinable : MonoBehaviour
    {
        [SerializeField] private List<Renderer> _outlinableRenderers;
        
        private Color _highlightedColor = Color.white;
        private Color _defaultColor = Color.black;
        private bool _debug;
        
        private readonly int _colorProperty = Shader.PropertyToID("_OutlineColor");
        
        public void ChangeOutline(bool isHighlighted)
        {
            foreach (var rend in _outlinableRenderers)
            {
                if (!rend) 
                    continue;

                rend.material.SetColor(_colorProperty, isHighlighted ? _highlightedColor : _defaultColor);
            }
        }

        [ContextMenu("Toggle Highlight (Debug)")]
        private void ToggleHighlightDebug()
        {
            _debug = !_debug;
            ChangeOutline(_debug);
        }
    }
}