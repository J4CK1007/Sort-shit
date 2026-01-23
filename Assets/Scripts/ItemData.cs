using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    public string displayName;

    [TextArea] public string description;

    // Optional: tags for scoring / grouping later
    public string[] tags;

    // Optional: show an icon in UI
    public Sprite icon;
}
