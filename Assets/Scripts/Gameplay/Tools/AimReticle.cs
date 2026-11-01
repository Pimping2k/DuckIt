using System;
using UnityEngine;

namespace Gameplay.Tools
{
    public interface IRadiusSource
    {
        float Radius { get; }
        event Action<float> RadiusChanged;
    }

    [RequireComponent(typeof(LineRenderer))]
    public class AimReticle : MonoBehaviour
    {
        [SerializeField] private float _baseRadius = 0.05f;
        [SerializeField] private int _segments = 48;
        [SerializeField] private float _surfaceOffset = 0.005f;
        [SerializeField] private float _radiusLerpSpeed = 12f;
        [SerializeField] private float _lineWidth = 0.004f;

        private LineRenderer _line;
        private IRadiusSource _source;
        private float _targetRadius;
        private float _currentRadius;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = true;
            _line.widthMultiplier = _lineWidth;
            _line.positionCount = _segments;

            for (int i = 0; i < _segments; i++)
            {
                float a = i / (float)_segments * Mathf.PI * 2f;
                _line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f));
            }

            _targetRadius = _currentRadius = _baseRadius;
            Hide();
        }

        private void OnDestroy() => Unbind();

        private void Update()
        {
            if (!IsVisible) return;

            _currentRadius = Mathf.Lerp(_currentRadius, _targetRadius, Time.deltaTime * _radiusLerpSpeed);
            transform.localScale = Vector3.one * _currentRadius;
        }

        public void Show(Vector3 point, Vector3 normal)
        {
            if (!IsVisible)
            {
                IsVisible = true;
                _line.enabled = true;
            }

            transform.SetPositionAndRotation(
                point + normal * _surfaceOffset,
                Quaternion.LookRotation(-normal));
        }

        public void Hide()
        {
            IsVisible = false;
            if (_line) _line.enabled = false;
        }

        public void Bind(IRadiusSource source)
        {
            Unbind();
            _source = source;
            if (_source == null) return;

            _source.RadiusChanged += OnRadiusChanged;
            SetRadius(_source.Radius);
        }

        public void Unbind()
        {
            if (_source != null) _source.RadiusChanged -= OnRadiusChanged;
            _source = null;
        }

        public void SetRadius(float radius) => _targetRadius = Mathf.Max(0.001f, radius);

        public void ResetRadius() => SetRadius(_baseRadius);

        private void OnRadiusChanged(float radius)
        {
            // TODO: логика визуального увеличения радиуса (анимация, пульс, цвет и т.д.)
            SetRadius(radius);
        }
    }
}