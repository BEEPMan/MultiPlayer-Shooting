using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    // Components
    SpriteRenderer spriteRender;

    // Weapon data
    public ItemData.ItemType weaponType;
    public int damage;
    public float rate;

    // For melee weapon
    public BoxCollider2D meleeArea;
    public Vector2 areaOffset;
    public Vector2 areaSize;
    // For ranged weapon
    public GameObject projectile;

    // Weapon Rotation
    SpriteRenderer playerSprite;
    Vector3 lookDir;
    float angle;
    static Vector3 rightPos = new Vector3(0.35f, -0.15f, 0);
    static Vector3 rightPosReverse = new Vector3(-0.15f, -0.15f, 0);
    static Vector3 leftPos = new Vector3(-0.17f, -0.38f, 0);
    static Vector3 leftPosReverse = new Vector3(0.17f, -0.38f, 0);

    Player owner;

    private void Awake()
    {
        spriteRender = GetComponent<SpriteRenderer>();
        if (weaponType == ItemData.ItemType.Melee)
        {
            meleeArea.offset = areaOffset;
            meleeArea.size = areaSize;
        }
    }

    void Start()
    {
        if (weaponType == ItemData.ItemType.Melee)
        {
            meleeArea.enabled = false;
            meleeArea.GetComponent<Attackable>().damage = damage;
            meleeArea.GetComponent<Attackable>().targetTag = "Player";
        }
    }

    private void LateUpdate()
    {
        if (GameManager.Instance.player == null || owner == null) return;
        if (!owner.IsOwner) return;

        bool isReverse = playerSprite.flipX;
        if (weaponType== ItemData.ItemType.Melee)
        {
            transform.localPosition = isReverse ? leftPosReverse : leftPos;
            spriteRender.flipX = isReverse;
        }
        else
        {
            transform.localPosition = isReverse ? rightPosReverse : rightPos;
            spriteRender.flipX = isReverse;
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lookDir = mousePos - owner.transform.position;
        lookDir.z = 0;
        lookDir.Normalize();

        angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        if (mousePos.x < transform.position.x)
        {
            spriteRender.flipX = true;
            angle += 180;
        }
        else
            spriteRender.flipX = false;

        transform.rotation = Quaternion.Euler(0, 0, angle);
        owner.WeaponRotationServerRpc(angle, spriteRender.flipX);
    }

    public void SyncWeaponStatus(float angle, bool flipX)
    {
        transform.rotation = Quaternion.Euler(0, 0, angle);
        if(weaponType == ItemData.ItemType.Melee)
        {
            meleeArea.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        spriteRender.flipX = flipX;
    }

    public void SetOwner(Player player)
    {
        owner = player;
        playerSprite = player.GetComponent<SpriteRenderer>();
    }

    public void Attack()
    {
        if (weaponType == ItemData.ItemType.Melee)
        {
            StartCoroutine(MeleeAttack());
        }
        else if (weaponType == ItemData.ItemType.Range)
        {
            GameObject proj = Instantiate(projectile, owner.transform.position, Quaternion.Euler(0, 0, spriteRender.flipX ? angle + 90f : angle - 90f));
            proj.GetComponent<Rigidbody2D>().linearVelocity = (spriteRender.flipX ? -transform.right : transform.right) * 30f;
            proj.GetComponent<Attackable>().damage = damage;
            proj.GetComponent<Attackable>().targetTag = "Enemy";
        }
    }

    IEnumerator MeleeAttack()
    {
        meleeArea.enabled = true;
        yield return new WaitForSeconds(0.1f);
        meleeArea.enabled = false;
    }
}
