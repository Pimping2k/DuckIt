using Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UILoadingScreen : MonoBehaviour
    {
        [SerializeField] private Image _loadingBar;
        [SerializeField] private BootstrapManager _bootstrapManager;
        
        private void Awake()
        {
            _loadingBar.fillAmount = 0;
            _bootstrapManager.ProgressChanged += OnProgressChanged;
        }

        private void OnDestroy()
        {
            _bootstrapManager.ProgressChanged -= OnProgressChanged;
        }

        private void OnProgressChanged(float progress)
        {
            _loadingBar.fillAmount = progress/0.9f;
        }
    }
}