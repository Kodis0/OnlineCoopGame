using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkBootstrap : MonoBehaviour
{
    [Header("UI")]
    public Button hostButton;
    public Button clientButton;
    public InputField ipInput;   

    [Header("Scene")]
    public string gameSceneName = "Game";

    private void Awake()
    {
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
    }

    private void StartHost()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777);

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void StartClient()
    {
        string ip = (ipInput != null && !string.IsNullOrWhiteSpace(ipInput.text)) ? ipInput.text.Trim() : "127.0.0.1";

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, 7777);

        NetworkManager.Singleton.StartClient();
    }
}
