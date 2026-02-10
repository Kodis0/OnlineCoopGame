using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text playersListText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startButton;

    [Header("Text")]
    [SerializeField] private string loadingText = "LOBBY\n<waiting for network...>";
    [SerializeField] private string noLobbyText = "LOBBY\n<waiting for LobbyManager...>";

    private LobbyManager lobby;

    private void Awake()
    {
        if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
    }

    private void OnEnable()
    {
        // Подпишемся на появление/исчезновение клиентов (на всякий случай)
        TryHookNetworkCallbacks();
        // Попробуем сразу найти лобби и подписаться на список
        TryBindLobby();

        RefreshUI();
    }

    private void OnDisable()
    {
        UnbindLobby();
        UnhookNetworkCallbacks();
    }

    private void TryHookNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnAnyClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnAnyClientChanged;
    }

    private void UnhookNetworkCallbacks()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnAnyClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnAnyClientChanged;
    }

    private void OnAnyClientChanged(ulong _)
    {
        // При подключениях/отключениях иногда LobbyManager спавнится позже — перепривяжемся
        TryBindLobby();
        RefreshUI();
    }

    private void TryBindLobby()
    {
        if (lobby != null) return;

        lobby = FindFirstObjectByType<LobbyManager>();
        if (lobby == null) return;

        // Подпишемся на изменения NetworkList
        if (lobby.Players != null)
            lobby.Players.OnListChanged += OnPlayersListChanged;

        RefreshUI();
    }

    private void UnbindLobby()
    {
        if (lobby == null) return;

        if (lobby.Players != null)
            lobby.Players.OnListChanged -= OnPlayersListChanged;

        lobby = null;
    }

    private void OnPlayersListChanged(NetworkListEvent<LobbyManager.LobbyPlayer> _)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        // 1) Сеть ещё не поднята
        if (NetworkManager.Singleton == null)
        {
            SetButtonsEnabled(false);
            SetText(loadingText);
            return;
        }

        // 2) Лобби могло появиться позже — попробуем подцепиться
        if (lobby == null)
            TryBindLobby();

        // 3) Если лобби всё ещё нет — показываем статус
        if (lobby == null || lobby.Players == null)
        {
            SetButtonsEnabled(false);
            SetText(noLobbyText);
            return;
        }

        bool isHost = NetworkManager.Singleton.IsHost;
        bool isClient = NetworkManager.Singleton.IsClient;
        ulong myId = NetworkManager.Singleton.LocalClientId;

        bool myReady = GetMyReady(myId);
        UpdateReadyButtonVisual(myReady);

        // Start кнопка только у хоста
        if (startButton != null)
        {
            startButton.gameObject.SetActive(isHost);
            startButton.interactable = isHost && lobby.CanStart();
        }

        // Ready доступна всем клиентам (включая хоста), когда сеть активна
        if (readyButton != null)
            readyButton.interactable = isClient;

        // Текст списка игроков
        SetText(BuildPlayersText(myId, isHost));
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (readyButton != null) readyButton.interactable = enabled;
        if (startButton != null) startButton.interactable = enabled;
    }

    private void SetText(string text)
    {
        if (playersListText != null)
            playersListText.text = text;
    }

    private bool GetMyReady(ulong myId)
    {
        for (int i = 0; i < lobby.Players.Count; i++)
        {
            if (lobby.Players[i].ClientId == myId)
                return lobby.Players[i].Ready;
        }
        return false;
    }

    private void UpdateReadyButtonVisual(bool isReady)
    {
        if (readyButton == null) return;

        var t = readyButton.GetComponentInChildren<Text>();
        if (t != null) t.text = isReady ? "READY ✅" : "READY ❌";
    }

    private string BuildPlayersText(ulong myId, bool isHost)
    {
        var sb = new StringBuilder(256);

        sb.AppendLine("LOBBY");
        sb.AppendLine(isHost ? "Role: HOST" : "Role: CLIENT");
        sb.AppendLine();

        sb.AppendLine($"Players: {lobby.Players.Count}");
        sb.AppendLine(lobby.CanStart() ? "Status: ✅ Can start" : "Status: ⏳ Waiting");
        sb.AppendLine();

        sb.AppendLine("PLAYERS:");
        for (int i = 0; i < lobby.Players.Count; i++)
        {
            var p = lobby.Players[i];
            string mark = p.Ready ? "✅" : "❌";
            string you = p.ClientId == myId ? " (YOU)" : "";
            int number = i + 1; // Player 1..N
            sb.AppendLine($"{mark} Player {number}{you}");
        }

        sb.AppendLine();
        sb.AppendLine("Rule: everyone must be ✅");
        // Если хочешь красиво показывать minPlayersToStart — сделай в LobbyManager public getter и выведем тут.

        return sb.ToString();
    }

    private void OnReadyClicked()
    {
        if (NetworkManager.Singleton == null) return;

        if (lobby == null)
            TryBindLobby();

        if (lobby == null) return;

        // Не храним localReady — берём истинное значение с сервера и переключаем
        bool current = GetMyReady(NetworkManager.Singleton.LocalClientId);
        lobby.SetReadyServerRpc(!current);
    }

    private void OnStartClicked()
    {
        if (NetworkManager.Singleton == null) return;

        if (lobby == null)
            TryBindLobby();

        if (lobby == null) return;

        lobby.StartGameServerRpc();
    }
}
