using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine;

public class WaitMarker : Marker, INotification
{
    [SerializeField] bool endCinematicOnDialogueEnd = false;
    //Needed for INotification interface
    public bool EndCinematicOnDialogueEnd => endCinematicOnDialogueEnd;
    public PropertyName id => new PropertyName();
}
