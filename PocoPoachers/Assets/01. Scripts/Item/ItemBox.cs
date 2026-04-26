using UnityEngine;

public class ItemBox : MonoBehaviour
{
    public int[] ItemIds { get; private set; }

    public void Initialize(int[] itemIds)
    {
        ItemIds = itemIds;
    }
}
