using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Playables;

public class Cutscene : MonoBehaviour
{
    [Title("Cutscene")]
    public PlayableDirector cutscene;
    [Title("Conditions")]
    [ListDrawerSettings(Expanded = true)]
    public List<EventCondition> conditionsToPlay = new List<EventCondition>();
    [Title("Next Cutscene")]
    [ListDrawerSettings(Expanded = true)]
    public List<Cutscene> nextCutscenes = new List<Cutscene>();
    [Title("Events")]
    public CinematicEvents cinematicEvents;
    [Space(5)]
    public Transform cutsceneEventsHeader;

    public TimelineMarkerReceiver markerReceiver { get; private set; }

    private void Awake()
    {
        markerReceiver = GetComponent<TimelineMarkerReceiver>();
    }

    public void OnPlay()
    {
    }

    public void OnSkip()
    {
        if (!cutsceneEventsHeader) { return; }

        CutsceneEvents[] cutsceneEvents = cutsceneEventsHeader.GetComponentsInChildren<CutsceneEvents>(true);

        foreach (CutsceneEvents cutsceneEvent in cutsceneEvents)
        {
            if (cutsceneEvent.triggerEventsIfCutsceneSkipped)
                cutsceneEvent.TriggerEvents(true);
        }
    }

    public void ResetEvents()
    {
        if (!cutsceneEventsHeader) { return; }

        CutsceneEvents[] cutsceneEvents = cutsceneEventsHeader.GetComponentsInChildren<CutsceneEvents>(true);

        foreach (CutsceneEvents cutsceneEvent in cutsceneEvents)
        {
            cutsceneEvent.ResetEvent();
        }
    }


}
