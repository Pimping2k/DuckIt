using MyPackage.Runtime.ServiceLocator_Core;
using Services;
using UnityEngine;

namespace Restorable
{
    /// <summary>
    /// Тестовая кисть мышью. 1 = тряпка, 2 = скребок/шкурка, 3 = краска, 4 = лак.
    /// Использует старый Input Manager. Если включена только новая Input System, замени чтение мыши.
    /// </summary>
    public class RestorationBrush : MonoBehaviour
    {
        public Camera cam;
        public LayerMask paintLayer = ~0;
        public RestoreTool tool = RestoreTool.Clean;
        [Range(0.005f, 0.2f)] public float uvRadius = 0.04f;
        public float ratePerSecond = 2.5f;

        void Awake()
        {
            cam = ServiceLocator.Resolve<CameraService>().CameraBrain.GetComponent<Camera>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) 
                tool = RestoreTool.Clean;
            if (Input.GetKeyDown(KeyCode.Alpha2)) 
                tool = RestoreTool.Scrape;
            if (Input.GetKeyDown(KeyCode.Alpha3)) 
                tool = RestoreTool.Paint;
            if (Input.GetKeyDown(KeyCode.Alpha4)) 
                tool = RestoreTool.Varnish;

            if (!Input.GetMouseButton(0)) return;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f, paintLayer))
            {
                
                var item = hit.collider.GetComponentInParent<RestorableItem>(); 
                if (item != null)
                {
                    item.Stroke(item.GetUV(hit), uvRadius, tool, ratePerSecond * Time.deltaTime);
                }
            }
        }
    }
}
