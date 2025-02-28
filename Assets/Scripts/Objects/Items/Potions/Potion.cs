using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Potion", menuName = "Item/Potion", order = 3)]
public class Potion : Item
{
    [Header("Potion Data")]
    [Tooltip("A Special potion is one with a unique effect other than healing, curing and buffing Stats")]
    public bool useableOutsideCombat = false;
    public PotionIcon potionIcon;
    [Space(10)]
    public int hpGain;
    public int spGain;
    public int fpGain;
    [Space(10)]
    public bool canRevive;
    [Header("Status Effects")]
    public List<StatusEffectData> statusEffectsToCure;
    [Space(10)]
    public List<ChanceOfInflictingStatusEffect> inflictedStatusEffects = new List<ChanceOfInflictingStatusEffect>();
    //List Of StatusToBuff & BuffChange
    //Positive Status Effect To Inflict
    [Header("Special Potion Only")]
    public bool chargeAllItems = false;
    [Space(10)]
    public bool isSpecialPotion = false;
    public GameObject specialPotionPrefab;


    public enum PotionIcon
    {
        None,
        HP,
        SP,
        Buff,
        FP
    }

    public void UseOutsideCombat(PlayerGridUnit user)
    {
        user.CharacterHealth().OuterCombatRestore(hpGain, spGain, fpGain);

        foreach(StatusEffectData effect in statusEffectsToCure)
        {
            StatusEffectManager.Instance.CureStatusEffect(user, effect);
        }
    }
}
