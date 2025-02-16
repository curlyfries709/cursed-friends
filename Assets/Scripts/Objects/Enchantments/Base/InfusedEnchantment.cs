using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Infused Enchantment", menuName = "Item/Enchantment/Infused", order = 2)]
public class InfusedEnchantment : Enchantment
{
    /* 
     ONLY INFUSED ENCHANTMENTS CAN ALTER AFFINITIES!

    This is to prevent players constantly slotting an enchantment that protects their weakness into every armour. 
    ALSO, to avoid conflictions between enchantment where one reflects fire damage and the other resists fire damage.

    For Data display reasons, they also can only alter subattributes & the main Equipment alters Main Attributes.
 */
    [Header("INFUSED ENCHANTMENT DATA")]
    [Tooltip("ONLY INFUSED ENCHANTMENTS CAN ALTER SUBATTRIBUTES for Displaying Data reasons.")]
    public List<SubStatBonus> bonusSubAttributes;
    [Space(5)]
    [Tooltip("ONLY INFUSED ENCHANTMENTS CAN ALTER AFFINITIES for balance reasons")]
    public List<ElementAffinity> elementAlteration;



}
