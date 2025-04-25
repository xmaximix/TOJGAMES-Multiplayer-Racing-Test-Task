using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Configs
{
    [CreateAssetMenu(menuName = "Configs/Countdown Config")]
    public class CountdownConfig : ScriptableObject
    {
        [field: SerializeField] public int CountdownSeconds { get; private set; } = 3;
    }
}