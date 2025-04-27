using UnityEngine;

public class MapObject : MonoBehaviour
{
    private void OnEnable()
    {
        transform.SetParent(GameManager.Instance.mapGenerator.transform);
    }
}
