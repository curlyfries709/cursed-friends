using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using System.Linq;
using UnityEngine.InputSystem.DualShock;

public class ControlsManager : MonoBehaviour
{
    public static ControlsManager Instance { get; private set; }

    [SerializeField] PlayerInput playerInput;
    [SerializeField] float switchActionMapDelay = 0.25f;
    //[Space(10)]
    //[SerializeField] List<string> actionMapsThatShouldntEnableEnemyCinematicMode;

    //Look Input
    CinemachineInputProvider cinemachineInputProvider;

    //Controls Dict
    Dictionary<string, IControls> controlsDict = new Dictionary<string, IControls>();
    List<IControls> subscribedControls = new List<IControls>();
    List<Transform> controlUIHeaders = new List<Transform>();

    const string noControlsActionMap = "NoControls";

    string previousActionMap;

    string currentControlsKey = "Player";
    string previousControlsKey;

    char duplicateActionMapNameSeparator = ' ';

    bool changingControls = false;

    private void Awake()
    {
        if(!Instance)
            Instance = this;

        SetDebug();
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChanged;
    }

    private void SetDebug()
    {
        Debug.unityLogger.logEnabled = Debug.isDebugBuild;
        //Debug.developerConsoleVisible = Debug.isDebugBuild;
    }

    private void OnDeviceChanged(InputDevice inputDevice, InputDeviceChange inputDeviceChange)
    {
        /*Debug.Log("Device Change. New Index: " + GetUIControlsIndex());
        Debug.Log("New Device: " + inputDevice.name);
        Debug.Log("Current Control Scheme: " + playerInput.currentControlScheme);*/

        for (int i = controlUIHeaders.Count - 1; i >= 0; i--)
        {
            Transform header = controlUIHeaders[i];

            if(!UpdateControlHeader(header)) //Means it has been destroyed.
            {
                controlUIHeaders.RemoveAt(i);
            }
        }
    }



    public void SubscribeToPlayerInput(string actionMapName, IControls yourControls)
    {
        if (subscribedControls.Contains(yourControls) && controlsDict.ContainsKey(actionMapName)) { return; }

        string dictName = actionMapName;
        int count = 0;

        while (controlsDict.ContainsKey(dictName))
        {
            dictName = actionMapName + duplicateActionMapNameSeparator + count;
            count++;
        }

        subscribedControls.Add(yourControls);
        controlsDict[dictName] = yourControls;
        playerInput.actions.FindActionMap(actionMapName).Disable();
    }

    public void RemoveIControls(IControls yourControls)
    {
        bool wasRemoved = subscribedControls.Remove(yourControls);

        if (!wasRemoved) { return; }

        foreach (var item in controlsDict.Where(kvp => kvp.Value == yourControls).ToList())
        {
            controlsDict.Remove(item.Key);
        }
    }

    public void AddControlHeader(Transform header)
    {
        if (header && !controlUIHeaders.Contains(header))
        {
            controlUIHeaders.Add(header);
            //Update it at Start.
            UpdateControlHeader(header);
        }
    }

    public bool UpdateControlHeader(Transform controlHeader)
    {
        //Remove Destroyed Headers
        if (controlHeader == null || controlHeader.childCount < 3)
        {
            //Debug.Log("Removing Header");

            return false;
        }

        foreach (Transform child in controlHeader)
        {
            child.gameObject.SetActive(child.GetSiblingIndex() == GetUIControlsIndex());
        }

        return true;
    }

    public void RemoveControlHeader(Transform header)
    {
        if (controlUIHeaders.Contains(header))
        {
            controlUIHeaders.Remove(header);
        }
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChanged;
    }

    public void SwitchCurrentActionMap(string newActionMap)
    {
        if(newActionMap == "Menu")
        {
            Debug.LogError("WHEN SWITCHING TO MENU ACTION MAP, PLEASE USE ICONTROLS SWITCH OVERRIDE");
            return;
        }

        BeginSwitchControlRoutine(newActionMap, newActionMap);
    }

    public void SwitchCurrentActionMap(IControls controls)
    {
        string dictKey = "NoControls";

        foreach(var pair in controlsDict)
        {
            if(pair.Value == controls)
            {
                dictKey = pair.Key;
                break;
            }
        }

        //THis method only called if uses Duplicate Action Map Name.
        string newActionMap = dictKey.Split(duplicateActionMapNameSeparator)[0];
        BeginSwitchControlRoutine(newActionMap, dictKey);
    }


    public void RevertToPreviousControls()
    {
        //Debug.Log("Previous Action Map: " + previousActionMap);
        BeginSwitchControlRoutine(previousActionMap, previousControlsKey);
    }

    public void DisableControls(bool toggleBehaviour = true)
    {
        string currentActionMap = "";

        if (playerInput.currentActionMap != null)
            currentActionMap = playerInput.currentActionMap.name;

        if (currentActionMap != "" && controlsDict.ContainsKey(currentControlsKey))
        {
            controlsDict[currentControlsKey].ListenToInput(false);
            playerInput.currentActionMap.Disable();
        }

        playerInput.SwitchCurrentActionMap(noControlsActionMap);

        if (!toggleBehaviour) { return; }

        ToggleBehaviourBasedOnControls(noControlsActionMap);
        ToggleCameraControls(noControlsActionMap);
    }

    private void BeginSwitchControlRoutine(string newActionMap, string dictKey)
    {
        /*if (!changingControls)
            StartCoroutine(SwitchControlsRoutine(newActionMap, dictKey));*/

        SwitchControls(newActionMap, dictKey);
    }

    private void SwitchControls(string newActionMap, string dictKey)
    {
        string currentActionMap = "";

        if (playerInput.currentActionMap != null)
            currentActionMap = playerInput.currentActionMap.name;

        if (currentActionMap != "" && currentActionMap != noControlsActionMap && currentActionMap != newActionMap)
        {
            previousActionMap = currentActionMap;
            previousControlsKey = currentControlsKey;
        }

        if (currentActionMap != "" && controlsDict.ContainsKey(currentControlsKey))
        {
            controlsDict[currentControlsKey].ListenToInput(false);
            playerInput.currentActionMap.Disable();
        }

        ToggleBehaviourBasedOnControls(newActionMap);
        playerInput.SwitchCurrentActionMap(newActionMap);

        ToggleCameraControls(newActionMap);

        if (controlsDict.ContainsKey(dictKey))
        {
            playerInput.currentActionMap.Enable();
            controlsDict[dictKey].ListenToInput(true);
            currentControlsKey = dictKey;
        }
    }

    IEnumerator SwitchControlsRoutine(string newActionMap, string dictKey)
    {
        changingControls = true;

        string currentActionMap = "";

        if (playerInput.currentActionMap != null)
            currentActionMap = playerInput.currentActionMap.name;


        if (currentActionMap != "" && currentActionMap != noControlsActionMap && currentActionMap != newActionMap)
        {
            previousActionMap = currentActionMap;
            previousControlsKey = currentControlsKey;
        }

        if (currentActionMap != "" && controlsDict.ContainsKey(currentControlsKey))
        {
            controlsDict[currentControlsKey].ListenToInput(false);
            playerInput.currentActionMap.Disable();
        }

        ToggleBehaviourBasedOnControls(newActionMap);
        DisableControls(false);

        yield return new WaitForSecondsRealtime(switchActionMapDelay);

        playerInput.SwitchCurrentActionMap(newActionMap);

        ToggleCameraControls(newActionMap);

        if (controlsDict.ContainsKey(dictKey))
        {
            playerInput.currentActionMap.Enable();
            controlsDict[dictKey].ListenToInput(true);
            currentControlsKey = dictKey;
        }

        changingControls = false;
        
    }

    private void ToggleBehaviourBasedOnControls(string newActionMap)
    {
        bool inCombat = FantasyCombatManager.Instance && FantasyCombatManager.Instance.InCombat();

        if (inCombat || newActionMap == "Player")
        {
            if(newActionMap == "Player")
                HUDManager.Instance.ShowActiveHud();

            StoryManager.Instance.ActivateCinematicMode?.Invoke(false);

            if(newActionMap == "Player" || (inCombat && FantasyCombatManager.Instance.GetIsCombatInteractionAvailable()))
                InteractionManager.Instance.ShowInteractCanvas?.Invoke(true);
        }
        else
        {
            InteractionManager.Instance?.ShowInteractCanvas?.Invoke(false);
            StoryManager.Instance?.ActivateCinematicMode?.Invoke(true);
        }
    }

    private void ToggleCameraControls(string newActionMap)
    {
        if (cinemachineInputProvider && cinemachineInputProvider.gameObject.activeInHierarchy)
        {
            var actionMapActions = playerInput.actions.FindActionMap(newActionMap).actions;
            bool hasLookControl =  actionMapActions.Count > 0 && actionMapActions.IndexOf((action) => action.name == "Look") > -1;
            cinemachineInputProvider.enabled = hasLookControl;

            if(cinemachineInputProvider.TryGetComponent(out BaseCameraController cameraController))
            {
                cameraController.enabled = hasLookControl;
            }
        }
    }

    public void OnControlledCamLive(CinemachineInputProvider cinemachineInputProvider)
    {
        this.cinemachineInputProvider = cinemachineInputProvider;
        ToggleCameraControls(playerInput.currentActionMap.name);
    }

    //Getters

    public PlayerInput GetPlayerInput()
    {
        return playerInput;
    }

    public bool IsMoveInputAnalog()
    {
        return playerInput.currentControlScheme == "PlaystationController" || playerInput.currentControlScheme == "XboxController";
    }

    public bool IsCurrentDeviceKeyboardMouse()
    {
        //Current Control Scheme updates based on what input was pressed whereas Gamepad.current detects if Gamepad connected even when not being used.
        return playerInput.currentControlScheme == "Keyboard&Mouse";
    }

    public int GetUIControlsIndex()
    {
        if (Gamepad.current == null)
        {
            return 0;
        }
        //else if (playerInput.currentControlScheme == "PlaystationController")
        else if (Gamepad.current is DualShockGamepad)
        {
            return 2;
        }
        else //Assume it's an Xbox Controller.
        {
            return 1;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(hasFocus);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
