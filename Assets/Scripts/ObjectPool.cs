using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    class Pool
    {
        public GameObject Original { get; private set; }
        private Transform root;
        Queue<GameObject> poolQueue = new Queue<GameObject>();

        public void Init(GameObject original, int count = 20)
        {
            root = new GameObject { name = $"{original.name}_Pool" }.transform;
            DontDestroyOnLoad(root);
            Original = original;
            for (int i = 0; i < count; i++) Push(Create());
        }

        GameObject Create()
        {
            GameObject go = Instantiate(Original);
            go.transform.SetParent(root);
            go.name = Original.name;
            return go;
        }

        public void Push(GameObject go)
        {
            go.transform.SetParent(root);
            go.SetActive(false);
            poolQueue.Enqueue(go);
        }

        public GameObject Pop(Vector3 position, Quaternion rotation, Transform parent = null, int increaseCount = 1)
        {
            GameObject go;
            if (poolQueue.Count > 0) go = poolQueue.Dequeue();
            else if (increaseCount > 0)
            {
                for (int i = 0; i < increaseCount; i++) Push(Create());
                go = poolQueue.Dequeue();
            }
            else return null;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.SetActive(true);
            return go;
        }
    }

    private Dictionary<string, Pool> objectPool = new Dictionary<string, Pool>();

    public GameObject[] PoolLists;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < PoolLists.Length; i++)
        {
            CreatePool(PoolLists[i]);
        }
    }

    void Update()
    {

    }

    public void CreatePool(GameObject original)
    {
        if (objectPool.TryGetValue(original.name, out _)) return;
        Pool pool = new Pool();
        pool.Init(original);
        objectPool.Add(original.name, pool);
    }

    public void Push(GameObject go)
    {
        Pool pool;
        if (!objectPool.TryGetValue(go.name, out pool))
        {
            Destroy(go);
            return;
        }
        pool.Push(go);
    }

    public GameObject Pop(string name, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        Pool pool;
        if (!objectPool.TryGetValue(name, out pool))
        {
            Debug.LogError($"ObjectPool: {name} is not exist.");
            return null;
        }
        return pool.Pop(position, rotation, parent);
    }
}
