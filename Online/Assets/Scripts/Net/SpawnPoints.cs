using UnityEngine;
public class SpawnPoints : MonoBehaviour
{
    [Header("Spawn points (задай в инспекторе)")]
    public Transform[] points;

    [Header("Ground snap (чтобы спавнило ровно на полу)")]
    public bool snapToGround = true;
    public float rayStartUp = 5f;          
    public float rayDistance = 50f;        
    public float groundOffset = 0.1f;      
    public LayerMask groundMask = ~0;      

    public Vector3 GetRandomSpawn()
    {
        if (points == null || points.Length == 0)
            return Vector3.zero;

        int i = Random.Range(0, points.Length);
        return Prepare(points[i].position);
    }

    public Vector3 GetSpawnByIndex(int index)
    {
        if (points == null || points.Length == 0)
            return Vector3.zero;

        index = Mathf.Clamp(index, 0, points.Length - 1);
        return Prepare(points[index].position);
    }
    public Vector3 GetSpawnForClient(ulong clientId)
    {
        if (points == null || points.Length == 0)
            return Vector3.zero;

        int i = (int)(clientId % (ulong)points.Length);
        return Prepare(points[i].position);
    }

    private Vector3 Prepare(Vector3 p)
    {
        if (!snapToGround) return p;

        Vector3 start = p + Vector3.up * rayStartUp;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * groundOffset;
        }

        return p;
    }

#if UNITY_EDITOR
    [Header("Gizmos (для удобства)")]
    public bool drawGizmos = true;
    public float gizmoSize = 0.35f;

    private void OnDrawGizmos()
    {
        if (!drawGizmos || points == null) return;

        Gizmos.matrix = Matrix4x4.identity;
        for (int i = 0; i < points.Length; i++)
        {
            var t = points[i];
            if (t == null) continue;

            Gizmos.DrawWireSphere(t.position, gizmoSize);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(t.position + Vector3.up * 0.4f, $"SP{i}");
#endif
        }
    }
#endif
}
