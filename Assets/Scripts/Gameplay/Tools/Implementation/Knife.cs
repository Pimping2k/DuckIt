using Gameplay.Cardboard;
using UnityEngine;

namespace Gameplay.Tools.Implementation
{
    public class Knife : BaseTool
    {
        private RaycastHit[] _hits = new RaycastHit[10];
        
        public override void Use(Ray ray, bool pressed, float dt)
        {
            var size = Physics.RaycastNonAlloc(ray, _hits, 100f);
            
            if(size <= 0)
                return;

            for (int i = 0; i < size; i++)
            {
                var hit = _hits[i];
                var item =  hit.collider;
                
                if(!item.TryGetComponent<BoxInteractable>(out var box))
                    continue;

                box.Interact();
            }
        }
    }
}