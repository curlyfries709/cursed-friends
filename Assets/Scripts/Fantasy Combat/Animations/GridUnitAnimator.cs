using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
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

public class GridUnitAnimator : MonoBehaviour
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
    [HideInInspector] public int animIDSpeed;
    [HideInInspector] public int animIDGrounded;
    [HideInInspector] public int animIDJump;
    [HideInInspector] public int animIDFreeFall;
    [HideInInspector] public int animIDMotionSpeed;
    [HideInInspector] public int animIDIdle;
    [HideInInspector] public int animIDHit;
    [HideInInspector] public int animIDBackStep;
    [HideInInspector] public int animIDBurn;
    [HideInInspector] public int animIDKO;
    [HideInInspector] public int animIDGuarding;
    [HideInInspector] public int animIDEvade;
    [HideInInspector] public int animIDEvadeReturn;
    [HideInInspector] public int animIDCounter;
    [HideInInspector] public int animIDIdleReflect;
    [HideInInspector] public int animIDStealth;
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
    [HideInInspector] public int animIDTexting;
    [HideInInspector] public int animIDSleeping;
    [HideInInspector] public int animIDRevive;

    //Caches
    Animator animator;
    public CharacterGridUnit myUnit { get; private set; }

    List<GameObject> model = new List<GameObject>();
    List<GameObject> equipmentModels = new List<GameObject>();

    //Events
    //Action arrowAttackEvent;
    //Action activateArrowEvent;

    //Variables
    bool isFrozen = false;
    bool cancelSkillFeedbackDisplay = false;

    bool isLeader = false;

    [HideInInspector] public bool beginHealthCountdown = false;

    [System.Serializable]
    public class BodyTransform
    {
        public BodyPart bodyPart;
        public Transform bodyTransform;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        myUnit = GetComponentInParent<CharacterGridUnit>();

        isLeader = (myUnit is PlayerGridUnit player) && PartyData.Instance.GetLeader() == player;

        SetModels();
    }

    private void Start()
    {
        AssignAnimationIDs();
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
    public void Freeze(bool freeze)
    {
        isFrozen = freeze;
        animator.speed = freeze ? 0 : 1;
    }

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

    public void ChangeLayers(int newLayer)
    {
        for(int i = 0; i < animator.layerCount; i++)
        {
            if(i == newLayer)
            {
                animator.SetLayerWeight(i, 1);
            }
            else
            {
                animator.SetLayerWeight(i, 0);
            }
        }
    }

    //Setters

    //Handy Dandy Animator Functions

    //Set Speeds

    public void SetSpeed(float value)
    {
        animator.SetFloat(animIDSpeed, value);
    }

    public void SetMotionSpeed(float value)
    {
        //animator.SetFloat(animIDMotionSpeed, value);
    }

    //Bools
    public void SetBool(int animID, bool value)
    {
        animator.SetBool(animID, value);
    }

    public void SetSearching(bool value)
    {
        animator.SetBool(animIDSearching, value);
    }

    //Triggers
    public void SetTrigger(int animID)
    { 
        if(!isFrozen)
            animator.SetTrigger(animID);
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


    public void Idle()
    {
        animator.SetTrigger(animIDIdle);
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

    public void ShowModel(bool show)
    {
        foreach (GameObject obj in model)
        {
            obj.SetActive(show);
        }
    }

    public void HideStatusEffectsVFX()
    {
        foreach (BodyTransform bodyTransform in statusEffectVfxBodyTransforms)
        {
            bodyTransform.bodyTransform.gameObject.SetActive(false);
        }
    }

    private void SetModels()
    {
        foreach (Transform child in transform)
        {
            model.Add(child.gameObject);
        }

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
    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        animIDIdle = Animator.StringToHash("Idle");
        animIDHit = Animator.StringToHash("Hit");
        animIDBackStep = Animator.StringToHash("BackStep");
        animIDBurn = Animator.StringToHash("Burn");
        animIDKO = Animator.StringToHash("KO");
        animIDGuarding = Animator.StringToHash("Guarding");
        animIDEvade = Animator.StringToHash("Evade");
        animIDEvadeReturn = Animator.StringToHash("EvadeReturn");
        animIDCounter = Animator.StringToHash("Counter");
        animIDIdleReflect = Animator.StringToHash("IdleReflect");
        animIDStealth = Animator.StringToHash("Stealth");
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
        animIDTexting = Animator.StringToHash("Texting");
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
        return equipment.Weapon();
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

}
