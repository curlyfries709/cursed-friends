using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class DialogueMarker : Marker, INotification
{
    [SerializeField] Dialogue dialogueToPlay;
    [Header("Tutorial")]
    [SerializeField] bool playTutorialOnDialogueEnd = false;
    [SerializeField] int tutorialIndexToPlay = 0;
    public Dialogue Dialogue => dialogueToPlay;
    public bool PlayTutorialOnDialogueEnd => playTutorialOnDialogueEnd;
    public int TutorialIndex => tutorialIndexToPlay;

    //Needed for INotification interface
    public PropertyName id => new PropertyName();
}
