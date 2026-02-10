using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetSpawnManager : NetworkBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private SpawnPoints spawnPoints;

    [Header("Limits")]
    [SerializeField] private int maxPlayers = 5;

    private readonly Dictionary<ulong, int> assigned = new Dictionary<ulong, int>();
    private bool[] used;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        if (spawnPoints == null)
            spawnPoints = FindFirstObjectByType<SpawnPoints>();

        int count = (spawnPoints != null && spawnPoints.points != null) ? spawnPoints.points.Length : 0;
        count = Mathf.Min(count, maxPlayers);
        used = new bool[Mathf.Max(1, count)];

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            OnClientConnected(kv.Key);
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

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.Count > maxPlayers)
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        if (assigned.ContainsKey(clientId)) return;

        int index = GetRandomFreeSpawnIndex();
        assigned[clientId] = index;
        if (index >= 0 && index < used.Length) used[index] = true;

        StartCoroutine(SpawnWhenPlayerReady(clientId, index));
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (assigned.TryGetValue(clientId, out int index))
        {
            if (index >= 0 && index < used.Length) used[index] = false;
            assigned.Remove(clientId);
        }
    }

    private IEnumerator SpawnWhenPlayerReady(ulong clientId, int spawnIndex)
    {
        NetworkObject playerObj = null;
        float t = 0f;

        while (playerObj == null && t < 3f)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
                playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

            t += Time.deltaTime;
            yield return null;
        }

        if (playerObj == null) yield break;

        Vector3 pos = GetSpawnPosition(spawnIndex);

        TeleportLocal(playerObj.gameObject, pos);
        TeleportPlayerClientRpc(clientId, pos);
    }

    private Vector3 GetSpawnPosition(int spawnIndex)
    {
        if (spawnPoints == null)
            return Vector3.zero;

        return spawnPoints.GetSpawnByIndex(spawnIndex);
    }

    private int GetRandomFreeSpawnIndex()
    {
        if (used == null || used.Length == 0) return 0;

        List<int> free = new List<int>(used.Length);
        for (int i = 0; i < used.Length; i++)
            if (!used[i]) free.Add(i);

        if (free.Count == 0)
            return Random.Range(0, used.Length);

        return free[Random.Range(0, free.Count)];
    }

    private static void TeleportLocal(GameObject go, Vector3 pos)
    {
        var cc = go.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        go.transform.position = pos;

        if (cc != null) cc.enabled = true;
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(ulong clientId, Vector3 pos)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            return;

        var playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (playerObj == null) return;

        TeleportLocal(playerObj.gameObject, pos);
    }
}
