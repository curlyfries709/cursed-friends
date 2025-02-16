using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Item/Tool", order = 10)]
public class Tool : Item
{
    [Header("Tool Data")]
    public bool isPassive = false;
    [Tooltip("Can one intance of this tool be used infinite times?")]
    public bool hasInfiniteUses = false;
    [Header("Tool Object")]
    public GameObject toolFunctionalityPrefab;
}
