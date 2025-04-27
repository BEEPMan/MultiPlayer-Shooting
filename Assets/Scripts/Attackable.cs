using Unity.Netcode;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class Attackable : MonoBehaviour
{
    public enum AttackType { Melee, Projectile }
    public AttackType attackType;
    public string targetTag;
    public int damage;

    // For melee enemy
    [HideInInspector]
    public Enemy owner;

    private void LateUpdate()
    {
        if (attackType == AttackType.Melee)
        {
            if (targetTag == "Player")
            {
                Vector3 lookDir = owner.target.transform.position - transform.parent.position;
                lookDir.z = 0;
                lookDir.Normalize();

                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(targetTag))
        {
            if (targetTag == "Enemy")
            {
                Enemy enemy = collision.GetComponent<Enemy>();
                if (enemy != null)
                {
                    if (NetworkManager.Singleton.IsHost)
                        enemy.TakeDamage(damage);
                }
            }
            else if (targetTag == "Player")
            {
                Player player = collision.GetComponent<Player>();
                if (player != null)
                {
                    if (NetworkManager.Singleton.IsHost)
                        player.TakeDamage(damage);
                }
            }
            if (attackType == AttackType.Projectile)
                Destroy(gameObject);
        }
        else if (collision.CompareTag("Wall") || collision.CompareTag("Obstacle"))
        {
            if (attackType == AttackType.Projectile)
                Destroy(gameObject);
        }
    }
}
