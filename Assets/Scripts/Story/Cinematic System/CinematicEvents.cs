using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class CinematicEvents : MonoBehaviour
{
    [Title("Player State")]
    public Transform playerPostCinematicTransform;
    public PlayerStateMachine.PlayerState playerStateOnEnd;
    [Space(10)]
    public bool enablePlayerControls = true;
    [Title("Fader")]
    public bool cinematicFadesOut = true;
    public GameObject fader;
    public Color fadeColor = Color.black;
    [Title("Events")]
    public UnityEvent duringFadeCinematicEvents;
    [Space(5)]
    public UnityEvent postFadeCinematicEvents;
}
