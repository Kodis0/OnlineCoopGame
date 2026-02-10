using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public Text playersListText;
    public Button readyButton;
    public Button startButton;

    private LobbyManager lobby;
    private bool localReady;

    private void Start()
    {
        lobby = FindFirstObjectByType<LobbyManager>();

        if (readyButton != null) readyButton.onClick.AddListener(ToggleReady);
        if (startButton != null) startButton.onClick.AddListener(StartGame);

        UpdateReadyButtonText();
        InvokeRepeating(nameof(RefreshUI), 0.2f, 0.2f);
    }

    private void RefreshUI()
    {
        if (NetworkManager.Singleton == null) return;

        bool isHost = NetworkManager.Singleton.IsHost;

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isHost);
            startButton.interactable = isHost && lobby != null && lobby.AreAllReady();
        }

        if (playersListText != null)
            playersListText.text = BuildPlayersText();
    }

    private string BuildPlayersText()
    {
        if (lobby == null || lobby.Players == null)
            return "PLAYERS:\n(loading...)";

        var sb = new StringBuilder();
        sb.AppendLine("PLAYERS:");

        ulong myId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

        for (int i = 0; i < lobby.Players.Count; i++)
        {
            var p = lobby.Players[i];
            string mark = p.Ready ? "✅" : "❌";
            string me = (p.ClientId == myId) ? " (YOU)" : "";
            sb.AppendLine($"{mark} Player {p.ClientId}{me}");
        }

        sb.AppendLine();
        sb.AppendLine("Everyone must be ✅");
        return sb.ToString();
    }

    private void ToggleReady()
    {
        localReady = !localReady;
        UpdateReadyButtonText();

        if (lobby != null)
            lobby.SetReadyServerRpc(localReady);
    }

    private void UpdateReadyButtonText()
    {
        if (readyButton == null) return;

        var t = readyButton.GetComponentInChildren<Text>();
        if (t != null) t.text = localReady ? "READY ✅" : "READY ❌";
    }

    private void StartGame()
    {
        if (lobby != null)
            lobby.StartGameServerRpc();
    }
}
