using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TojGamesTask.Modules.Lobby.Presentation
{
    public sealed class LobbyView : MonoBehaviour
    {
        [field: SerializeField] [field: Header("Login Panel")] public GameObject NamePanel { get; private set; }
        [field: SerializeField] public TMP_InputField NameInput { get; private set; }
        [field: SerializeField] public TMP_InputField LobbyCodeInput { get; private set; }
        [field: SerializeField] public Button JoinButton { get; private set; }
        [field: SerializeField] [field: Header("Lobby Panel")] public GameObject LobbyPanel { get; private set; }
        [field: SerializeField] public TextMeshProUGUI PlayerListText { get; private set; }
        [field: SerializeField] public TextMeshProUGUI PlayerCountText { get; private set; }
        [field: SerializeField] public TextMeshProUGUI LobbyCodeText { get; private set; }
        [field: SerializeField] public Button StartButton { get; private set; }
        [field: SerializeField] public Button LeaveButton { get; private set; }
    }
}