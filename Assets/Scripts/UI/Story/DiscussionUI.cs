using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class DiscussionUI : MonoBehaviour, IControls
{
    [Header("Data")]
    [SerializeField] StoryCharacter discussionOwner;
    [Header("Components")]
    [SerializeField] GameObject discussionUICam;
    [SerializeField] GameObject dialogueCam;
    [Space(10)]
    [SerializeField] CanvasGroup canvasGroup;
    [Header("UI")]
    [SerializeField] Transform optionHeader;
    [SerializeField] TextMeshProUGUI[] discussionTexts = new TextMeshProUGUI[6];
    [Space(10)]
    [SerializeField] List<Transform> controlHeaders;

    List<DiscussionTopic> discussionTopics = new List<DiscussionTopic>();

    const string myActionMap = "Discussion";

    private void Awake()
    {
        foreach (Transform controlHeader in controlHeaders)
        {
            ControlsManager.Instance.AddControlHeader(controlHeader);
        }

        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }

    private void OnEnable()
    {
        HUDManager.Instance.HideHUDs();
        UpdateUI();

        //Set UI.
        gameObject.SetActive(true);
        discussionUICam.SetActive(true);

        ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    public void PlayDiscussion(TextMeshProUGUI discussionText)
    {
        DiscussionTopic discussionToPlay = discussionTopics.Find((discussion) => discussion.subject == discussionText.text);

        canvasGroup.alpha = 0;

        discussionUICam.SetActive(false);
        dialogueCam.SetActive(true);

        DialogueManager.Instance.PlayDialogue(discussionToPlay.dialogue, false);
        StoryManager.Instance.DiscussionComplete(discussionToPlay);

        DialogueManager.Instance.DialogueEnded += OnDiscussionComplete;
    }

    private void OnDiscussionComplete()
    {
        DialogueManager.Instance.DialogueEnded -= OnDiscussionComplete;

        discussionUICam.SetActive(true);
        dialogueCam.SetActive(false);

        UpdateUI();
        canvasGroup.alpha = 1;
    }

    private void UpdateUI()
    {
        discussionTopics = StoryManager.Instance.GetAvailableDiscussions(discussionOwner);

        List<DiscussionTopic> availableTopics = new List<DiscussionTopic>(discussionTopics);

        foreach (Transform child in optionHeader)
        {
            int index = child.GetSiblingIndex();
            bool isAvailableSlot = discussionTexts[index].color == Color.white;

            if (!isAvailableSlot)
            {
                child.gameObject.SetActive(true);
                continue;
            }

            bool activate = index <= discussionTopics.Count;
            child.gameObject.SetActive(activate);

            if (activate)
            {
                discussionTexts[index].text = availableTopics[0].subject;
                availableTopics.RemoveAt(0);
            }
        }
    }

    private void OnSelectOption(int optionIndex)
    {
        Transform selectedOption = optionHeader.GetChild(optionIndex);
        if (!selectedOption.gameObject.activeInHierarchy) { return; } //Option has been disabled.

        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
        selectedOption.GetComponent<Button>().onClick?.Invoke();
    }

    public bool HasDiscussions()
    {
        return StoryManager.Instance.GetAvailableDiscussions(discussionOwner).Count > 0;
    }

    private void Exit()
    {
        AudioManager.Instance.PlaySFX(SFXType.TabBack);
        ControlsManager.Instance.SwitchCurrentActionMap("Player");
        discussionUICam.SetActive(false);
        gameObject.SetActive(false);
        HUDManager.Instance.ShowActiveHud();
    }

    private void OnDisable()
    {
        discussionUICam.SetActive(false);
    }

    private void OnListenForOption(InputAction.CallbackContext context)
    {
        if (!context.performed) { return; }

        switch (context.action.name)
        {
            case "Option1":
                OnSelectOption(0);
                break;
            case "Option2":
                OnSelectOption(1);
                break;
            case "Option3":
                OnSelectOption(2);
                break;
            case "Option4":
                OnSelectOption(3);
                break;
            case "Option5":
                OnSelectOption(4);
                break;
            case "Option6":
                OnSelectOption(5);
                break;
        }
    }

    private void OnExit(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            Exit();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnExit;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnListenForOption;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnExit;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnListenForOption;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }
}
