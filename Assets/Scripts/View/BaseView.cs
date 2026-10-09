using System;
using Gameplay.Core.View;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace View
{
    public class BaseView : MonoBehaviour, IView, IDisposable
    {
        [SerializeField] private ViewType _viewType;
        [SerializeField] private bool _closeWithBackButton;
        
        public ViewType ViewType => _viewType;
        
        protected ViewManager ViewManager;
        protected IInputService InputService;

        protected object[] Payload = {};
        
        protected virtual void Start()
        {
            ViewManager = ServiceLocator.Resolve<ViewManager>();
            InputService = ServiceLocator.Resolve<IInputService>();
            
            if (_closeWithBackButton)
                InputService.UI.Cancel.performed += OnBackPerformed;
        }

        private void OnDestroy()
        {
            if (_closeWithBackButton)
                InputService.UI.Cancel.performed -= OnBackPerformed;
        }

        public void Setup(object[] payload)
        {
            OnRequested(payload);
        }
        
        public void Initialize()
        {
            OnInitialized();
        }

        public void Show()
        {
            InputService.ChangeMap(MapType.UI);
            gameObject.SetActive(true);
            OnShown();
        }

        public void Hide()
        {
            InputService.ChangeMap(MapType.Gameplay);
            gameObject.SetActive(false);
            OnHide();
        }

        public void Dispose()
        {
            OnDisposed();
        }

        protected virtual void OnDisposed(){}
        protected virtual void OnInitialized() { }
        protected virtual void OnShown() { }
        protected virtual void OnHide() { }
        protected virtual void OnRequested(params object[] payload) { }
        
        private void OnBackPerformed(InputAction.CallbackContext obj)
        {
            ViewManager.HideCurrentView();
        }
    }

    public abstract class BaseView<TView> : BaseView where TView : BaseView<TView> { }
}