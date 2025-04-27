using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    // Components
    PlayerInput input;
    SpriteRenderer spriteRender;
    Rigidbody2D rigid;
    Animator anim;

    // Movement
    Vector2 inputVec;
    bool isDashing;
    float dashEndTime;
    float dashDuration = 0.2f;
    float lastDashTime = -999f;

    // Player Stats
    public int maxHealth = 100;
    public int health;
    public float speed = 5f;
    public float dashSpeed = 20f;
    public float dashCooldown = 1f;
    public int coin;

    // Weapon
    public Transform hand;
    public Weapon[] weapons;

    private void Awake()
    {
        spriteRender = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        isDashing = false;

        

        health = maxHealth;
        coin = 0;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameManager.Instance.player = this;
            GameManager.Instance.cinemachine.Follow = transform;
        }
        foreach (var weapon in weapons)
        {
            weapon.SetOwner(this);
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rigid.linearVelocity = inputVec.normalized * dashSpeed;

            if (Time.time > dashEndTime)
            {
                isDashing = false;
                rigid.linearVelocity = Vector2.zero;
            }
        }
        else
            rigid.MovePosition(rigid.position + inputVec.normalized * speed * Time.fixedDeltaTime);
    }

    private void LateUpdate()
    {
        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0)
            spriteRender.flipX = inputVec.x < 0;
    }

    [Rpc(SendTo.Server)]
    public void WeaponRotationServerRpc(float angle, bool flipX)
    {
        weapons[0].SyncWeaponStatus(angle, flipX);
        WeaponRotationClientRpc(angle, flipX);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void WeaponRotationClientRpc(float angle, bool flipX)
    {
        if (IsOwner || NetworkManager.Singleton.IsHost) return;
        weapons[0].SyncWeaponStatus(angle, flipX);
    }

    public void OnMove(Vector2 value)
    {
        inputVec = value;
    }

    public void Attack()
    {
        weapons[0].Attack();
        AttackServerRpc();
    }

    [Rpc(SendTo.Server)]
    void AttackServerRpc()
    {
        weapons[0].Attack();
        AttackClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void AttackClientRpc()
    {
        if(IsOwner || IsHost) return;
        weapons[0].Attack();
    }

    public void Dash()
    {
        if (Time.time < lastDashTime + dashCooldown || inputVec == Vector2.zero) return;
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        lastDashTime = Time.time;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Item"))
        {
            ItemData itemData = collision.GetComponent<Item>().itemData;
            switch(itemData.itemType)
            {
                case ItemData.ItemType.Melee:
                    //weapons[0].AddItem(itemData);
                    break;
                case ItemData.ItemType.Range:
                    //weapons[1].AddItem(itemData);
                    break;
                case ItemData.ItemType.Artifact:
                    // Add artifact logic here
                    break;
                case ItemData.ItemType.Health:
                    health += itemData.value;
                    if (health > maxHealth)
                        health = maxHealth;
                    break;
                case ItemData.ItemType.Coin:
                    coin += itemData.value;
                    break;
            }
            Destroy(collision.gameObject);
        }
    }
}
