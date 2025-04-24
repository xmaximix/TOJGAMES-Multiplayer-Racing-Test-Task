using System.Threading;
using R3;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TojGamesTask.Common.Extensions
{
    public static class ButtonExtensions
    {
        public static Observable<Unit> OnClickAsObservable(
            this Button button,
            CancellationToken ct = default)
        {
            return Observable.FromEvent<UnityAction, Unit>(
                handler => () => handler(Unit.Default),
                h       => button.onClick.AddListener(h),
                h       => button.onClick.RemoveListener(h),
                ct     
            );
        }
    }
}