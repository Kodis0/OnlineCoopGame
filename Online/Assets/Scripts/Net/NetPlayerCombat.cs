using Unity.Netcode;
using UnityEngine;

public class NetPlayerCombat : NetworkBehaviour
{
    [Header("Combat")]
    public float fireRate = 0.2f;
    public float range = 50f;
    public int damage = 25;

    [Header("Refs")]
    public Transform shootOrigin; 

    private float nextFireTime;

    private void Update()
    {
        if (!IsOwner) return;

        var gm = FindFirstObjectByType<NetGameManager>();
        if (gm != null && !gm.MatchRunning.Value) return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            Vector3 origin = shootOrigin != null ? shootOrigin.position : transform.position + Vector3.up * 1.5f;
            Vector3 dir = shootOrigin != null ? shootOrigin.forward : transform.forward;

            FireServerRpc(origin, dir);
        }
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 origin, Vector3 direction, ServerRpcParams rpcParams = default)
    {
        direction = direction.normalized;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range))
        {
            var health = hit.collider.GetComponentInParent<NetHealth>();
            if (health != null)
            {
                ulong attacker = rpcParams.Receive.SenderClientId;
                health.DealDamageServerRpc(damage, attacker);
            }
        }
    }
}
