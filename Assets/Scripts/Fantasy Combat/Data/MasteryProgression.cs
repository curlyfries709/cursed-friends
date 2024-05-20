using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(fileName = "New Mastery Progression", menuName = "Configs/Mastery/Progression", order = 1)]
public class MasteryProgression : ScriptableObject
{
    [Title("Details")]
    public string progressionName;
    public ProgressionType progressionType;
    [Space(5)]
    [TextArea(2,5)]
    public string description;
    [Title("Attribute")]
    public Attribute rewardAttribute;
    [Range(1, 50)]
    public int rewardPoints = 1;
    [Title("Completion")]
    public int requiredCountToComplete;
    [Title("Mastery UI")]
    public Sprite progressionIcon;
    public Color iconColor = Color.red;

    public enum ProgressionType
    {
        UsePhys,
        PhysKo,
        Hit,
        Evade,
        Counter,
        Survive,
        UsePotion,
        Fired,
        Move,
        UseChain,
        GoAgain,
        UseMag,
        UseOrb,
        MagKo,
        UseSupport,
        UseBlessing,
        ChargeItems,
        Crit,
        InflictSE,
        Wound
    }
}
