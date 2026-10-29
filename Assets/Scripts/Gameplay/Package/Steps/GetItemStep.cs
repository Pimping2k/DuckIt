using Data;
using DG.Tweening;
using Extensions;
using Gameplay.Cardboard;
using MyPackage.Runtime.ServiceLocator_Core;

namespace Gameplay.Package.Steps
{
    public class GetItemStep : PackageStep
    {
        private PlacementManager _placementManager;
        
        protected override void OnInitialized()
        {
            _placementManager = ServiceLocator.Resolve<PlacementManager>();
        }

        public override void Perform()
        {
            Animations.RestorableItems.PopItemFromBox(Package.Item.transform, Package.ItemPosition,
                _placementManager.ItemPositions.GetRandomValue()).OnComplete(Complete);
        }
    }
}