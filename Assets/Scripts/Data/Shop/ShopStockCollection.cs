using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Stock", menuName = "Configs/Shop Stock", order = 9)]
public class ShopStockCollection : ScriptableObject
{
    [Range(1, 99)]
    [Tooltip("At what level must Keenan be for this stock to be added to shop")]
    public int levelToAddStockToShop = 1;
    [Space(5)]
    [Range(1, 100)]
    [Tooltip("To Avoid the shop list being oversaturated, there's an expiration for old items. Set this to 100, for the items to be shown infinitely")]
    public int levelToStopShowingStock = 9;
    [Space(10)]
    [TableList(AlwaysExpanded = true)]
    public List<ShopItem> stockCollection;
}

[System.Serializable]
public class ShopItem
{
    public Item item;
    public int dailyStockCount = 1;
}
