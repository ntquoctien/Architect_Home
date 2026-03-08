using UnityEngine;

public enum FurniturePlacementType
{
    Floor,
    Wall,
    Decoration
}

public enum FurnitureCategory
{
    Sofa,
    Table,
    Chair,
    Bed,
    Light,
    Decor
}

[CreateAssetMenu(fileName = "NewFurniture", menuName = "Decor/Furniture")]
public class FurnitureData : ScriptableObject
{
    [Header("Identity")]
    public string id;                  // item id (unique)
    public string displayName;         // display name

    [Header("Assets")]
    public GameObject prefab;          // Prefab 3D
    public Sprite icon;                // Icon UI

    [Header("Classification")]
    public FurniturePlacementType placementType = FurniturePlacementType.Floor;
    public FurnitureCategory category = FurnitureCategory.Decor;

    [Header("Economy (optional)")]
    public int price;                  // price (optional)
}
