using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Object/ItemData", order = 1)]
public class ItemData : ScriptableObject
{
    public enum ItemType { Melee, Range, Artifact, Health, Coin }

    [Header("Main Info")]
    public ItemType itemType;
    public int itemId;
    public string itemName;
    [TextArea]
    public string itemDesc;
    public Sprite itemIcon;
    public int value;
}
