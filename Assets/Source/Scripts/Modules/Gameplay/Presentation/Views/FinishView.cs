using TMPro;
using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Presentation.Views
{
    public class FinishView : MonoBehaviour
    {
        [field: SerializeField] public GameObject Panel { get; private set; }
        [field: SerializeField] public TextMeshProUGUI ResultsText { get; private set; }
    }
}