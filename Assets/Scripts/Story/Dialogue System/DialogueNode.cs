using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

public class DialogueNode : ScriptableObject
{
    [HideIf("hasMultipleSpeakers")]
    [Title("Speaker Data")]
    public StoryCharacter speaker;
    [ShowIf("hasMultipleSpeakers")]
    [Title("Speaker Data")]
    public List<StoryCharacter> speakers;
    [HideIf("isDialogueChoice")]
    public CharacterMood mood;
    [HideIf("isDialogueChoice")]
    public bool shakeCam = false;
    [Space(10)]
    [Title("Dialogue")]
    [HideIf("isDialogueChoice")]
    public AudioClip soundOnPlay;
    [Space(5)]
    [HideLabel]
    [TextArea(5, 100)] public string text = "Dialogue Text";
    [Title("Dialogue Type")]
    public bool isDialogueChoice;
    [HideIf("isDialogueChoice")]
    public bool isThinkBubble;
    [ShowIf("isDialogueChoice")]
    public bool isTalentChoice;
    [ShowIf("isDialogueChoice")]
    [Tooltip("This dialogue choice doesn't advance the main dialogue. Therefore, on end it loops back to original choice.")]
    public bool isBonusDialogueChoice;
    [HideIf("isDialogueChoice")]
    public bool hasMultipleSpeakers;

    [Title("Talent Data")]
    [ShowIf("ShowTalentModifiers")]
    public Talent talent;
    [Range(5, 100)]
    [ShowIf("ShowTalentModifiers")]
    public int lv1TalentSuccessChance = 5;
    [Space(10)]
    [ShowIf("ShowTalentModifiers")]
    public List<TalentCheckModifier> talentCheckModifiers;


    [Title("Events")]
    public Objective completeObjective;
    [ShowIf("isDialogueChoice")]
    public ChoiceReferences choiceReference = ChoiceReferences.None;

    [Title("Conditions")]
    [Tooltip("Dialogue is checked in order of highest priority. Helpful when you want conditional dialogues to be checked first")]
    [ShowIf("HasConditions")]
    public int nodePriorityNum = 0;
    [ShowIf("isDialogueChoice")]
    [Tooltip("Should this be shown as a unselectable Choice in the UI?")]
    public bool showChoiceEvenIfIneligible = false;
    [Space(10)]
    public List<EventCondition> conditionsToUnlockNode = new List<EventCondition>();

    [Title("Node Data")]
    [ReadOnly]
    public Rect rect = new Rect(10, 20, 300, 200);
    [ReadOnly]
    public List<DialogueNode> childNodes = new List<DialogueNode>();

#if UNITY_EDITOR
    //Add & Remove Children
    public void AddChild(DialogueNode childNode)
    {

        Undo.RecordObject(this, "Add Dialogue Link");
        childNodes.Add(childNode);
        EditorUtility.SetDirty(this);
    }

    public void RemoveChild(DialogueNode childNode)
    {
        Undo.RecordObject(this, "Remove Dialogue Link");
        childNodes.Remove(childNode);
        EditorUtility.SetDirty(this);
    }

    public void SetIsDialogueChoice(bool isChoice)
    {
        if (isDialogueChoice != isChoice)
        {
            isDialogueChoice = isChoice;
            EditorUtility.SetDirty(this);
        }
    }

    //Node Set Position & Size
    public void SetPosition(Vector2 newPosition)
    {
        Undo.RecordObject(this, "Move Dialogue Node");
        rect.position = newPosition;
        EditorUtility.SetDirty(this);
    }

    public void SetSize(Vector2 newSize)
    {
        rect.size = newSize;
        EditorUtility.SetDirty(this);
    }

#endif

    //Getters

    public StoryCharacter GetSpeaker()
    {
        return speaker;
    }

    private bool ShowTalentModifiers()
    {
        return isTalentChoice && isDialogueChoice;
    }

    public bool HasConditions()
    {
        return conditionsToUnlockNode.Count > 0;
    }
}
