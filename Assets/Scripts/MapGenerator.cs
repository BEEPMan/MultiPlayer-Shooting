using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MapGenerator : NetworkBehaviour
{
    public GameObject[] rooms;
    public GameObject[] corridors;
    public GameObject[] walls;

    HashSet<Vector2> roomPositions = new();
    Vector2[] directions = new Vector2[]
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    public Dictionary<Vector2, Room> roomDict = new();

    // Host only
    public void GenerateMap(int count)
    {
        List<Vector2> spawnableRooms = new();
        Vector2 start = Vector2.zero;

        // Generate opened rooms
        roomPositions.Add(start);
        spawnableRooms.Add(start);
        GameObject room = ObjectPool.Instance.Pop(rooms[1].name, Vector2.zero, Quaternion.identity, transform);
        roomDict.Add(Vector2.zero, room.GetComponent<Room>());
        GenerateMapClientRpc(rooms[1].name, Vector2.zero, true);

        while (roomPositions.Count < count)
        {
            Vector2 baseRoom = spawnableRooms[Random.Range(0, spawnableRooms.Count)];

            int idx = Random.Range(0, directions.Length);
            Vector2 newRoom = baseRoom + directions[idx] * 22f;

            if (!roomPositions.Contains(newRoom))
            {
                roomPositions.Add(newRoom);
                spawnableRooms.Add(newRoom);
                room = ObjectPool.Instance.Pop(rooms[1].name, newRoom, Quaternion.identity, transform);
                roomDict.Add(newRoom, room.GetComponent<Room>());
                GenerateMapClientRpc(rooms[1].name, newRoom, true);
            }
        }

        // Generate outside walls and edge lists between rooms
        List<(Vector2, Vector2)> edges = new();
        foreach (var roomPos in roomPositions)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2 newRoom = roomPos + directions[i] * 22f;
                if (!roomPositions.Contains(newRoom))
                {
                    GameObject wallPrefab = walls[i];
                    ObjectPool.Instance.Pop(wallPrefab.name, roomPos, Quaternion.identity, transform);
                    GenerateMapClientRpc(wallPrefab.name, roomPos);
                }
                else
                {
                    if (roomPos.x < newRoom.x || roomPos.y < newRoom.y)
                    {
                        edges.Add((roomPos, newRoom));
                    }
                }
            }
        }

        // Generate corridors
        System.Random rand = new();
        edges = edges.OrderBy(x => rand.Next()).ToList();

        Dictionary<Vector2, Vector2> parent = new();

        foreach (var roomPos in roomPositions)
            parent[roomPos] = roomPos;

        Vector2 Find(Vector2 x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(Vector2 x, Vector2 y)
        {
            Vector2 rootX = Find(x);
            Vector2 rootY = Find(y);
            if (rootX != rootY)
                parent[rootX] = rootY;
        }

        foreach (var edge in edges)
        {
            Vector2 roomA = edge.Item1;
            Vector2 roomB = edge.Item2;
            if (Find(roomA) != Find(roomB))
            {
                Union(roomA, roomB);
                GameObject corridorPrefab;
                if (roomA.x == roomB.x)
                {
                    corridorPrefab = corridors[0];
                }
                else
                {
                    corridorPrefab = corridors[1];

                }
                ObjectPool.Instance.Pop(corridorPrefab.name, roomA, Quaternion.identity, transform);
                GenerateMapClientRpc(corridorPrefab.name, roomA);
            }
            else
            {
                if (Random.Range(0, 100) % 2 == 0)
                {
                    GameObject corridorPrefab;
                    if (roomA.x == roomB.x)
                    {
                        corridorPrefab = corridors[0];

                    }
                    else
                    {
                        corridorPrefab = corridors[1];
                    }
                    ObjectPool.Instance.Pop(corridorPrefab.name, roomA, Quaternion.identity, transform);
                    GenerateMapClientRpc(corridorPrefab.name, roomA);
                }
                else
                {
                    GameObject wallPrefab;
                    if (roomA.x == roomB.x)
                    {
                        wallPrefab = walls[4];
                    }
                    else
                    {
                        wallPrefab = walls[5];
                    }
                    ObjectPool.Instance.Pop(wallPrefab.name, roomA, Quaternion.identity, transform);
                    GenerateMapClientRpc(wallPrefab.name, roomA);
                }
            }
        }
    }

    public void GenerateLobby()
    {
        Vector2 start = Vector2.zero;
        roomPositions.Add(start);
        GameObject room = ObjectPool.Instance.Pop(rooms[0].name, start, Quaternion.identity,transform);
        roomDict.Add(start, room.GetComponent<Room>());
        GenerateMapClientRpc(rooms[0].name, start, true);
        for (int i = 0; i < 4; i++)
        {
            GameObject wallPrefab = walls[i];
            ObjectPool.Instance.Pop(wallPrefab.name, start, Quaternion.identity, transform);
            GenerateMapClientRpc(wallPrefab.name, start);
        }
    }

    public void ClearRoom()
    {
        while (transform.childCount > 0)
        {
            ObjectPool.Instance.Push(transform.GetChild(0).gameObject);
        }
        roomPositions.Clear();
        roomDict.Clear();
        ClearRoomClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ClearRoomClientRpc()
    {
        if (IsHost) return;

        while (transform.childCount > 0)
        {
            ObjectPool.Instance.Push(transform.GetChild(0).gameObject);
        }
        roomPositions.Clear();
        roomDict.Clear();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void GenerateMapClientRpc(string name, Vector2 position, bool isRoom = false)
    {
        if (IsHost)
            return;
        GameObject room = ObjectPool.Instance.Pop(name, position, Quaternion.identity, transform);
        if (isRoom)
            roomDict.Add(position, room.GetComponent<Room>());
    }
}
