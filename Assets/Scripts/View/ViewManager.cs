using System;
using System.Collections.Generic;
using EventSub;
using EventSub.Implementation;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace View
{
    public class ViewManager : MonoBehaviour, IService
    {
        [SerializeField] private List<BaseView> _views;
        
        private readonly Dictionary<ViewType, BaseView> _viewMap = new();
        private BaseView _currentView;

        private void Awake()
        {
            foreach (var view in _views)
            {
                if (!view) 
                    continue;
                
                _viewMap[view.ViewType] = view;
            }
            
            EventBus.Subscribe<RequestViewEvent>(OnRequestedViewEvent);
        }

        private void Start()
        {
            foreach (var view in _viewMap.Values)
            {
                view.Hide();
                view.Initialize();
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<RequestViewEvent>(OnRequestedViewEvent);
        }

        public TView ShowView<TView>(ViewType type, object[] payload = null) where TView : BaseView
        {
            return ShowView(type, payload) as TView;
        }
        
        /// <summary>
        /// General method to get and instant show selected view
        /// Closing previous open view
        /// </summary>
        public BaseView ShowView(ViewType type, object[] payload = null)
        {
            if (!_viewMap.TryGetValue(type, out var view))
            {
                Debug.LogError($"Вьюха {type} не зарегистрирована в ViewManager", this);
                return null;
            }

            if (_currentView) _currentView.Hide();
            _currentView = view;
            view.Setup(payload);
            view.Show();
            return view;
        }

        public void HideViewByType(ViewType type)
        {
            _viewMap.TryGetValue(type, out var view);
            view?.Hide();
        }

        /// <summary>
        /// Hides current opened view
        /// </summary>
        public void HideCurrentView()
        {
            _currentView?.Hide();
            _currentView = null;
        }

        private void OnRequestedViewEvent(RequestViewEvent e)
        {
            ShowView(e.ViewType, e.Payload);
        }
    }
}