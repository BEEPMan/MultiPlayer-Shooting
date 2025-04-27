using Unity.Netcode;
using UnityEngine;

public class NetworkSpawner : NetworkBehaviour
{
    [Rpc(SendTo.Server)]
    public void SpawnObjectServerRpc(string prefabName, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = ObjectPool.Instance.Pop(prefabName, position, rotation);
        if(prefab!=null)
            SpawnObjectClientRpc(prefabName, position, rotation);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SpawnObjectClientRpc(string prefabName, Vector3 position, Quaternion rotation)
    {
        if (IsOwner || IsHost) return;
        GameObject prefab = ObjectPool.Instance.Pop(prefabName, position, rotation);
    }
}
