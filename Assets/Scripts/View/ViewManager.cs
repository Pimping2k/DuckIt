using System;
using System.Collections.Generic;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;

namespace Gameplay.Core.View
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
        }

        private void Start()
        {
            foreach (var view in _viewMap.Values)
            {
                view.Hide();
                view.Initialize();
            }
        }

        /// <summary>
        /// General method to get and instant show selected view
        /// Closing previous open view
        /// </summary>
        public TView ShowView<TView>(ViewType type) where TView : BaseView
        {
            _currentView?.Hide();

            if (_viewMap.TryGetValue(type, out var view))
            {
                _currentView = view;
                _currentView.Show();
                return view as TView;
            }

            return null;
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
    }
}