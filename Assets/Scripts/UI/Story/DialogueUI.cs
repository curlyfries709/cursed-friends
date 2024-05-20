using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MoreMountains.Feedbacks;
using AnotherRealm;
using System.Linq;

public class DialogueUI : MonoBehaviour, IControls
{
    [Header("Feedback")]
    [SerializeField] MMF_Player camShakeFeedback;
    [Header("Dialogue Style")]
    [SerializeField] GameObject mainDialogueUI;
    [SerializeField] GameObject thinkDialogueUI;
    [SerializeField] GameObject multiSpeakerDialogueUI;
    [SerializeField] GameObject dialogueChoiceUI;
    [Header("Main Dialogue")]
    [SerializeField] TextMeshProUGUI mainSpeakerName;
    [SerializeField] TextMeshProUGUI mainDialogueText;
    [Space(10)]
    [SerializeField] Image mainSpeakerBG;
    [SerializeField] Image mainSpeakerPortrait;
    [Space(10)]
    [SerializeField] GameObject mainNextIcon;
    [Header("Think Dialogue")]
    [SerializeField] TextMeshProUGUI thinkDialogueText;
    [SerializeField] GameObject thinkNextIcon;
    [Header("Choice Dialogue")]
    [SerializeField] TextMeshProUGUI choiceSpeakerName;
    [SerializeField] TextMeshProUGUI choiceDialogueText;
    [Space(10)]
    [SerializeField] Image choiceSpeakerBG;
    [SerializeField] Image choiceSpeakerPortrait;
    [Header("Choices")]
    [SerializeField] List<GameObject> choiceSequence;
    [Space(10)]
    [SerializeField] Color selectedChoiceBGColor;
    [SerializeField] Color defaultChoiceBGColor;
    [Space(5)]
    [SerializeField] Color bonusChoiceTextColor;
    [SerializeField] Color defaultChoiceTextColor;
    [Header("Multi Speaker Dialogue")]
    [SerializeField] Transform multiSpeakerPotraitHeader;
    [Space(10)]
    [SerializeField] TextMeshProUGUI multiSpeakerText;
    [SerializeField] GameObject multiSpeakerNextIcon;

    const string myActionMap = "Dialogue";

    //Choice Index
    int currentChoiceIndex = 0;

    List<DialogueNode> selectedBonusChoices = new List<DialogueNode>();
    Coroutine currentRoutine = null;

    //Cache
    TextMeshProUGUI currentTextArea;
    string currentDialogue;
    GameObject currentNextIcon;

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
    }

    public void UpdateUI()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
           
        HideAllCanvases();

        if (!DialogueManager.Instance.IsDialoguePlaying()) { return; }

        DialogueNode currentNode = DialogueManager.Instance.currentNode;

        if (DialogueManager.Instance.isChoosing)
        {
            currentChoiceIndex = 0;

            SetDialogueArea(currentNode, choiceDialogueText, choiceSpeakerName, choiceSpeakerBG, choiceSpeakerPortrait, null);
            BuildChoiceList();
            return;
        }

        if (currentNode.isThinkBubble)
        {
            thinkDialogueUI.SetActive(true);
            SetDialogueArea(currentNode, thinkDialogueText, null, null, null, thinkNextIcon);
        }
        else if (currentNode.hasMultipleSpeakers)
        {
            multiSpeakerDialogueUI.SetActive(true);

            UpdateMultipleSpeakers(currentNode);
            SetDialogueArea(currentNode, multiSpeakerText, mainSpeakerName, null, null, multiSpeakerNextIcon);
        }
        else
        {
            mainDialogueUI.SetActive(true);
            SetDialogueArea(currentNode, mainDialogueText, mainSpeakerName, mainSpeakerBG, mainSpeakerPortrait, mainNextIcon);
        }

        //Shake Cam if true.
        if (currentNode.shakeCam)
            camShakeFeedback.PlayFeedbacks();
    }

     private void BuildChoiceList()
     {
        dialogueChoiceUI.SetActive(true);

        

        foreach (GameObject choiceObj in choiceSequence)
        {
            int index = choiceObj.transform.GetSiblingIndex();

            bool shouldShow = index < GetChoices().Count;
            choiceObj.SetActive(shouldShow);

            if (!shouldShow) { continue; }

            DialogueNode currentChoice = GetChoices()[index];
            TextMeshProUGUI choiceText = choiceObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

            choiceText.color = currentChoice.isBonusDialogueChoice ? bonusChoiceTextColor : defaultChoiceTextColor;
            choiceText.text = currentChoice.text;
        }

        UpdateSelectedChoice(0);
     }

    private void UpdateSelectedChoice(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        CombatFunctions.UpdateListIndex(indexChange, currentChoiceIndex, out currentChoiceIndex, GetChoices().Count);

        foreach (GameObject choiceObj in choiceSequence)
        {
            int index = choiceObj.transform.GetSiblingIndex();
            bool isSelected = index == currentChoiceIndex;

            if (choiceObj.activeInHierarchy)
            {
                choiceObj.GetComponent<Image>().color = isSelected ? selectedChoiceBGColor : defaultChoiceBGColor;
                choiceObj.transform.GetChild(1).gameObject.SetActive(isSelected);
            }
        }
    }

    //Handy dandy functions

    private void SetDialogueArea(DialogueNode dialogueNode, TextMeshProUGUI dialogueText, TextMeshProUGUI speakerText, Image speakerBG, Image speakerPotrait, GameObject nextIcon)
    {
        if (speakerText)
        {
            speakerText.text = dialogueNode.speaker.characterName;
            LayoutRebuilder.ForceRebuildLayoutImmediate(speakerText.transform.parent as RectTransform);
        }
            
        if (speakerBG)
            speakerBG.sprite = dialogueNode.speaker.potraitBackground;

        if (speakerPotrait)
            speakerPotrait.sprite = dialogueNode.speaker.moodSprites.First((item) => item.mood == dialogueNode.mood).sprite;

        if (nextIcon)
        {
            currentRoutine = StartCoroutine(TypeSentence(dialogueText, dialogueNode.text, nextIcon));
        }
        else
        {
            dialogueText.text = dialogueNode.text;
        }
            
    }

    private void UpdateMultipleSpeakers(DialogueNode dialogueNode)
    {
        foreach(Transform child in multiSpeakerPotraitHeader)
        {
            int index = child.GetSiblingIndex();
            bool shouldShow = index < dialogueNode.speakers.Count;
          

            child.gameObject.SetActive(shouldShow);

            if (!shouldShow) { continue; }

            StoryCharacter character = dialogueNode.speakers[index];

            Image speakerBG = child.GetChild(0).GetChild(0).GetComponent<Image>();
            Image speakerPotrait = child.GetChild(1).GetChild(0).GetComponent<Image>();

            speakerBG.sprite = character.potraitBackground;
            speakerPotrait.sprite = character.moodSprites.First((item) => item.mood == dialogueNode.mood).sprite;
        }
    }

    private List<DialogueNode> GetChoices()
    {
        //Remove all selected bonus choices.
        return DialogueManager.Instance.GetUIChoices().Except(selectedBonusChoices).ToList();
    }

    private void SelectChoice()
    {
        DialogueNode selectedChoice = GetChoices()[currentChoiceIndex];

        if (selectedChoice.isBonusDialogueChoice)
        {
            selectedBonusChoices.Add(selectedChoice);
        }
        else
        {
            selectedBonusChoices.Clear();
        }

        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
        DialogueManager.Instance.SelectChoice(selectedChoice);
    }

     public void ContinueConversation(InputAction.CallbackContext context)
     {
        if(context.action.name != "Next") { return; }

         if (context.performed)
         {
             if (DialogueManager.Instance.isChoosing)
             {
                 SelectChoice();
             }
             else
             {
                
                DisplayOrContinueDialogue();
             }
         }
     }

     public void Scroll(InputAction.CallbackContext context)
     {
         if (context.performed && DialogueManager.Instance.isChoosing)
         {
            if(context.action.name == "ScrollU")
            {
                UpdateSelectedChoice(-1);
            }
            else if (context.action.name == "ScrollD")
            {
                UpdateSelectedChoice(1);
            }
         }
     }

    private void OnSkip(InputAction.CallbackContext context)
    {
        if (context.action.name != "Skip") { return; }

        if (context.performed && CinematicManager.Instance.isCinematicPlaying && !DialogueManager.Instance.isChoosing)
        {
            CinematicManager.Instance.SkipCurrentCinematic();
        }
    }


    private void HideAllCanvases()
    {
        mainDialogueUI.SetActive(false);
        thinkDialogueUI.SetActive(false);
        multiSpeakerDialogueUI.SetActive(false);
        dialogueChoiceUI.SetActive(false);
    }

    //Input
    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += Scroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += ContinueConversation;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSkip;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= Scroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= ContinueConversation;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSkip;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

    //Dialogue Animation
    IEnumerator TypeSentence(TextMeshProUGUI textArea, string sentence, GameObject nextIcon)
    {
        SetCurrentTextData(textArea, sentence, nextIcon);

        nextIcon.SetActive(false);
        textArea.text = "";

         foreach (char letter in sentence.ToCharArray())
         {
             textArea.text = textArea.text + letter;
             yield return null;
         }

        //Show Caret Icon.
        nextIcon.SetActive(true);
        currentRoutine = null;
    }

    private void DisplayOrContinueDialogue()
    {
        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

        if (currentRoutine == null) 
        {
            //If DIalogue fully displayed, continue.
            DialogueManager.Instance.Next();
            return; 
        }

        //On Next Button, if dialogue still typing, fully display it before skipping to next dialogue
        StopCoroutine(currentRoutine);
        
        currentTextArea.text = currentDialogue;
        currentNextIcon.SetActive(true);

        currentRoutine = null;
    }

    private void SetCurrentTextData(TextMeshProUGUI textArea, string sentence, GameObject nextIcon)
    {
        currentTextArea = textArea;
        currentDialogue = sentence;
        currentNextIcon = nextIcon;
    }
     
}
