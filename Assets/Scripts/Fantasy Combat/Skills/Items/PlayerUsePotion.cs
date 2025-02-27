using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Sirenix.OdinInspector;
using MoreMountains.Feedbacks;
public class PlayerUsePotion : PlayerBaseSkill
{
    [Title("USE POTION DATA")]
    public BasicPotionEffect potionEffect;
    [Space(10)]
    [SerializeField] MMF_Player feedbackToPlay;
    [Header("Timers")]
    [SerializeField] float acitvatePotionTime = 0.5f;
    [SerializeField] float itemUseExtensionTime = 0.5f;
    [Space(10)]
    [SerializeField] float passPotionAnimDelay = 0.3f;
    [Header("Orbit Data")]
    [SerializeField] float orbitPointRotationSpeed = 15;
    [SerializeField] Vector3 orbitStartingRotation;

    //Cache
    public CharacterGridUnit potionDrinker { get; private set; }
    CinemachineBlendListCamera cmBlendlist;

    Transform orbitPoint;


    protected override void Awake()
    {
        base.Awake();
        cmBlendlist = blendListCamera.GetComponent<CinemachineBlendListCamera>();
    }

    public override void SkillSelected()
    {
        if (!skillTriggered)
        {
            GridVisual();
        }
        else
        {
            orbitPoint.Rotate(Vector3.down * orbitPointRotationSpeed * Time.deltaTime);
        }
    }

    public override bool TryTriggerSkill()
    {
        if (CanTriggerSkill(!canTargetSelf))
        {
            if (canTargetSelf)
            {
                potionDrinker = myUnit;
            }
            else
            {
                potionDrinker = selectedUnits[0] as CharacterGridUnit;
            }

            SetupCamera();

            BeginSkill(0, 0, true);//Unit Position Updated here

            //Remove Item From Inventory
            InventoryManager.Instance.UsedPotionInCombat(player, potionEffect.potionData);

            //StartCoroutine
            if (canTargetSelf)
            {
                StartCoroutine(UsePotionRoutine());
            }
            else
            {
                StartCoroutine(PassPotionRoutine());
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    IEnumerator UsePotionRoutine()
    {
        //Setup Cam
        ActivateVisuals(true);
        feedbackToPlay?.PlayFeedbacks();

        myUnit.unitAnimator.TriggerSkill("Drink");

        yield return new WaitForSeconds(acitvatePotionTime);

        //Activate Potion Effect
        potionEffect.ActivateEffect(potionDrinker, myUnit, collectionManager);
    }

    IEnumerator PassPotionRoutine()
    {
        //Setup Cam
        ActivateVisuals(true);
        feedbackToPlay?.PlayFeedbacks();

        myUnit.unitAnimator.TriggerSkill("Throw");

        yield return new WaitForSeconds(passPotionAnimDelay);

        if(!potionDrinker.Health().isKnockedDown && !potionDrinker.Health().isKOed)
            potionDrinker.unitAnimator.TriggerSkill("Drink");

        yield return new WaitForSeconds(acitvatePotionTime - passPotionAnimDelay);

        orbitPoint = potionDrinker.statusEffectCamTarget;

        yield return new WaitForSeconds(passPotionAnimDelay);
        //Activate Potion Effect
        potionEffect.ActivateEffect(potionDrinker, myUnit, collectionManager);
    }

    private void SetupCamera()
    {
        //Set Rotations.
        myUnit.statusEffectCamTarget.localRotation = Quaternion.Euler(orbitStartingRotation);
        potionDrinker.statusEffectCamTarget.localRotation = Quaternion.Euler(orbitStartingRotation);

        orbitPoint = myUnit.statusEffectCamTarget;
        UpdateBlendListSettings();
    }

    private void UpdateBlendListSettings()
    {
        if (canTargetSelf)
        {
            cmBlendlist.LookAt = orbitPoint;
            cmBlendlist.Follow = orbitPoint;
        }
        else
        {
            //Throw Cam
            cmBlendlist.transform.GetChild(0).GetComponent<CinemachineVirtualCamera>().LookAt = orbitPoint;
            cmBlendlist.transform.GetChild(0).GetComponent<CinemachineVirtualCamera>().Follow = orbitPoint;

            //Use Cam
            cmBlendlist.transform.GetChild(1).GetComponent<CinemachineVirtualCamera>().LookAt = potionDrinker.statusEffectCamTarget;
            cmBlendlist.transform.GetChild(1).GetComponent<CinemachineVirtualCamera>().Follow = potionDrinker.statusEffectCamTarget;
        }
        
    }

    public override void OnSkillInterrupted(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult != BattleResult.Restart) { return; }

        StopAllCoroutines();
        feedbackToPlay?.StopFeedbacks();
        ActivateVisuals(false);
    }

    public override void SkillCancelled(bool showActionMenu = true)
    {
        base.SkillCancelled(false);

        //Go back to Items List
        collectionManager.OpenItemMenu(player, true);
    }


    //Helpers
    public void Setup(PlayerGridUnit playerGridUnit, BasicPotionEffect potionEffect, FantasyCombatCollectionManager collectionManager)
    {
        myUnit = playerGridUnit;
        player = playerGridUnit;

        myUnitMoveTransform = playerGridUnit.transform;
        moveTransformGridCollider = myUnit.gridCollider;
        
        this.potionEffect = potionEffect;
        this.collectionManager = collectionManager;

        this.potionEffect.dataDisplayExtension = itemUseExtensionTime;

        //Update Skill Name
        string textToAppend = "Use ";

        if (!canTargetSelf)
        {
            textToAppend = "Pass ";
        }

        skillName = textToAppend + potionEffect.potionData.itemName;
        canTargetKOEDUnits = potionEffect.potionData.canRevive;

    }


}
