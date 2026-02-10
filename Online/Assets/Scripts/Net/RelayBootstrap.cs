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
    public Button hostButton;
    public Button clientButton;

    public InputField joinCodeInput; 
    public Text joinCodeText;      

    public int maxPlayers = 5;       
    public string connectionType = "udp";

    public string gameSceneName = "Game";
    public GameObject menuRoot;

    private async void Awake()
    {
        hostButton.onClick.AddListener(() => _ = StartHostRelay());
        clientButton.onClick.AddListener(() => _ = StartClientRelay());

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

    private async Task StartHostRelay()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            if (JoinCodeStore.Instance != null)
                JoinCodeStore.Instance.SetCode(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new Unity.Networking.Transport.Relay.RelayServerData(alloc, connectionType));

            NetworkManager.Singleton.StartHost();

            if (menuRoot != null) menuRoot.SetActive(false);
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
            string joinCode = joinCodeInput != null ? joinCodeInput.text.Trim() : "";
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("Join code is empty.");
                return;
            }

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = new RelayServerData(allocation, connectionType);
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();

            NetworkManager.Singleton.StartClient();
            if (menuRoot != null) menuRoot.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogError("StartClientRelay failed: " + e);
        }
    }
}
