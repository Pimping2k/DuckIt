using System;
using Cysharp.Threading.Tasks;
using Data;
using MyPackage.Runtime.ServiceLocator_Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bootstrap
{
    public class BootstrapManager : MonoBehaviour, IService
    {
        public bool IsReady { get; private set; }
        public event Action<float> ProgressChanged;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            IsReady = true;
        }

        private void Start()
        {
            StartLoading().Forget();
        }

        private async UniTaskVoid StartLoading()
        {
            var token = gameObject.GetCancellationTokenOnDestroy();

            var loadOp = SceneManager.LoadSceneAsync(Tags.Scenes.GAMEPLAY, LoadSceneMode.Additive);
            loadOp.allowSceneActivation = true;

            while (!loadOp.isDone)
            {
                if (token.IsCancellationRequested) return;
                ProgressChanged?.Invoke(loadOp.progress);
                await UniTask.NextFrame(cancellationToken: token);
            }

            var mainScene = SceneManager.GetSceneByName(Tags.Scenes.GAMEPLAY);
            SceneManager.SetActiveScene(mainScene);

            await SceneManager.UnloadSceneAsync(Tags.Scenes.BOOTSTRAP);
        }
    }
}