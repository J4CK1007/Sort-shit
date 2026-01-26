using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Sorting Game/Item")]
public class ItemData : ScriptableObject
{
    public string description;
    public Sprite icon;
    public string[] tags;

    public string DisplayName => name;
}
