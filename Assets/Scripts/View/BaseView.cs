using System;
using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Core.View
{
    public class BaseView : MonoBehaviour, IView, IDisposable
    {
        [SerializeField] private bool _closeWithBackButton;
    
        protected ViewManager ViewManager;
        protected IInputService InputService;
        
        protected virtual void Start()
        {
            ViewManager = ServiceLocator.Resolve<ViewManager>();
            InputService = ServiceLocator.Resolve<IInputService>();
            
            if (_closeWithBackButton)
            {
                InputService.UI.Cancel.performed += OnBackPerformed;
            }
        }

        private void OnDestroy()
        {
            if (_closeWithBackButton)
            {
                InputService.UI.Cancel.performed -= OnBackPerformed;
            }
        }

        public virtual void Initialize()
        {
            OnInitialized();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnShown();
        }

        public virtual void Hide()
        {
            OnHide();
            gameObject.SetActive(false);
        }

        public void Dispose()
        {
            OnDisposed();
        }

        public ViewType ViewType { get; }
        protected virtual void OnDisposed(){}
        protected virtual void OnInitialized() { }
        protected virtual void OnShown() { }
        protected virtual void OnHide() { }

        private void OnBackPerformed(InputAction.CallbackContext obj)
        {
            ViewManager.HideCurrentView();
        }
    }

    public abstract class BaseView<TView> : BaseView where TView : BaseView<TView> { }
}