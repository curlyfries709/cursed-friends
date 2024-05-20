using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Orb", menuName = "Item/Orb", order = 4)]
public class Orb : Item
{
    [Header("Cooldown")]
    public int cooldownAtWisLv1 = 5;
    [Header("Prefabs")]
    public GameObject orbSkillPrefab;


    public int GetMaxHealth()
    {
        return cooldownAtWisLv1 * TheCalculator.Instance.GetCooldownHealRate();
    }
}
