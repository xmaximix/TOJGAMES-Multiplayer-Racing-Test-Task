using Cysharp.Threading.Tasks;
using TojGamesTask.Common.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace TojGamesTask.Modules.Load.Bootstrap
{
    public class LoaderBootstrap : IInitializable
    {
        private readonly ISceneService sceneService;

        [Inject]
        public LoaderBootstrap(ISceneService sceneService) => this.sceneService = sceneService;

        public void Initialize()
        {
            sceneService.LoadSceneAsync("Lobby").Forget();
        }
    }
}