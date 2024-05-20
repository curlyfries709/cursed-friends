
using UnityEngine.Playables;
using UnityEngine;
using UnityEngine.Animations;


public class TimelineMarkerReceiver : MonoBehaviour, INotificationReceiver
{
    DialogueMarker currentDialogueMarker;

    bool subscribedToOnTutorialEnd = false;
    bool subscribedToOnDialogueEnd = false;

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is DialogueMarker dialogueMarker && dialogueMarker.Dialogue != null && !DialogueManager.Instance.IsDialoguePlaying())
        {
            DialogueManager.Instance.PlayDialogue(dialogueMarker.Dialogue, false);
            ListenForDialogueEnd(dialogueMarker);
        }
        else if(notification is WaitMarker waitMarker && DialogueManager.Instance.IsDialoguePlaying())
        {
            //stopCinematicOnDialogueEnd = waitMarker.EndCinematicOnDialogueEnd;
            //Debug.Log("MARKER CALLED: " + waitMarker.time);
        }
        else if (notification is AnimationOverrideMarker animationOverrideMarker)
        {
            PlayOverride(animationOverrideMarker, origin.GetGraph().GetResolver()); //The latter is the Playable Director
        }
    }

    public void PlayOverride(AnimationOverrideMarker animationOverrideMarker, IExposedPropertyTable resolver)
    {
        PropertyName referenceExposedName = animationOverrideMarker.animator.exposedName;

        Animator animator = resolver.GetReferenceValue(referenceExposedName, out bool isValid) as Animator;


        //Override Animator method. Deprecated due to causing stutter. 

        /*AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        AnimationClip defaultClip = animator.runtimeAnimatorController.animationClips[0];
        animatorOverrideController[defaultClip] = animationOverrideMarker.Animation;

        //Override Animator
        animator.runtimeAnimatorController = animatorOverrideController;*/


        PlayableDirector ghostDirector = CinematicManager.Instance.GhostLooper();
        PlayableGraph ghostGraph = ghostDirector.playableGraph;

        var playableOutput = AnimationPlayableOutput.Create(ghostGraph, animator.runtimeAnimatorController.name, animator);

        // Wrap the clip in a playable

        var clipPlayable = AnimationClipPlayable.Create(ghostGraph, animationOverrideMarker.Animation);

        // Connect the Playable to an output
        playableOutput.SetSourcePlayable(clipPlayable);
    }

    public void ListenForDialogueEnd(DialogueMarker dialogueMarker)
    {
        currentDialogueMarker = dialogueMarker;

        //Subscribe To Dialogue End Event
        if (!subscribedToOnDialogueEnd)
        {
            DialogueManager.Instance.DialogueEnded += OnDialogueEnd; //Sometimes doesn't get called when called in Wait Marker Section
            subscribedToOnDialogueEnd = true;
        }   
    }

    private void OnDialogueEnd()
    {
        //UnSubscribe
        DialogueManager.Instance.DialogueEnded -= OnDialogueEnd;
        subscribedToOnDialogueEnd = false;

        if(!CinematicManager.Instance.isCinematicPlaying || CinematicManager.Instance.isSkipping)
        {
            currentDialogueMarker = null;

            if (subscribedToOnTutorialEnd)
            {
                StoryManager.Instance.TutorialComplete -= OnDialogueEnd;
                subscribedToOnTutorialEnd = false;
            }

            return;
        }

        if (subscribedToOnTutorialEnd)
        {
            StoryManager.Instance.TutorialComplete -= OnDialogueEnd;
            subscribedToOnTutorialEnd = false;
        }

        if (currentDialogueMarker && currentDialogueMarker.PlayTutorialOnDialogueEnd)
        {
            //Play Tutorial
            StoryManager.Instance.PlayTutorial(currentDialogueMarker.TutorialIndex);

            StoryManager.Instance.TutorialComplete += OnDialogueEnd;
            currentDialogueMarker = null;

            subscribedToOnTutorialEnd = true;
        }
        else
        {
            //Continue Timeline
            CinematicManager.Instance.ResumeCinematic();
        }
    }

}
