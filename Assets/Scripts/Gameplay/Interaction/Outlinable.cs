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
        
        private MaterialPropertyBlock _block;
        private readonly int _colorProperty = Shader.PropertyToID("_OutlineColor");
        
        public void ChangeOutline(bool isHighlighted)
        {
            _block ??= new MaterialPropertyBlock();
            var color = isHighlighted ? _highlightedColor : _defaultColor;

            foreach (var rend in _outlinableRenderers)
            {
                if (!rend) continue;
                rend.GetPropertyBlock(_block);
                _block.SetColor(_colorProperty, color);
                rend.SetPropertyBlock(_block);
            }
        }

        [ContextMenu("Toggle Highlight (Debug)")]
        private void ToggleHighlightDebug()
        {
            _debug = !_debug;
            ChangeOutline(_debug);
        }

        private void OnValidate()
        {
            if(_outlinableRenderers == null || _outlinableRenderers.Count == 0)
                GetComponentsInChildren(true, _outlinableRenderers);
        }
    }
}