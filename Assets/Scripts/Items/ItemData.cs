using UnityEngine;

public enum ItemType { Medikit, Magazine }

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/Item")]
public class ItemData : ScriptableObject
{
    public ItemType type;

    [Header("Valores")]
    public int healAmount = 30;      // si es Medikit
    public int magsToAdd = 1;        // si es Magazine
}
