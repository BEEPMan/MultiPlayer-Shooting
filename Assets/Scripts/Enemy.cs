using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.UIElements;

public class Enemy : NetworkBehaviour
{
    public enum EnemyType { Melee, Range }
    public EnemyType enemyType;

    public BoxCollider2D meleeArea;
    public GameObject projectile;

    Rigidbody2D rigid;
    SpriteRenderer spriteRender;
    Animator anim;

    List<Node> pathNodes;
    Node targetNode;

    Vector2 moveDir;
    bool isDead;

    Coroutine chaseCoroutine;
    float pathUpdateTimer = 0f;
    float pathUpdateInterval = 0.5f;

    public Room currentRoom;

    public Player target;

    public int health = 100;
    public int damage = 5;
    public float attackRange = 0.5f;
    public float attackCooldown = 1f;
    float attackTimer = 0f;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRender = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        moveDir = Vector2.zero;
        isDead = false;
    }

    public override void OnNetworkSpawn()
    {
        if (enemyType == EnemyType.Melee)
        {
            meleeArea.enabled = false;
            meleeArea.GetComponent<Attackable>().damage = damage;
            meleeArea.GetComponent<Attackable>().targetTag = "Player";
            meleeArea.GetComponent<Attackable>().owner = this;
        }
        if (IsHost)
            FindTarget();
    }

    void Update()
    {
        if (isDead) return;
        if (target == null)
        {
            if (IsHost)
                FindTarget();
            if (target == null) return;
        }
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            if (Vector3.Distance(transform.position, target.transform.position) < attackRange)
            {
                Attack();
                attackTimer = 0f;
            }
        }
        pathUpdateTimer += Time.deltaTime;
        if (pathUpdateTimer >= pathUpdateInterval)
        {
            pathUpdateTimer = 0f;
            List<Node> newPathNodes = GameManager.Instance.path.FindPath(transform.position, target.transform.position);
            if (newPathNodes != null)
            {
                pathNodes = newPathNodes;

                if (chaseCoroutine != null)
                {
                    StopCoroutine(chaseCoroutine);
                }

                chaseCoroutine = StartCoroutine(Chase());
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (target != null)
        {
            if (Vector3.Distance(transform.position, target.transform.position) < attackRange)
                return;
        }
        rigid.MovePosition(rigid.position + moveDir.normalized * 2f * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (isDead) return;
        if (target != null)
        {
            if (target.transform.position.x < transform.position.x)
            {
                spriteRender.flipX = true;
            }
            else
            {
                spriteRender.flipX = false;
            }
        }
    }

    public void SetCurrentRoom(Vector2 roomPos)
    {
        currentRoom = GameManager.Instance.mapGenerator.roomDict[roomPos];
        SetCurrentRoomClientRpc(roomPos);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetCurrentRoomClientRpc(Vector2 roomPos)
    {
        if (IsHost) return;
        currentRoom = GameManager.Instance.mapGenerator.roomDict[roomPos];
    }

    void FindTarget()
    {
        if (currentRoom == null) return;
        foreach (var player in currentRoom.currentPlayer)
        {
            if (player == null) continue;
            if (target == null)
            {
                target = player;
                continue;
            }
            if (Vector3.Distance(transform.position, player.transform.position) < Vector3.Distance(transform.position, target.transform.position))
            {
                target = player;

            }
        }
        if(target != null)
            SetTargetClientRpc(target.NetworkObjectId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetTargetClientRpc(ulong targetId)
    {
        if (IsHost) return;
        target = NetworkManager.SpawnManager.SpawnedObjects[targetId].GetComponent<Player>();
    }


    IEnumerator Chase()
    {
        int i = 0;

        targetNode = pathNodes[0];
        while(true)
        {
            if ((transform.position - targetNode.position).magnitude < 0.3f)
            {
                i++;
                if (i >= pathNodes.Count)
                {
                    yield break;
                }
                targetNode = pathNodes[i];
            }
            moveDir = targetNode.position - transform.position;
            yield return null;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        health -= amount;
        anim.SetTrigger("Hit");
        if (health <= 0)
        {
            Die();
        }
    }

    void Attack()
    {
        if (isDead) return;
        switch (enemyType)
        {
            case EnemyType.Melee:
                StartCoroutine(MeleeAttack());
                break;
            case EnemyType.Range:
                Vector3 lookDir = target.transform.position - transform.position;
                lookDir.z = 0;
                lookDir.Normalize();
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                GameObject proj = Instantiate(projectile, transform.position, Quaternion.Euler(0, 0, angle + 90f));
                proj.GetComponent<Rigidbody2D>().linearVelocity = lookDir * 30f;
                proj.GetComponent<Attackable>().damage = damage;
                proj.GetComponent<Attackable>().targetTag = "Player";
                break;
        }
        AttackClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void AttackClientRpc()
    {
        if (isDead) return;
        switch (enemyType)
        {
            case EnemyType.Melee:
                StartCoroutine(MeleeAttack());
                break;
            case EnemyType.Range:
                Vector3 lookDir = target.transform.position - transform.position;
                lookDir.z = 0;
                lookDir.Normalize();
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                GameObject proj = Instantiate(projectile, transform.position, Quaternion.Euler(0, 0, angle + 90f));
                proj.GetComponent<Rigidbody2D>().linearVelocity = lookDir * 30f;
                proj.GetComponent<Attackable>().damage = damage;
                proj.GetComponent<Attackable>().targetTag = "Player";
                break;
        }
    }

    IEnumerator MeleeAttack()
    {
        meleeArea.enabled = true;
        yield return new WaitForSeconds(0.1f);
        meleeArea.enabled = false;
    }

    void Die()
    {
        isDead = true;
        GetComponent<Collider2D>().enabled = false;
        anim.SetBool("Dead", true);
        currentRoom.OnEnemyDead(NetworkObject);
        DieClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void DieClientRpc()
    {
        if (IsHost) return;
        isDead = true;
        GetComponent<Collider2D>().enabled = false;
        anim.SetBool("Dead", true);
        currentRoom.OnEnemyDead(NetworkObject);
    }
}
