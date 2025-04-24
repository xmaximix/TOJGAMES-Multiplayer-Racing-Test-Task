using VContainer;
using VContainer.Unity;

namespace TojGamesTask.Common.DI
{
    public abstract class BaseLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            foreach (var installer in GetComponentsInChildren<IInstaller>(true)) 
                installer.Install(builder);
        }
    }
}