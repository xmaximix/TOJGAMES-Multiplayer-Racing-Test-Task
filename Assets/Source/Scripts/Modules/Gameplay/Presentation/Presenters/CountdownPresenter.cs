using System;
using R3;
using TojGamesTask.Modules.Gameplay.Core;
using TojGamesTask.Modules.Gameplay.Network;
using TojGamesTask.Modules.Gameplay.Presentation.Views;
using VContainer;
using VContainer.Unity;

namespace TojGamesTask.Modules.Gameplay.Presentation.Presenters
{
    public sealed class CountdownPresenter : IStartable, IDisposable
    {
        private readonly CountdownView view;
        private readonly CountdownService svc;
        private readonly CompositeDisposable d = new();

        public CountdownPresenter(CountdownView view, CountdownService svc)
        {
            this.view = view;
            this.svc = svc;
        }

        public void Start()
        {
            view.Panel.SetActive(true);

            svc.Ticks
                .Subscribe(n => view.Text.text = n.ToString())
                .AddTo(d);

            svc.Finished
                .Subscribe(_ => view.Panel.SetActive(false))
                .AddTo(d);
        }

        public void Dispose()
        {
            d.Dispose();
        }
    }
}