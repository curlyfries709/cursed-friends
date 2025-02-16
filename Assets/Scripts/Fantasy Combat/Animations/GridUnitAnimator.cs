using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;

public enum BodyPart
{
    Eyes,
    Hips,
    Head,
    Chest
}

public class GridUnitAnimator : CharacterAnimator
{
    [Title("Speed")]
    [SerializeField] float animatorSlowMoSpeed = 0.3f;
    [Title("Components")]
    [SerializeField] EnemyIntiateCombat freeRoamAttackObject;
    [SerializeField] Equipment equipment;
    [Title("Bones")]
    [SerializeField] Transform rightHand;
    [SerializeField] Transform spineBone;
    [Title("VFX Transforms")]
    [SerializeField] List<BodyTransform> statusEffectVfxBodyTransforms;
    [Title("Feedbacks")]
    [SerializeField] MMF_Player enjoyPotionFeedback;

    // animation IDs
    [HideInInspector] public int animIDHit;
    [HideInInspector] public int animIDBackStep;
    [HideInInspector] public int animIDBurn;
    [HideInInspector] public int animIDKO;
    [HideInInspector] public int animIDGuarding;
    [HideInInspector] public int animIDEvade;
    [HideInInspector] public int animIDEvadeReturn;
    [HideInInspector] public int animIDCounter;
    [HideInInspector] public int animIDIdleReflect;

    [HideInInspector] public int animIDArm;
    [HideInInspector] public int animIDUnarm;
    [HideInInspector] public int animIDSearching;
    [HideInInspector] public int animIDChase;
    [HideInInspector] public int animIDBarrel;
    [HideInInspector] public int animIDRoamAttack;
    [HideInInspector] public int animIDKnockdown;
    [HideInInspector] public int animIDChain;
    [HideInInspector] public int animIDFiredUp;
    [HideInInspector] public int animIDAmbushed;
    [HideInInspector] public int animIDSleeping;
    [HideInInspector] public int animIDRevive;

    //Caches
    public CharacterGridUnit myUnit { get; private set; }

    List<GameObject> equipmentModels = new List<GameObject>();

    //Events
    //Action arrowAttackEvent;
    //Action activateArrowEvent;

    //Variables

    bool cancelSkillFeedbackDisplay = false;

    bool isLeader = false;

    [HideInInspector] public bool beginHealthCountdown = false;

    [System.Serializable]
    public class BodyTransform
    {
        public BodyPart bodyPart;
        public Transform bodyTransform;
    }

    protected override void Awake()
    {
        base.Awake();

        myUnit = GetComponentInParent<CharacterGridUnit>();
        isLeader = (myUnit is PlayerGridUnit player) && PartyManager.Instance.GetLeader() == player;
    }


    //Animation Events
    public void ShowDamageFeedback(int disableSlowMo)
    {
        if (cancelSkillFeedbackDisplay)
        {
            cancelSkillFeedbackDisplay = false;
            return;
        }

        IDamageable.unitAttackComplete?.Invoke(beginHealthCountdown);

        if (disableSlowMo == 0)
        {
            ActivateSlowmo();
        }
    }

    public void TriggerEvasionEvent()
    {
        //Always Trigger Evade Event
        Evade.Instance.PlayEvadeEvent();
    }

    public void AmbushAttackComplete()
    {
        BattleStarter.Instance.PlayerStartCombatAttackComplete?.Invoke(myUnit.GetComponent<EnemyStateMachine>());
    }

    public void AmbushTargetHit()
    {
        BattleStarter.Instance.TargetHit();
    }

    public void EnableHitbox(int enable)
    {
        if(enable == 0)
        {
            freeRoamAttackObject.ActivateHitBox(false);
        }
        else
        {
            freeRoamAttackObject.ActivateHitBox(true);
        }
        
    }

    public void PlayFootstepSFX()
    {
        if (!isLeader) { return; }

        AudioManager.Instance.PlaySFX(SFXType.GrassStep);
    }


    public void PreparePOFKnockout()
    {
        POFDirector.Instance.PrepareEnemyKO();
    }

    public void POFPose()
    {
        POFDirector.Instance.ShowIntiatorUI();
    }

    public void ShowWeapon()
    {
        ShowWeapon(true);
    }

    public void HideWeapon()
    {
        ShowWeapon(false);
    }

    /*public void ShowArrow()
    {
        activateArrowEvent?.Invoke();
    }

    public void ShootArrow()
    {
        arrowAttackEvent?.Invoke();
    }*/

    //Feedbacks
    public void PlayEnjoyPotionSFX()
    {
        enjoyPotionFeedback?.PlayFeedbacks();
    }

    //Methods

    public void ActivateSlowmo()
    {
        animator.speed = animatorSlowMoSpeed;
    }

    public void ReturnToNormalSpeed()
    {
        if (!isFrozen)
        {
            animator.speed = 1;
        } 
    }

    public bool IsWeaponDrawn()
    {
        AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0) && currentInfo.IsTag("Draw"))
        {
            return true;
        }
        
        return false;
    }

    public bool IsWeaponSheathed()
    {
        AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0) && currentInfo.IsTag("Sheath"))
        {
            return true;
        }

        return false;
    }


    //More Handy Dandy Animator Functions
    public void SetSearching(bool value)
    {
        animator.SetBool(animIDSearching, value);
    }


    public void TriggerSkill(string triggerName)
    {
        if (!isFrozen)
            animator.SetTrigger(triggerName);
    }

    public void KO()
    {
        Freeze(false);
        SetTrigger(animIDKO);
    }

    public void Ambush()
    {
        ShowWeapon();
        animator.SetTrigger("Ambush");
    }

    public void AttackIntruder()
    {
        ShowWeapon();
        animator.SetTrigger(animIDRoamAttack);
    }

    public void Counter()
    {
        animator.SetTrigger(animIDCounter);
    }

    public void Hit()
    {
        if (!animator.GetCurrentAnimatorStateInfo(1).IsTag("Hurt"))
        {
            animator.SetTrigger(animIDHit);
        }
    }

    public void IdleBeforeReflect()
    {
        animator.SetTrigger(animIDIdleReflect);
    }

    public void Burn()
    {
        animator.SetTrigger(animIDBurn);
    }

    public void StartChain()
    {
        animator.SetTrigger("StartChain");
    }


    public void FiredUp()
    {
        animator.ResetTrigger(animIDIdle);
        animator.SetTrigger(animIDFiredUp);
    }

    public void DrawWeapon(bool draw)
    {
        if (draw)
        {
            animator.SetTrigger(animIDArm);
        }
        else
        {
            animator.SetTrigger(animIDUnarm);
        }
    }

    public void PrepareToTriggerSkill()
    {
        SetSpeed(0);
        CancelDisplaySkillFeedbackEvent(false);
    }

    public void CancelDisplaySkillFeedbackEvent(bool cancel)
    {
        cancelSkillFeedbackDisplay = cancel;
    }



    public void HideStatusEffectsVFX()
    {
        foreach (BodyTransform bodyTransform in statusEffectVfxBodyTransforms)
        {
            bodyTransform.bodyTransform.gameObject.SetActive(false);
        }
    }

    protected override void SetModels()
    {
        base.SetModels();

        if (!HasWeapon()) { return; }
        //Equipment Models
        foreach(Transform header in equipment.GetEquipmentHeaders())
        {
            foreach(Transform child in header)
            {
                equipmentModels.Add(child.gameObject);
            }
        }
    }

    public void ResetAnimatorToCombatState()
    {
        ResetAnimator(1);
    }

    public void ResetAnimatorToRoamState()
    {
        ResetAnimator(0);
    }

    private void ResetAnimator(int newLayer)
    {
        gameObject.SetActive(false);
        gameObject.SetActive(true);
        ReturnToNormalSpeed();

        ChangeLayers(newLayer);
    }

    //Animation IDs
    

    protected override void AssignAnimationIDs()
    {
        base.AssignAnimationIDs();

        animIDHit = Animator.StringToHash("Hit");
        animIDBackStep = Animator.StringToHash("BackStep");
        animIDBurn = Animator.StringToHash("Burn");
        animIDKO = Animator.StringToHash("KO");
        animIDGuarding = Animator.StringToHash("Guarding");
        animIDEvade = Animator.StringToHash("Evade");
        animIDEvadeReturn = Animator.StringToHash("EvadeReturn");
        animIDCounter = Animator.StringToHash("Counter");
        animIDIdleReflect = Animator.StringToHash("IdleReflect");
        animIDArm = Animator.StringToHash("Arm");
        animIDUnarm = Animator.StringToHash("Unarm");
        animIDSearching = Animator.StringToHash("Searching");
        animIDChase = Animator.StringToHash("Chase");
        animIDBarrel = Animator.StringToHash("Barrel");
        animIDRoamAttack = Animator.StringToHash("RoamAttack");
        animIDKnockdown = Animator.StringToHash("Knockdown");
        animIDChain = Animator.StringToHash("Chain");
        animIDFiredUp = Animator.StringToHash("Fired");
        animIDAmbushed = Animator.StringToHash("Ambushed");
        animIDSleeping = Animator.StringToHash("Sleeping");
        animIDRevive = Animator.StringToHash("Revive");
    } 

    public void SetupArrowAttackEvent(Action showAction, Action fireAction)
    {
        //activateArrowEvent = showAction;
        //arrowAttackEvent = fireAction;
    }

    public Transform GetSpine()
    {
        return spineBone;
    }

    public Transform GetRightHand()
    {
        return rightHand;
    }

    public void ShowWeapon(bool show)
    {
        if (!HasWeapon()) { return; }

        foreach (Transform child in equipment.GetMainWeaponHeader())
        {
            child.gameObject.SetActive(show);
        }
    }

    public bool HasWeapon()
    {
        return equipment && equipment.Weapon();
    }

    public bool ShouldDrawWeapon()
    {
        if (HasWeapon())
        {
            return !equipment.GetMainWeaponHeader().GetChild(0).gameObject.activeInHierarchy;
        }

        return false;
    }

    public void HideAllEquipment()
    {
        foreach(GameObject child in equipmentModels)
        {
            child.SetActive(false);
        }
    }

    public List<GameObject> GetEquipment()
    {
        return equipmentModels;
    }

    public EnemyIntiateCombat GetEnemyBattleTrigger()
    {
        return freeRoamAttackObject;
    }

    public Transform GetStatusEffectVFXBodyTransform(BodyPart bodyPart)
    {
        return statusEffectVfxBodyTransforms.Find((item) => item.bodyPart == bodyPart).bodyTransform;
    }

    public void SetEquipment(Equipment equipment)
    {
        this.equipment = equipment;
        SetModels();
    }

}
