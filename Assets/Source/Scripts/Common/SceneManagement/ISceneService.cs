using Cysharp.Threading.Tasks;

namespace TojGamesTask.Common.SceneManagement
{
    public interface ISceneService
    {
        UniTask LoadSceneAsync(string sceneName);
    }
}