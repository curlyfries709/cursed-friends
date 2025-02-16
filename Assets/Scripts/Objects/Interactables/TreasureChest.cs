using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class TreasureChest : Lootable, ISaveable
{
    [Header("Treasure Chest")]
    [SerializeField] Transform chestHead;
    [SerializeField] protected GameObject lootVFX;
    [Header("Animation")]
    [SerializeField] Vector3 openedChestRotation;
    [SerializeField] float rotateAnimTime = 0.5f;
    [Header("Unlock Requirements")]
    [SerializeField] protected Talent talentRequiredToUnlock;
    [Range(1, 20)]
    [SerializeField] protected int talentRequiredLevel = 1;
    [ShowIf("talentRequiredToUnlock")]
    [Header("Ineligible Dialogue")]
    [Tooltip("Dialogue to Display if talent level not high enough")]
    [SerializeField] Dialogue ineligibleLevelDialogue;
    [Space(5)]
    [Tooltip("Dialogue to Display if talent not unlocked...Meaning Party member has yet to join")]
    [SerializeField] Dialogue ineligibleTalentDialogue;

    //Saving Data
    [SerializeField, HideInInspector]
    private ChestState chestState = new ChestState();
    bool isDataRestored = false;

    protected bool locked = false;

    protected override void Awake()
    {
        base.Awake();

        locked = talentRequiredToUnlock;
        sfxOnBeginLoot = SFXType.OpenChest;
    }

    protected void UnlockChest(bool inCombat)
    {
        if (StoryManager.Instance.TriggerFirstTimeEvent("TreasureChest")) { return; } 

        if (MeetLockedByTalentReq())
        {
            if (TryUnlockChest())
            {
                locked = false;
                BeginLoot(inCombat);
            }         
        }
        else
        {
            TriggerLockedDialogue();
        }
    }

    protected bool MeetLockedByTalentReq()
    {
        if (locked)
        {
            return TalentProgressionManager.Instance.SucceedLevelCheck(talentRequiredToUnlock, talentRequiredLevel);
        }

        return true;
    }

    protected virtual bool TryUnlockChest()
    {
        return true;
    }

    public override void HandleInteraction(bool inCombat)
    {
        UnlockChest(inCombat);
    }

    protected void TriggerLockedDialogue()
    {
        int currentTalentLevel = TalentProgressionManager.Instance.GetTalentLevelWithBoost(talentRequiredToUnlock);

        if (currentTalentLevel == 0) //Means Party Member hasn't joined Yet.
        {
            DialogueManager.Instance.PlayDialogue(ineligibleTalentDialogue, false);
        }
        else
        {
            DialogueManager.Instance.PlayDialogue(ineligibleLevelDialogue, false);
        }
    }

    public override void OnLootEmpty()
    {
        OpenChest(false);
    }

    private void OpenChest(bool isRestore)
    {
        DisableInteraction();
        lootVFX.SetActive(false);

        if (isRestore)
        {
            chestHead.localRotation = Quaternion.Euler(openedChestRotation);
        }
        else
        {
            chestHead.DOLocalRotate(openedChestRotation, rotateAnimTime).SetEase(Ease.OutSine);
            AudioManager.Instance.PlaySFX(SFXType.OpenChest);
        }
        
    }

    //Saving
    [System.Serializable]
    public class ChestState
    {
        public bool locked = false;
        public List<int> takenItemIndices = new List<int>();
    }


    public virtual object CaptureState()
    {
        chestState.locked = locked;
        chestState.takenItemIndices = takenItemIndices;

        return SerializationUtility.SerializeValue(chestState, DataFormat.Binary);
    }

    public virtual void RestoreState(object state)
    {
        isDataRestored = true;
        if (state == null) { return; }

        byte[] bytes = state as byte[];
        chestState = SerializationUtility.DeserializeValue<ChestState>(bytes, DataFormat.Binary);

        //Restore Date
        locked = chestState.locked;
        takenItemIndices = chestState.takenItemIndices;

        RestoreChestState();
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }

    protected void RestoreChestState()
    {
        items = new List<Item>(originalItems);

        foreach (int index in takenItemIndices)
        {
            items.RemoveAt(index);
        }

        if (items.Count == 0)
        {
            OpenChest(true);
        }
        else
        {
            if (!interactionCollider)
                SetCollider();

            //Close Chest
            interactionCollider.enabled = true;
            chestHead.localRotation = Quaternion.Euler(new Vector3(0, 0, -180));
            lootVFX.SetActive(true);
        }
    }
}
