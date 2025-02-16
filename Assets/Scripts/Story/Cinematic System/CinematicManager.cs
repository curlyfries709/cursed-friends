using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AnotherRealm;
using UnityEngine.Playables;
using UnityEngine.UI;
using System;
using UnityEngine.Timeline;
using System.Linq;
using UnityEngine.InputSystem;
using DG.Tweening;

public class CinematicManager : MonoBehaviour, IControls
{
    public static CinematicManager Instance { get; private set; }

    [Header("General Timelines")]
    [SerializeField] PlayableDirector ghostLoopDirector;
    [SerializeField] Transform assetsHeader;
    [Header("UI")]
    [SerializeField] GameObject skipCanvas;
    [Header("Fader")]
    [SerializeField] float startCinematicFadeInTime = 0.35f;
    [SerializeField] FadeUI fader;
    [SerializeField] Image faderImage;

    //Variables
    float fadeInTime;
    float fadeOutTime;

    public bool isSkipping { get; private set; } = false;
    public bool isCinematicPlaying { get; private set; } = false;

    //Cache
    CanvasGroup faderCanvasGroup;
    const string myActionkey = "Cinematic";

    //Cinematic
    CinematicEvents cinematicEvents = null;
    CinematicTrigger currentCinematicTrigger = null;

    Cutscene currentCinematicCutscene = null;
    Cutscene nextCutscene = null;

    //Cutscene Cache
    DirectorWrapMode cutsceneOriginalWrapMode;
    Double cutsceneDuration;

    int currentWaitMarker = 0;
    List<AnimationOverrideMarker> animationOverrideMarkers = new List<AnimationOverrideMarker>();

    //Events
    public Action CinematicBegun;
    public Action CinematicEnded;

    private void Awake()
    {
        Instance = this;
        fadeInTime = fader.fadeInTime;
        fadeOutTime = fader.fadeOutTime;
        faderCanvasGroup = fader.GetComponent<CanvasGroup>();
        //assetsHeader.gameObject.SetActive(false);

        ControlsManager.Instance.SubscribeToPlayerInput(myActionkey, this);
    }

    private void Start()
    {
        DialogueManager.Instance.ChoiceWithReferenceSelected += UpdateNextCutscene;
    }

    public bool PlayCinematic(CinematicTrigger cinematic)
    {
        Cutscene startCutscene = cinematic.startCutscene;

        if (HandyFunctions.CanUnlockConditionalEvent(startCutscene.conditionsToPlay))
        {
            StartCoroutine(BeginCinematicRoutine(cinematic));
            AudioManager.Instance.StopMusic();
            return true;
        }

        return false;
    }

    IEnumerator BeginCinematicRoutine(CinematicTrigger cinematic)
    {
        isCinematicPlaying = true;

        if (cinematic.fadeIn)
        {
            ControlsManager.Instance.DisableControls();
            cinematic.fader.alpha = 0;
            cinematic.fader.gameObject.SetActive(true);
            cinematic.fader.DOFade(1, startCinematicFadeInTime);
            yield return new WaitForSeconds(startCinematicFadeInTime);
        }

        ControlsManager.Instance.SwitchCurrentActionMap(myActionkey);

        ShowPlayerParty(false);
        cinematic.gameObject.SetActive(true);

        nextCutscene = cinematic.startCutscene;
        currentCinematicTrigger = cinematic;

        CinematicBegun?.Invoke();

        skipCanvas.SetActive(true);

        PlayGhostLooper();

        assetsHeader.gameObject.SetActive(true);

        PlayNextCutscene();
    }

    private void PlayNextCutscene()
    {
        if (currentCinematicCutscene)
        {
            //Unsubscribe from current cutscene
            currentCinematicCutscene.cutscene.stopped -= OnCutsceneEnd;
        }
        //Play Cutscene
        currentCinematicCutscene = nextCutscene;
        currentCinematicCutscene.cutscene.Play();
        currentCinematicCutscene.OnPlay();
        //Subcribe to on end
        currentCinematicCutscene.cutscene.stopped += OnCutsceneEnd;
        //Prepare to hold at first Wait Marker
        cutsceneOriginalWrapMode = currentCinematicCutscene.cutscene.extrapolationMode;
        cutsceneDuration = currentCinematicCutscene.cutscene.duration;
        HoldCinematicAtMarker(0); //Hold At First Marker
        //Prep next cutscene
        UpdateNextCutscene(ChoiceReferences.None);
    }

    private void PlayGhostLooper()
    {
        ghostLoopDirector.Play();
    }

    private void OnCutsceneEnd(PlayableDirector director)
    {
        if (nextCutscene)
        {
            PlayNextCutscene();
        }
        else
        {
            OnCinematicEnd();
        }
    }

    private void OnCinematicEnd()
    {
        ControlsManager.Instance.DisableControls();

        //Unsubscribe from current event
        currentCinematicCutscene.cutscene.stopped -= OnCutsceneEnd;

        //Setup Events to trigger
        cinematicEvents = currentCinematicCutscene.cinematicEvents; //Last Cutscene should always have this to set Player Pos.

        //Set To null
        currentCinematicCutscene = null;
        nextCutscene = null;

        //Start Routine
        StartCoroutine(CinematicEndRoutine());
    }

    IEnumerator CinematicEndRoutine()
    {
        skipCanvas.SetActive(false);

        //Fade in
        if (cinematicEvents.cinematicFadesOut)
        {
            faderCanvasGroup.alpha = 1;
            faderImage.color = cinematicEvents.fadeColor;
            fader.Fade(true);
            yield return new WaitForSeconds(fadeInTime);
        }

        if (cinematicEvents.fader)
            cinematicEvents.fader.SetActive(false);

        //Stop Ghost Looper
        ghostLoopDirector.Stop();

        ShowPlayerParty(true);

        //Trigger During Fade Events
        cinematicEvents.duringFadeCinematicEvents?.Invoke();

        assetsHeader.gameObject.SetActive(false);

        //Warp Player, if not in combat.
        if (cinematicEvents.playerStateOnEnd != PlayerStateMachine.PlayerState.FantasyCombat)
        {
            PlayerSpawnerManager.Instance.GetPlayerStateMachine().WarpPlayer(cinematicEvents.playerPostCinematicTransform, cinematicEvents.playerStateOnEnd, true);
            //Play Roam Music
            AudioManager.Instance.PlayMusic(MusicType.Roam);
        }
            

        if (cinematicEvents.cinematicFadesOut)
        {
            fader.Fade(false);
            //Fade out
            yield return new WaitForSeconds(fadeOutTime);
        }

        isCinematicPlaying = false;
        isSkipping = false;

        //Disable Cinematic Mode.
        CinematicEnded?.Invoke();

        //Trigger Post Fade Events
        currentCinematicTrigger.gameObject.SetActive(false);
        cinematicEvents.postFadeCinematicEvents?.Invoke();

        //Enable Controls
        if (cinematicEvents.enablePlayerControls)
            ControlsManager.Instance.SwitchCurrentActionMap("Player");
    }

    private void TriggerSkippedCutsceneEvents(Cutscene skippedCutscene)
    {
        skippedCutscene.OnSkip();
    }

    private void UpdateNextCutscene(ChoiceReferences choice)
    {
        nextCutscene = GetNextCutscene(choice, currentCinematicCutscene);
    }

    private Cutscene GetNextCutscene(ChoiceReferences choice, Cutscene currentCutscene)
    {
        if (!currentCutscene || currentCutscene.nextCutscenes.Count == 0) { return null; }

        List<int> choiceReferences = new List<int>(StoryManager.Instance.StoredChoiceReferences);

        if (choice != ChoiceReferences.None && !choiceReferences.Contains((int)choice))
            choiceReferences.Add((int)choice);

        foreach (Cutscene cutscene in currentCutscene.nextCutscenes)
        {
            if (HandyFunctions.CanUnlockConditionalEvent(cutscene.conditionsToPlay))
            {
                return cutscene;
            }
        }

        //Code Shouldn't reach here.
        return null;
    }

    private void ShowPlayerParty(bool show)
    {
        PlayerSpawnerManager.Instance.ShowPlayerParty(show);
    }

    public void SkipCurrentCinematic()
    {
        if (!isCinematicPlaying || isSkipping) { return; }

        if (DialogueManager.Instance.IsDialoguePlaying()) //Check current playing dialogue first, if any
        {
            if (!DialogueManager.Instance.SkipCutsceneDialogue(DialogueManager.Instance.currentDialogue))
            {
                return;
            }
        }

        animationOverrideMarkers.Clear();

        int counter = 0;

        Cutscene cutsceneToCheck = currentCinematicCutscene;
        Cutscene finalCutscene = currentCinematicCutscene;

        while (cutsceneToCheck) //Continue Until no next cutscene.
        {
            finalCutscene = cutsceneToCheck;

            //Debug.Log("Checking Cutscene: " + cutsceneToCheck.name);

            /*
             * counter++;
             * if(counter == 150)
            {
                Debug.Log("Skip Current Cinematic Loop hit counter!");
                break;
            }*/

            Double currentTime = cutsceneToCheck.cutscene.time;

            TimelineAsset timelineAsset = cutsceneToCheck.cutscene.playableAsset as TimelineAsset;

            if (timelineAsset.markerTrack.GetMarkers() == null) //If no Markers
            {
                TriggerSkippedCutsceneEvents(cutsceneToCheck);
                cutsceneToCheck = GetNextCutscene(ChoiceReferences.None, cutsceneToCheck);
                //Debug.Log("No Markers. New Cutscene: " + cutsceneToCheck.name);
                continue;
            }

            List<IMarker> dialogueMarkers = timelineAsset.markerTrack.GetMarkers().Where((marker) => marker is DialogueMarker && marker.time >= currentTime).ToList();
            
            if (dialogueMarkers.Count == 0) //If No Dialogue markers
            {
                TriggerSkippedCutsceneEvents(cutsceneToCheck);
                cutsceneToCheck = GetNextCutscene(ChoiceReferences.None, cutsceneToCheck);
                //Debug.Log("No Dialogue Markers. New Cutscene: " + cutsceneToCheck.name);
                continue;
            }

            //Loop through each Dialogue Marker
            foreach (IMarker marker in dialogueMarkers)
            {
                DialogueMarker dialogueMarker = marker as DialogueMarker;

                if (!DialogueManager.Instance.SkipCutsceneDialogue(dialogueMarker.Dialogue)) //If Not skippable
                {
                    if (currentCinematicCutscene != cutsceneToCheck)
                    {
                        //Stop Current Cutscene & Play New One.

                        if (currentCinematicCutscene) 
                        {
                            //Unsubscribe from current cutscene
                            currentCinematicCutscene.cutscene.stopped -= OnCutsceneEnd;
                            //Stop it.
                            currentCinematicCutscene.cutscene.Stop();
                        }
                            
                        //Play Cutscene
                        currentCinematicCutscene = cutsceneToCheck;
                        currentCinematicCutscene.cutscene.Play();

                        //Subcribe to on end
                        currentCinematicCutscene.cutscene.stopped += OnCutsceneEnd;

                        //Prepare data for Wait Marker
                        cutsceneOriginalWrapMode = currentCinematicCutscene.cutscene.extrapolationMode;
                        cutsceneDuration = currentCinematicCutscene.cutscene.duration;

                        //Prep next cutscene
                        UpdateNextCutscene(ChoiceReferences.None);
                    }

                    //Tell Marker Receiver to listen to dialogue end event.
                    currentCinematicCutscene.markerReceiver.ListenForDialogueEnd(dialogueMarker);

                    //Jump to its corresponding wait Marker
                    List<IMarker> waitMarkers = timelineAsset.markerTrack.GetMarkers().Where((waitMarker) => waitMarker is WaitMarker && waitMarker.time >= dialogueMarker.time).ToList();
                    Double newTime = waitMarkers[0].time;

                    List<IMarker> allWaitMarkers = timelineAsset.markerTrack.GetMarkers().Where((waitMarker) => waitMarker is WaitMarker).ToList();

                    HoldCinematicAtMarker(allWaitMarkers.IndexOf(waitMarkers[0]));

                    currentCinematicCutscene.cutscene.time = newTime;

                    //Play Animations
                    animationOverrideMarkers = animationOverrideMarkers.Concat(timelineAsset.markerTrack.GetMarkers().Where((marker) => marker is AnimationOverrideMarker && marker.time < newTime).ToList().ConvertAll((marker) => marker as AnimationOverrideMarker)).ToList();
                    PlayAnimationOverrides();

                    return;
                } 
            }


            //If here means all dialogue markers are skippable
            //Add All Animation Override Markers to list.
            animationOverrideMarkers = animationOverrideMarkers.Concat(timelineAsset.markerTrack.GetMarkers().Where((marker) => marker is AnimationOverrideMarker).ToList().ConvertAll((marker) => marker as AnimationOverrideMarker)).ToList();

            TriggerSkippedCutsceneEvents(cutsceneToCheck);
            cutsceneToCheck = GetNextCutscene(ChoiceReferences.None, cutsceneToCheck);
        }

        isSkipping = true;

        Cutscene playingCutscene = currentCinematicCutscene;

        //If You Get Here End The Cinematic
        DialogueManager.Instance.DialogueInterrupted();

        nextCutscene = null; //Make it null so cinematic ends when OnCutsceneEnd Event called;
        currentCinematicCutscene = finalCutscene; //Update current cinematic to trigger it's Post cinematic events.

        playingCutscene.cutscene.Stop();
    }

    private void OnDisable()
    {
        //DialogueManager.Instance.ChoiceWithReferenceSelected -= UpdateNextCutscene;
    }



    public PlayableDirector GhostLooper()
    {
        return ghostLoopDirector;
    }

    private void PlayAnimationOverrides()
    {
        List<AnimationOverrideMarker> animationsToPlay = new List<AnimationOverrideMarker>();
        List<Animator> animators = new List<Animator>();
        animationOverrideMarkers.Reverse();

        //Filter List By Unique Animators
        foreach (AnimationOverrideMarker marker in animationOverrideMarkers)
        {
            Animator animator = marker.animator.Resolve(currentCinematicCutscene.cutscene.playableGraph.GetResolver());

            if (animator && !animators.Contains(animator))
            {
                animationsToPlay.Add(marker);
                animators.Add(animator);
            }
        }

        //Play Overrides
        foreach (AnimationOverrideMarker marker in animationsToPlay)
        {
            //Debug.Log("Playing Override Marker for: " + marker.animator.Resolve(currentCinematicCutscene.cutscene.playableGraph.GetResolver()).name);
            //Debug.Log("Animation: " + marker.Animation.name);
            currentCinematicCutscene.markerReceiver.PlayOverride(marker, currentCinematicCutscene.cutscene.playableGraph.GetResolver());
        }
    }

    public void HoldCinematicAtMarker(int index)
    {
        PlayableDirector playable = currentCinematicCutscene.cutscene;

        TimelineAsset timelineAsset = playable.playableAsset as TimelineAsset;

        if(!timelineAsset.markerTrack || timelineAsset.markerTrack.GetMarkers() == null) { return; }

        List<IMarker> waitMarkers = timelineAsset.markerTrack.GetMarkers().Where((marker) => marker is WaitMarker).ToList();

        if(waitMarkers.Count == 0) { return; }

        //Grab Marker.
        WaitMarker newMarkerToholdAt = waitMarkers[index] as WaitMarker;
        currentCinematicCutscene.cutscene.extrapolationMode = DirectorWrapMode.Hold;

        //Debug.Log("Holding at Marker: " + waitMarkers.IndexOf(newMarkerToholdAt) + " With Time: " + newMarkerToholdAt.time + " Timeline Time: " + playable.time);

        playable.playableGraph.GetRootPlayable(0).SetDuration(newMarkerToholdAt.time);

        currentWaitMarker = index;
    }

    public void ResumeCinematic()
    {
        if (!isCinematicPlaying) { return; }

        PlayableDirector playable = currentCinematicCutscene.cutscene;

        TimelineAsset timelineAsset = playable.playableAsset as TimelineAsset;
        List<IMarker> waitMarkers = timelineAsset.markerTrack.GetMarkers().Where((marker) => marker is WaitMarker).ToList();

        WaitMarker newMarkerToholdAt;

        currentWaitMarker++;

        if (currentWaitMarker >= waitMarkers.Count)
        {
            WaitMarker lastWaitMarker = waitMarkers[waitMarkers.Count - 1] as WaitMarker;
            //Means this is the last wait Marker. //Revert Extrapolation mode. 
            if (!lastWaitMarker.EndCinematicOnDialogueEnd)
                playable.playableGraph.GetRootPlayable(0).SetDuration(cutsceneDuration);

            currentCinematicCutscene.cutscene.extrapolationMode = cutsceneOriginalWrapMode;

            if (lastWaitMarker.EndCinematicOnDialogueEnd)
                playable.Stop();
            return;
        }

        newMarkerToholdAt = waitMarkers[currentWaitMarker] as WaitMarker;

        //Debug.Log("Holding at Marker: " + waitMarkers.IndexOf(newMarkerToholdAt) + " With Time: " + newMarkerToholdAt.time + " Timeline Time: " + playable.time);

        playable.playableGraph.GetRootPlayable(0).SetDuration(newMarkerToholdAt.time);
        
    }

    //Input
    private void OnSkip(InputAction.CallbackContext context)
    {
        if (context.action.name != "Skip") { return; }

        if (context.performed && isCinematicPlaying)
        {
            SkipCurrentCinematic();
        }
    }



    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSkip;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSkip;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

}
