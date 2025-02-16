using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentTransforms : MonoBehaviour
{
    [Header("Transforms")]
    public Transform enchantmentHeader;
    [Space(10)]
    public Transform weaponHeaderTransform;
    [Tooltip("Things Such as Quivers, Dual wielding weapons, etc")]
    public List<Transform> otherEquipmentHeaders;
}
