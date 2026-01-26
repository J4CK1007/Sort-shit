using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Sorting Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public ItemData[] allItems;
}
