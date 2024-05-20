using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

[CreateAssetMenu(fileName = "New Topic", menuName = "Story/Discussion", order = 3)]
public class DiscussionTopic : ScriptableObject
{
    [Title("Details")]
    public StoryCharacter discussionOwner;
    [Tooltip("Text to display on Discussion UI")]
    public string subject;
    [Title("Dialogue")]
    public Dialogue dialogue;
    [Title("Expiration")]
    public GameDate expirationDate;

}
