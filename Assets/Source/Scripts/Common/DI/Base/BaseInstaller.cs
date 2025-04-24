using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TojGamesTask.Common.DI
{
    public abstract class BaseInstaller : MonoBehaviour, IInstaller
    {
        public abstract void Install(IContainerBuilder builder);
    }
}