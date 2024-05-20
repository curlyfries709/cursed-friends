using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class PhotoshootAnimator : MonoBehaviour
{
    [Title("Universal Setups")]
    [SerializeField] GameObject counterSetup;
    [SerializeField] GameObject firedUpSetup;
    [Space(10)]
    [SerializeField] GameObject beatdownSetup;
    [Title("Player Setups")]
    [SerializeField] GameObject blessingSetup;


    Animator animator;
    const int setupTransformIndex = 1;

    GameObject model;
    GameObject activeSetup = null;

    private void Awake()
    {
        model = transform.GetChild(0).gameObject;
        animator = model.GetComponent<Animator>();

        model.SetActive(false);

        model.transform.localPosition = Vector3.zero;
        model.transform.localEulerAngles = Vector3.zero;
    }


    //UI ACTIVATION
    public void PlayCounterUI()
    {
        ActivateSetupUI(counterSetup, "CounterUI");
    }

    public void PlayBlessingUI()
    {
        ActivateSetupUI(blessingSetup, "Bless");
    }

    public void PlayFiredUpUI()
    {
        ActivateSetupUI(firedUpSetup, "Fired");
    }

    public void PlayBeatdownUI()
    {
        if (beatdownSetup)
        {
            ActivateSetupUI(beatdownSetup, "Beatdown");
        }
        else
        {
            PlayFiredUpUI();
        }
    }

    public void DeactivateSet()
    {
        FantasyCombatManager.Instance.ActivatePhotoshootSet(false);
        model.SetActive(false);
        activeSetup.SetActive(false);
    }

    private void ActivateSetupUI(GameObject setup, string triggerName)
    {
        FantasyCombatManager.Instance.ActivatePhotoshootSet(true);

        //Set
        activeSetup = setup;
        transform.position = activeSetup.transform.GetChild(setupTransformIndex).position;
        transform.rotation = activeSetup.transform.GetChild(setupTransformIndex).rotation;

        //Activate
        model.SetActive(true);
        setup.SetActive(true);

        //Trigger Anims
        animator.SetLayerWeight(2, 1);
        animator.SetTrigger(triggerName);
    }
}
