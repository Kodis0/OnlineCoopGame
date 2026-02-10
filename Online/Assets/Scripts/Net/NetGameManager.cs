using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetGameManager : NetworkBehaviour
{
    [Header("Match")]
    public float matchDurationSeconds = 90f;

    public NetworkVariable<float> TimeLeft = new NetworkVariable<float>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> MatchRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkList<ScoreEntry> Scoreboard;

    private void Awake()
    {
        Scoreboard = new NetworkList<ScoreEntry>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartMatchServer();
        }
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!MatchRunning.Value) return;

        TimeLeft.Value -= Time.deltaTime;
        if (TimeLeft.Value <= 0f)
        {
            TimeLeft.Value = 0f;
            MatchRunning.Value = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < Scoreboard.Count; i++)
            if (Scoreboard[i].ClientId == clientId) return;

        Scoreboard.Add(new ScoreEntry { ClientId = clientId, Kills = 0 });
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddKillServerRpc(ulong killerClientId)
    {
        if (!IsServer) return;
        if (!MatchRunning.Value) return;

        for (int i = 0; i < Scoreboard.Count; i++)
        {
            if (Scoreboard[i].ClientId == killerClientId)
            {
                var e = Scoreboard[i];
                e.Kills += 1;
                Scoreboard[i] = e;
                return;
            }
        }

        Scoreboard.Add(new ScoreEntry { ClientId = killerClientId, Kills = 1 });
    }

    private void StartMatchServer()
    {
        TimeLeft.Value = matchDurationSeconds;
        MatchRunning.Value = true;
    }

    [Serializable]
    public struct ScoreEntry : INetworkSerializable, IEquatable<ScoreEntry>
    {
        public ulong ClientId;
        public int Kills;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Kills);
        }

        public bool Equals(ScoreEntry other) => ClientId == other.ClientId && Kills == other.Kills;
    }
}
