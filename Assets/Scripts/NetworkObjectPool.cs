using UnityEngine;
using Unity.Netcode;
using UnityEngine.Pool;
using System.Collections.Generic;
using NUnit.Framework;
using System;

public class NetworkObjectPool : NetworkBehaviour
{
    public static NetworkObjectPool Instance { get; private set; }

    [SerializeField]
    List<PoolConfigObject> PooledPrefabsList;

    HashSet<GameObject> m_Prefabs = new();

    Dictionary<GameObject, ObjectPool<NetworkObject>> m_PooledObjects = new();

    Dictionary<string, GameObject> m_NameToPrefabs = new();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        foreach (var configObject in PooledPrefabsList)
        {
            RegisterPrefabInternal(configObject.Prefab, configObject.PrewarmCount);
            m_NameToPrefabs.Add(configObject.Prefab.name, configObject.Prefab);
        }
    }

    public override void OnNetworkDespawn()
    {
        foreach (var prefab in m_Prefabs)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
            m_PooledObjects[prefab].Clear();
        }
        m_PooledObjects.Clear();
        m_Prefabs.Clear();
    }

    public void OnValidate()
    {
        for (int i = 0; i < PooledPrefabsList.Count; i++)
        {
            var prefab = PooledPrefabsList[i].Prefab;
            if (prefab != null)
            {
                Assert.IsNotNull(prefab.GetComponent<NetworkObject>(), $"{nameof(NetworkObjectPool)}: Pooled prefab \"{prefab.name}\" at index {i.ToString()} has no {nameof(NetworkObject)} component.");
            }
        }
    }

    public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var networkObject = m_PooledObjects[prefab].Get();

        var noTransform = networkObject.transform;
        noTransform.position = position;
        noTransform.rotation = rotation;

        return networkObject;
    }

    public NetworkObject GetNetworkObject(string name, Vector3 position, Quaternion rotation)
    {
        var networkObject = m_PooledObjects[m_NameToPrefabs[name]].Get();

        var noTransform = networkObject.transform;
        noTransform.position = position;
        noTransform.rotation = rotation;

        return networkObject;
    }

    public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
    {
        m_PooledObjects[prefab].Release(networkObject);
    }

    public void ReturnNetworkObject(NetworkObject networkObject, string name)
    {
        m_PooledObjects[m_NameToPrefabs[name]].Release(networkObject);
    }

    void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
    {
        NetworkObject CreateFunc()
        {
            return Instantiate(prefab).GetComponent<NetworkObject>();
        }

        void ActionOnGet(NetworkObject networkObject)
        {
            networkObject.gameObject.SetActive(true);
        }

        void ActionOnRelease(NetworkObject networkObject)
        {
            networkObject.gameObject.SetActive(false);
        }

        void ActionOnDestroy(NetworkObject networkObject)
        {
            Destroy(networkObject.gameObject);
        }

        m_Prefabs.Add(prefab);

        m_PooledObjects[prefab] = new(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, defaultCapacity: prewarmCount);

        // Pool에 오브젝트 미리 생성
        var prewarmNetworkObjects = new List<NetworkObject>();
        for (int i = 0; i < prewarmCount; i++)
        {
            prewarmNetworkObjects.Add(m_PooledObjects[prefab].Get());
        }
        foreach (var networkObject in prewarmNetworkObjects)
        {
            m_PooledObjects[prefab].Release(networkObject);
        }

        NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
    }

    [Serializable]
    struct PoolConfigObject
    {
        public GameObject Prefab;
        public int PrewarmCount;
    }

    class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
    {
        GameObject m_Prefab;
        NetworkObjectPool m_Pool;

        public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
        {
            m_Prefab = prefab;
            m_Pool = pool;
        }

        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            return m_Pool.GetNetworkObject(m_Prefab, position, rotation);
        }

        public void Destroy(NetworkObject networkObject)
        {
            m_Pool.ReturnNetworkObject(networkObject, m_Prefab);
        }
    }
}
