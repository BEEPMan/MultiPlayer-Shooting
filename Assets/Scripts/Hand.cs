using UnityEngine;

public class Hand : MonoBehaviour
{
    SpriteRenderer playerSprite;

    Player owner;

    Vector3 handPos = new Vector3(0.35f, -0.15f, 0);
    Vector3 handPosFlip = new Vector3(-0.15f, -0.15f, 0);

    private void Awake()
    {
        playerSprite = owner.GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        bool isFlipped = playerSprite.flipX;

        transform.localPosition = isFlipped ? handPosFlip : handPos;
    }
}
