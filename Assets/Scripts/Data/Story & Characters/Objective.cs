using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Objective", menuName = "Story/Objective", order = 2)]
public class Objective : ScriptableObject
{
    public Quest quest;
    [Space(5)]
    [TextArea(5, 20)]
    public string description;
    [Title("Conditions")]
    public ChoiceReferences choiceReferenceToUnlock = ChoiceReferences.None;
    [Space(5)]
    [ListDrawerSettings(Expanded = true)]
    public List<Objective> nextObjectives = new List<Objective>();


    public string GetObjectiveID()
    {
        return quest.name + name;
    }
}
