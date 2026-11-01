using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;

namespace Gameplay.PackageRelated.Steps
{
    public class OpenBoxStep : PackageStep
    {
        [SerializeField] private Animator _packageAnimator;
        
        public override async void Perform()
        {
            var token = this.GetCancellationTokenOnDestroy();
            
            _packageAnimator.SetTrigger(AnimationCache.Box.OPEN_TRIGGER);
            
            await UniTask.WaitForSeconds(2.5f, cancellationToken: token);
            
            if(token.IsCancellationRequested)
                return;
            
            Complete();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(!_packageAnimator)
                _packageAnimator = GetComponentInChildren<Animator>();
        }
#endif
    }
}