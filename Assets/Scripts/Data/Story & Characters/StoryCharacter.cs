using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "New Character", menuName = "Story/Character", order = 0)]
public class StoryCharacter : ScriptableObject
{
    [Title("Name & BG Potrait")]
    [PreviewField(60, Alignment = ObjectFieldAlignment.Left), HideLabel]
    public Sprite potraitBackground;
    [LabelWidth(120)]
    public string characterName;
    [Tooltip("This only applied to the Dialogue Editor")]
    public DialogueNodeColor dialogueNodeColour;
    [Space(20)]
    [TableList(AlwaysExpanded = true, DrawScrollView = false)]
    public List<CharacterMoodSprite> moodSprites;
}

public enum DialogueNodeColor
{
    Grey,
    Blue,
    Turquoise,
    Green,
    Yellow,
    Orange,
    Red


}

public enum CharacterMood
{
    Normal,
    Sad
}

[System.Serializable]
public class CharacterMoodSprite
{
    public CharacterMood mood;
    [TableColumnWidth(100, Resizable = false)]
    [PreviewField(75, Alignment = ObjectFieldAlignment.Center)]
    public Sprite sprite;
}

