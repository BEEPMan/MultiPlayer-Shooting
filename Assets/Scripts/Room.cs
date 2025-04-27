using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Room : MonoBehaviour
{
    // Room Information
    public enum RoomType { SpawnPoint, Battle, Shop, Event, Exit }
    public enum RoomState { Empty, Occupied, Cleared }
    public RoomType roomType;
    public RoomState roomState;
    public GameObject[] spawnPoints;
    private bool[] isSpawned;
    public GameObject[] enemies;
    public int[] spawnCount;
    private int currentEnemyCount;

    public List<Player> currentPlayer;

    // 입구를 막는 문 오브젝트
    public GameObject door;

    private void Awake()
    {
        roomState = RoomState.Empty;
        currentEnemyCount = 0;
        isSpawned = new bool[spawnPoints.Length];
        currentPlayer = new();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            currentPlayer.Add(player);
            if (roomType == RoomType.Battle && roomState == RoomState.Empty)
            {
                StartBattle();
            }
        }
    }

    public void StartBattle()
    {
        // 입구를 모두 막음.
        door.SetActive(true);
        roomState = RoomState.Occupied;
        GameManager.Instance.path.ChangeRoom(this);
        if (NetworkManager.Singleton.IsServer)
            SpawnEnemies();
    }

    public void Clear()
    {
        roomState = RoomState.Cleared;
        door.SetActive(false);
    }

    public void SpawnEnemies()
    {
        for (int i = 0; i < enemies.Length; i++)
        {
            for (int j = 0; j < spawnCount[i]; j++)
            {
                if(currentEnemyCount >= spawnPoints.Length)
                {
                    break;
                }
                int spawnIndex = Random.Range(0, spawnPoints.Length);
                while(isSpawned[spawnIndex])
                {
                    spawnIndex = Random.Range(0, spawnPoints.Length);
                }
                //GameObject enemy = Instantiate(enemies[i], spawnPoints[spawnIndex].transform.position, Quaternion.identity);
                NetworkObject enemy = NetworkObjectPool.Instance.GetNetworkObject(enemies[i], spawnPoints[spawnIndex].transform.position, Quaternion.identity);
                enemy.Spawn();
                //enemy.transform.SetParent(transform);
                enemy.GetComponent<Enemy>().SetCurrentRoom(transform.position);
                currentEnemyCount++;
                isSpawned[spawnIndex] = true;
            }
        }
    }

    public void OnEnemyDead(NetworkObject enemy)
    {
        StartCoroutine(DespawnEnemy(enemy));
    }

    IEnumerator DespawnEnemy(NetworkObject enemy)
    {
        yield return new WaitForSeconds(1f);
        currentEnemyCount--;
        if (NetworkManager.Singleton.IsHost)
            enemy.Despawn();
        if (currentEnemyCount <= 0)
        {
            Clear();
        }
    }
}
