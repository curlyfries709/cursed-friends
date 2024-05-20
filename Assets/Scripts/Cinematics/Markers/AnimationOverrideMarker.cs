using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class AnimationOverrideMarker : Marker, INotification
{
    [Header("Components")]
    public ExposedReference<Animator> animator;
    [SerializeField] AnimationClip animation;

    public AnimationClip Animation => animation;

    //Needed for INotification interface
    public PropertyName id => new PropertyName();
}
