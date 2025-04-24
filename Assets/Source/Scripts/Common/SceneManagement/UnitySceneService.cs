using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace TojGamesTask.Common.SceneManagement
{
    public class UnitySceneService : ISceneService
    {
        public async UniTask LoadSceneAsync(string sceneName)
        {
            var target = SceneManager.GetSceneByName(sceneName);
            if (target.isLoaded) return;

            await SceneManager.LoadSceneAsync(sceneName).ToUniTask();
            await UniTask.WaitUntil(() => SceneManager.GetActiveScene() == target);
        }
    }
}