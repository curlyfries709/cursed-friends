using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{

    //Animation IDs
    [HideInInspector] public int animIDSpeed;
    [HideInInspector] public int animIDGrounded;
    [HideInInspector] public int animIDJump;
    [HideInInspector] public int animIDFreeFall;
    [HideInInspector] public int animIDMotionSpeed;
    [HideInInspector] public int animIDIdle;
    [HideInInspector] public int animIDTexting;
    [HideInInspector] public int animIDStealth;

    protected Animator animator;
    protected List<GameObject> model = new List<GameObject>();

    protected bool isFrozen = false;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        SetModels();
    }


    private void Start()
    {
        AssignAnimationIDs();
    }

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

    //Triggers
    public void Freeze(bool freeze)
    {
        isFrozen = freeze;
        animator.speed = freeze ? 0 : 1;
    }


    public void SetTrigger(int animID)
    {
        if (!isFrozen)
            animator.SetTrigger(animID);
    }

    public void Idle()
    {
        animator.SetTrigger(animIDIdle);
    }


    public void ChangeLayers(int newLayer)
    {
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (i == newLayer)
            {
                animator.SetLayerWeight(i, 1);
            }
            else
            {
                animator.SetLayerWeight(i, 0);
            }
        }
    }

    public void ShowModel(bool show)
    {
        foreach (GameObject obj in model)
        {
            obj.SetActive(show);
        }
    }

    protected virtual void SetModels()
    {
        foreach (Transform child in transform)
        {
            model.Add(child.gameObject);
        }
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    protected virtual void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        animIDIdle = Animator.StringToHash("Idle");
        animIDTexting = Animator.StringToHash("Texting");
        animIDStealth = Animator.StringToHash("Stealth");
    }
}
