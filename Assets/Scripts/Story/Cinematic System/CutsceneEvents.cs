using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.Events;

public class CutsceneEvents : MonoBehaviour
{
    [Title("Conditions")]
    public bool triggerEventsIfCutsceneSkipped;
    [SerializeField] bool playOnEnable = true;
    [Title("Jump Object")]
    [SerializeField] bool doJump;
    [ShowIf("doJump")]
    [SerializeField] List<JumpGameObject> jumpingObjects;
    [Title("Warp Object")]
    [SerializeField] bool doWarp;
    [ShowIf("doWarp")]
    [SerializeField] List<WarpGameObject> warpingObjects;
    [Title("Restore Vitals")]
    [SerializeField] bool restorePartyVitals;
    [ShowIf("restorePartyVitals")]
    [Tooltip("Dialogue to play before restore vitals event triggered")]
    [SerializeField] Dialogue restoreVitalsPreDialogue;
    [Space(10)]
    [ShowIf("restorePartyVitals")]
    [Range(0, 100)]
    [SerializeField] int hpRestorePercentage;
    [ShowIf("restorePartyVitals")]
    [Range(0, 100)]
    [SerializeField] int spRestorePercentage;
    [ShowIf("restorePartyVitals")]
    [Range(0, 100)]
    [SerializeField] int fpRestorePercentage;
    [Title("Load Functions")]
    [SerializeField] bool returnToTitleScreen = false;
    [Title("External Events")]
    [SerializeField] bool invokeExternalEvents;
    [ShowIf("invokeExternalEvents")]
    [SerializeField] UnityEvent externalEvents;

    bool eventsTriggered = false;
    bool dialoguePlayed = false;
    bool subscribedToDialogueEndEvent = false;

    [System.Serializable]
    public class JumpGameObject
    {
        public GameObject jumpingObject;
        public Transform origin;
        public Transform destination;
        [Header("Numbers")]
        public float duration;
        public float jumpPower;
        public int numOfJumps;
    }

    [System.Serializable]
    public class WarpGameObject
    {
        public GameObject warpObject;
        public Transform destination;
    }

    private void OnEnable()
    {
        if(playOnEnable)
            TriggerEvents(false);
    }

    public void TriggerEvents(bool triggerViaCinematicSkip)
    {
        if (eventsTriggered) { return; }

        eventsTriggered = true;

        if (doJump)
            DoJump();

        if (doWarp)
            DoWarp();

        if (restorePartyVitals)
            RestoreVitals();

        if (invokeExternalEvents)
            externalEvents?.Invoke();

        if (returnToTitleScreen)
            ReturnToTitleScreen();
    }

    private void RestoreVitals()
    {
        if (restoreVitalsPreDialogue && !dialoguePlayed)
        {
            dialoguePlayed = true;
            subscribedToDialogueEndEvent = true;

            DialogueManager.Instance.PlayDialogue(restoreVitalsPreDialogue, false);
            DialogueManager.Instance.DialogueEnded += RestoreVitals;
            return;
        }
        else if (subscribedToDialogueEndEvent)
        {
            DialogueManager.Instance.DialogueEnded -= RestoreVitals;
            subscribedToDialogueEndEvent = false;
        }

        foreach (PlayerGridUnit player in PartyManager.Instance.GetAllPlayerMembersInWorld())
        {
            int hpGain = player.CharacterHealth().GetVitalValueFromPercentage(hpRestorePercentage, CharacterHealth.Vital.HP);
            int spGain = player.CharacterHealth().GetVitalValueFromPercentage(spRestorePercentage, CharacterHealth.Vital.SP);
            int fpGain = player.CharacterHealth().GetVitalValueFromPercentage(fpRestorePercentage, CharacterHealth.Vital.FP);

            player.CharacterHealth().OuterCombatRestore(hpGain, spGain, fpGain);
        }

        
        AudioManager.Instance.PlaySFX(SFXType.PotionPowerUp);
        //Reset in case you wanna trigger event multiple times.
        dialoguePlayed = false;

        if (restoreVitalsPreDialogue) 
        {
            //I assume this dialogue to be played post cinematic, so revert to player controls.
            ControlsManager.Instance.SwitchCurrentActionMap("Player");
        } 
            
    }


    private void DoJump()
    {
        foreach (JumpGameObject jumpGameObject in jumpingObjects)
        {
            GameObject jumpingObj = jumpGameObject.jumpingObject;

            jumpingObj.transform.position = jumpGameObject.origin.position;
            jumpingObj.transform.rotation = jumpGameObject.origin.rotation;

            jumpingObj.transform.DOJump(jumpGameObject.destination.position, jumpGameObject.jumpPower, jumpGameObject.numOfJumps, jumpGameObject.duration);
            jumpingObj.transform.DORotate(jumpGameObject.destination.rotation.eulerAngles, jumpGameObject.duration);
        }
    }

    private void DoWarp()
    {
        foreach(WarpGameObject warpGameObject in warpingObjects)
        {
            GameObject warpingObject = warpGameObject.warpObject;
            ARDynamicObstacle dynamicObstacle = warpingObject.GetComponentInChildren<ARDynamicObstacle>();

            if(dynamicObstacle)
            {
                Debug.Log("Warping Dynamic Obstacle");
                dynamicObstacle.Warp(warpGameObject.destination);
            }
            else
            {
                warpingObject.transform.position = warpGameObject.destination.position;
                warpingObject.transform.rotation = warpGameObject.destination.rotation;
            }
        }
    }

    private void ReturnToTitleScreen()
    {
        GameManager.Instance.ConfirmQuit();
    }
    public void ResetEvent()
    {
        eventsTriggered = false;
    }
}
