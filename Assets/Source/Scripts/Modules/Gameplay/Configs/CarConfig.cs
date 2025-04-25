using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Configs
{
    [CreateAssetMenu(menuName = "Configs/Car Config")]
    public class CarConfig : ScriptableObject
    {
        [field: SerializeField] [field: Header("Drive Settings")]
        public float MaxSpeed { get; private set; } = 20f;

        [field: SerializeField] public float Acceleration { get; private set; } = 10f;

        [field: SerializeField] public float TurnSpeed { get; private set; } = 90f;
    }
}