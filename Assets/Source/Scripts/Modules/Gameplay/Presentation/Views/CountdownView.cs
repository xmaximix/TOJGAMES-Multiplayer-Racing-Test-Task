using TMPro;
using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Presentation.Views
{
    public class CountdownView : MonoBehaviour
    {
        [field:SerializeField] public GameObject Panel { get; private set; }
        [field:SerializeField] public TextMeshProUGUI Text { get; private set; }
    }
}