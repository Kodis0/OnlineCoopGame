using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetHealth : NetworkBehaviour
{
    public int maxHp = 100;

    public NetworkVariable<int> Hp = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private SpawnPoints spawns;
    private CharacterController cc;

    public override void OnNetworkSpawn()
    {
        cc = GetComponent<CharacterController>();
        if (!IsServer) return;

        Hp.Value = maxHp;
        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        for (int i = 0; i < 60; i++)
        {
            spawns = FindFirstObjectByType<SpawnPoints>();
            if (spawns != null) break;
            yield return null;
        }

        MoveToSpawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DealDamageServerRpc(int damage, ulong attackerClientId)
    {
        if (!IsServer) return;
        if (Hp.Value <= 0) return;

        Hp.Value = Mathf.Max(0, Hp.Value - damage);

        if (Hp.Value == 0)
        {
            Hp.Value = maxHp;
            MoveToSpawn();
        }
    }

    private void MoveToSpawn()
    {
        Vector3 p = Vector3.zero;

        if (spawns != null)
        {
            p = spawns.GetSpawnForClient(OwnerClientId);
        }

        Vector3 start = p + Vector3.up * 50f;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            p = hit.point + Vector3.up * 0.1f;

        if (cc != null) cc.enabled = false;
        transform.position = p;
        if (cc != null) cc.enabled = true;
    }
}
