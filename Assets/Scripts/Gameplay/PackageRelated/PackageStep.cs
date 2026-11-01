using System;
using UnityEngine;

namespace Gameplay.PackageRelated
{
    public abstract class PackageStep : MonoBehaviour
    {
        public event Action<PackageStep> Completed;

        public Package Package { get; private set; }

        public void Initialize(Package package)
        {
            Package = package;
            OnInitialized();
        }

        public virtual bool CanPerform() => true;
        public abstract void Perform();
        
        protected virtual void OnInitialized(){}
        protected virtual void OnComplete(){}
        protected virtual void Complete()
        {
            OnComplete();
            Completed?.Invoke(this);
        }
    }
}