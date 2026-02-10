using System;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    [SerializeField] private int minPlayersToStart = 1;

    public bool CanStart()
    {
        if (Players.Count < minPlayersToStart) return false;
        for (int i = 0; i < Players.Count; i++)
            if (!Players[i].Ready) return false;
        return true;
    }

    [Serializable]
    public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
    {
        public ulong ClientId;
        public bool Ready;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Ready);
        }

        public bool Equals(LobbyPlayer other) => ClientId == other.ClientId && Ready == other.Ready;
    }

    public NetworkList<LobbyPlayer> Players;

    [SerializeField] private string gameSceneName = "Game";

    private void Awake()
    {
        Players = new NetworkList<LobbyPlayer>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        AddOrReset(NetworkManager.Singleton.LocalClientId);

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            AddOrReset(kv.Key);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId) => AddOrReset(clientId);

    private void OnClientDisconnected(ulong clientId)
    {
        for (int i = Players.Count - 1; i >= 0; i--)
            if (Players[i].ClientId == clientId)
                Players.RemoveAt(i);
    }

    private void AddOrReset(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                var p = Players[i];
                p.Ready = false;
                Players[i] = p;
                return;
            }
        }

        Players.Add(new LobbyPlayer { ClientId = clientId, Ready = false });
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
    {
        ulong cid = rpcParams.Receive.SenderClientId;

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == cid)
            {
                var p = Players[i];
                p.Ready = ready;
                Players[i] = p;
                return;
            }
        }

        Players.Add(new LobbyPlayer { ClientId = cid, Ready = ready });
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (rpcParams.Receive.SenderClientId != NetworkManager.Singleton.LocalClientId) return;
        if (!CanStart()) return;

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
