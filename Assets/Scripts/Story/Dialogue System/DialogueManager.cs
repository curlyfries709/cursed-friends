using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using AnotherRealm;
using Sirenix.Serialization;

public class DialogueManager : MonoBehaviour, ISaveable
{
    public static DialogueManager Instance { get; private set; }
    [SerializeField] DialogueUI dialogueUI;

    public Dialogue currentDialogue { get; private set; } = null;
    DialogueNode bonusDialogueChoiceParentNode = null;

    public DialogueNode currentNode { get; private set; }
    public bool isChoosing { get; private set; }

    //Saving Data
    [SerializeField, HideInInspector]
    private List<string> playedDialogue = new List<string>();
    bool isDataRestored = false;

    public Action<ChoiceReferences> ChoiceWithReferenceSelected;
    public Action DialogueEnded;

    const string myActionMap = "Dialogue";

    private void Awake()
    {
        Instance = this;
    }

    public void PlayDialogue(Dialogue dialogue, bool storeDialogue)
    {
        if(currentDialogue == dialogue) { return; } //Means Dialogue is already playing.

        if (storeDialogue && !playedDialogue.Contains(dialogue.name))
        {
            playedDialogue.Add(dialogue.name);
        }

        HUDManager.Instance.HideHUDs();

        currentDialogue = dialogue;
        currentNode = currentDialogue.GetRootNode();

        ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);

        //Always Skip Start Node.
        Next();
    }

    public void Next()
    {
        if (!HasNext())
        {
            if (bonusDialogueChoiceParentNode)
            {
                currentNode = bonusDialogueChoiceParentNode;
                bonusDialogueChoiceParentNode = null;
            }
            else
            {
                EndDialogue();
                return;
            }
        }

        int numOfChildren = currentNode.childNodes.Count;

        if (numOfChildren > 1)
        {  
            //Highest Priority First
            foreach (DialogueNode dialogue in currentNode.childNodes.OrderByDescending((node) => node.nodePriorityNum))
            {
                if(HandyFunctions.CanUnlockConditionalEvent(dialogue.conditionsToUnlockNode))
                {
                    if (dialogue.isDialogueChoice)
                    {
                        isChoosing = true;
                        break;
                    }
                    else
                    {
                        currentNode = dialogue;
                        break;
                    }
                }
            }
        }
        else
        {
            //In This Situation current Node will only have One Child.
            currentNode = currentNode.childNodes[0];
        }

        if(currentNode && !isChoosing && currentNode.completeObjective)
            StoryManager.Instance.CompleteObjective(currentNode.completeObjective);

        //Call Complete Objective.
        dialogueUI.UpdateUI();
    }

    public bool HasNext()
    {
        if (currentNode == null)
        {
            return false;
        }

        return currentNode.childNodes.Count > 0;
    }


    //Select Choice
    public void SelectChoice(DialogueNode selectedNode)
    {
        isChoosing = false;

        bonusDialogueChoiceParentNode = selectedNode.isBonusDialogueChoice ? currentNode : null;
        currentNode = selectedNode;

        if (selectedNode.choiceReference != ChoiceReferences.None)
        {
            ChoiceWithReferenceSelected?.Invoke(selectedNode.choiceReference);
        }

        if (selectedNode.completeObjective)
            StoryManager.Instance.CompleteObjective(selectedNode.completeObjective);
        
        Next();
    }




    private IEnumerable<DialogueNode> FilterChoices(IEnumerable<DialogueNode> choices)
    {
        foreach (DialogueNode choice in choices)
        {
            if (!choice.isDialogueChoice) { continue; }

            if (choice.HasConditions())
            {
                if (choice.showChoiceEvenIfIneligible)
                {
                    yield return choice;
                }

                bool eligible = true;

                foreach (EventCondition condition in choice.conditionsToUnlockNode)
                {
                    if (HandyFunctions.EvaluateEventCondition(condition) == true)
                    {
                        continue;
                    }
                    else
                    {
                        eligible = false;
                        break;
                    }
                }

                if (eligible)
                {
                    yield return choice;
                }
            }
            else
            {
                yield return choice;
            }
        }
    }



    public void EndDialogue()
    {
        if (currentNode == null)
        {
            Debug.Log("CURRENT DIALOGUE NODE IS NULL");
            /*currentDialogue = null;
            dialogueUI.UpdateUI();
            DialogueEnded?.Invoke();
            return;*/
        }

        //Hide UI
        currentNode = null;
        currentDialogue = null;

        isChoosing = false;

        dialogueUI.UpdateUI();

        ControlsManager.Instance.RevertToPreviousControls();

        DialogueEnded?.Invoke();
    }

    public void DialogueInterrupted()
    {
        if (IsDialoguePlaying())
        {
            bonusDialogueChoiceParentNode = null;

            if (playedDialogue.Contains(currentDialogue.name))
            {
                playedDialogue.Remove(currentDialogue.name);
            }

            EndDialogue();
        }
    }

    public bool SkipCutsceneDialogue(Dialogue dialogue)
    {
        DialogueNode currentNodeToCheck = dialogue.GetRootNode();

        if(dialogue == currentDialogue)
            currentNodeToCheck = bonusDialogueChoiceParentNode ? bonusDialogueChoiceParentNode : currentNode;

        DialogueNode firstNodeChecked = currentNodeToCheck;
        //int counter = 0;

        while (true)
        {
            /*counter++;

            if (counter == 150)
            {
                Debug.Log("Skip Cutscene Dialogue hit counter!");
                break;
            }*/

            if (IsNodeSkippable(currentNodeToCheck))
            {
                //If Node Skipped, Trigger Events on Node
                if (currentNodeToCheck.completeObjective)
                    StoryManager.Instance.CompleteObjective(currentNode.completeObjective);

                if (currentNodeToCheck.childNodes.Count == 0)
                {
                    break;
                }
                else
                {
                    int numOfChildren = currentNodeToCheck.childNodes.Count;

                    if (numOfChildren > 1) //If Multiple children, select node based on previous decisions. 
                    {
                        foreach (DialogueNode node in currentNodeToCheck.childNodes.OrderByDescending((node) => node.nodePriorityNum))
                        {
                            if (HandyFunctions.CanUnlockConditionalEvent(node.conditionsToUnlockNode))
                            {
                                currentNodeToCheck = node;
                                break;
                            }
                        }
                    }
                    else
                    {
                        currentNodeToCheck = currentNodeToCheck.childNodes[0];
                    } 
                }
            }
            else
            {
                //Skip to this node.
                currentDialogue = dialogue;
                currentNode = currentNodeToCheck;

                //Enable Dialogue Controls
                ControlsManager.Instance.SwitchCurrentActionMap(myActionMap);
                Next(); //Call Next to trigger the choice.

                //return false if choice with choice reference must be made.
                return false;
            }
        }

        //return true if dialogue successfully skipped
        return true;
    }



    private bool IsNodeSkippable(DialogueNode dialogueNode)
    {
        int numOfChildren = dialogueNode.childNodes.Count;

        //if a decision with choice reference must be made, (meaning it's important) , do not skip
        if (numOfChildren > 1)
        {
            foreach (DialogueNode node in dialogueNode.childNodes.OrderByDescending((node) => node.nodePriorityNum))
            {
                if(HandyFunctions.CanUnlockConditionalEvent(node.conditionsToUnlockNode))
                {
                    if (node.isDialogueChoice && node.choiceReference != ChoiceReferences.None)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    //Getters

    public bool IsDialoguePlaying()
    {
        return currentDialogue != null;
    }

    public bool HasDialoguePlayed(Dialogue dialogue)
    {
        return playedDialogue.Contains(dialogue.name);
    }

    public IEnumerable<DialogueNode> GetUIChoices()
    {
        //Loop Through Children. Check if Eligible to Show Choice.
        return FilterChoices(currentNode.childNodes);
    }

    //Saving
    public object CaptureState()
    {
        return SerializationUtility.SerializeValue(playedDialogue, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;
        if (state == null) { return; }

        byte[] bytes = state as byte[];
        playedDialogue = SerializationUtility.DeserializeValue<List<string>>(bytes, DataFormat.Binary);
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }

}
