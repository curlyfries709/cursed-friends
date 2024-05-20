using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Story/Quest", order = 1)]
public class Quest : ScriptableObject
{
    public string title;
    public QuestType type;
    public bool isFantasyQuest;
    [Space(5)]
    public List<Objective> startObjectives;

    public enum QuestType
    {
        Main,
        Side
    }

}


