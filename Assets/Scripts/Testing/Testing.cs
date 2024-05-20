using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Sirenix.Serialization;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;
using Cinemachine;
using UnityEngine.Rendering.HighDefinition;

public class Testing : MonoBehaviour
{
    public static Testing Instance { get; private set; }
    //[SerializeField] Transform warp;
    [SerializeField] PlayerGridUnit player;
    [SerializeField] Transform target;
    [SerializeField] CinemachineCollider cmCollider;
    [SerializeField] CinemachineVirtualCamera camWithCollider;
    [SerializeField] CustomPassVolume customPass;
    [SerializeField] bool invokeEvents;
    public UnityEvent eventToTrigger;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnTEst;
        //eventToTrigger?.Invoke();

        //Debug.Log("Kira Animator Binding: " + director.GetGenericBinding(kiraAnimator));
        //Debug.Log("Keenan Animator Binding: " + director.GetGenericBinding(keenanAnimator));

        //director.

        
    }

    private void OnEnable()
    {
        if (invokeEvents)
            eventToTrigger.Invoke();
    }

    private void Update()
    {
        //Debug.Log("Is VIew Obscrued" + cmCollider.IsTargetObscured(camWithCollider));
        //customPass.SetActive(cmCollider.IsTargetObscured(camWithCollider));
        
    }

    private void ChangeAnim()
    {
        //var bindings = data.Timeline.outputs;
        //var bindings = director.playableAsset.outputs;
    }


    private void OnTEst(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "Test")
            {
               
            }
        }
    }


}
