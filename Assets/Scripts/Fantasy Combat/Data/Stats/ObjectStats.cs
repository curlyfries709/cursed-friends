using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectStats : UnitStats
{
    [Header("Object SPECIFIC DATA")]
    [SerializeField] int armour;
    [Space(10)]
    [Tooltip("If this object is immediately destroyed when hit by this affinity, mark the affinity as weak.")]
    [SerializeField] List<ElementAffinity> elementAffinities = new List<ElementAffinity>();

    public override int GetArmour()
    {
        return armour;
    }

    protected override List<ElementAffinity> GetDefaultElementAffinities()
    {
        return elementAffinities;
    }
}
