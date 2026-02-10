using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using Unity.Networking.Transport.Relay;

public class RelayBootstrap : MonoBehaviour
{
    [Header("Buttons")]
    public Button hostButton;
    public Button clientButton;

    [Header("Join Code Input (client)")]
    public InputField joinCodeInput;

    [Header("UI Panels")]
    public GameObject panelConnect;
    public GameObject panelLobby;

    [Header("Relay")]
    public int maxPlayers = 5;
    public string connectionType = "udp";

    public LobbyManager lobbyManagerInScene;
    public NetworkObject lobbyManagerPrefab;

    private async void Awake()
    {
        if (panelConnect != null) panelConnect.SetActive(true);
        if (panelLobby != null) panelLobby.SetActive(false);

        if (hostButton != null) hostButton.onClick.AddListener(() => _ = StartHostRelay());
        if (clientButton != null) clientButton.onClick.AddListener(() => _ = StartClientRelay());

        await InitServices();
    }

    private async Task InitServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Services init/auth failed: " + e);
        }
    }

    private void ShowLobbyUI()
    {
        if (panelConnect != null) panelConnect.SetActive(false);
        if (panelLobby != null) panelLobby.SetActive(true);
    }

    private async Task StartHostRelay()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            if (JoinCodeStore.Instance != null)
                JoinCodeStore.Instance.SetCode(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(alloc, connectionType));

            NetworkManager.Singleton.StartHost();

            if (lobbyManagerPrefab != null)
            {
                var existing = FindFirstObjectByType<LobbyManager>();
                if (existing == null)
                {
                    var no = Instantiate(lobbyManagerPrefab);
                    no.Spawn(true);
                }
            }

            var lm = FindFirstObjectByType<LobbyManager>();
            if (lm != null)
            {
                var no = lm.GetComponent<NetworkObject>();
                if (no != null && !no.IsSpawned)
                    no.Spawn();
            }

            ShowLobbyUI();
        }
        catch (Exception e)
        {
            Debug.LogError("StartHostRelay failed: " + e);
        }
    }

    private async Task StartClientRelay()
    {
        try
        {
            string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogError("Join code is empty.");
                return;
            }

            JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(code);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(alloc, connectionType));

            NetworkManager.Singleton.StartClient();

            ShowLobbyUI();
        }
        catch (Exception e)
        {
            Debug.LogError("StartClientRelay failed: " + e);
        }
    }
}
