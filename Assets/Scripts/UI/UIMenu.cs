using Bootstrap;
using Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class UIMenu : MonoBehaviour
    {
        [SerializeField] private Button _playButton;
        
        private void Awake()
        {
            _playButton.onClick.AddListener(OnPlay);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveAllListeners();
        }

        private void OnPlay()
        {
            SceneManager.LoadScene(Tags.Scenes.BOOTSTRAP, LoadSceneMode.Single);
        }
    }
}