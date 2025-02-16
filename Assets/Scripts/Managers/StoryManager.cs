using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Sirenix.Serialization;
using MoreMountains.Feedbacks;
using UnityEngine.InputSystem;



public class StoryManager : MonoBehaviour, ISaveable, IControls
{
    public static StoryManager Instance { get; private set; }

    [Header("New Game Config")]
    [SerializeField] CinematicTrigger openingCinematic;
    [SerializeField] float openingCinematicDelay = 0.5f;
    [Header("Tutorial")]
    [SerializeField] Transform tutorialHeader;
    [SerializeField] bool enableTutorials = true;
    [Header("Discussion")]
    [SerializeField] DiscussionTopic treasureChestDiscussion;
    [SerializeField] DiscussionTopic monsterChestDiscussion;
    [Header("First Time Event Dialogue")]
    [SerializeField] Dialogue firstTreasureChestDialogue;
    [SerializeField] Dialogue firstMonsterChestDialogue;

    //Saving Data
    [SerializeField, HideInInspector]
    private StoryState storyState = new StoryState();
    bool isDataRestored = false;

    //Cache
    public bool isCursed { get; private set; } = false;
    public bool isTutorialPlaying { get; private set; } = false;

    bool isHudActive = false;

    //Storage
    public List<int> StoredChoiceReferences { get; private set; } =  new List<int>();
    public List<DiscussionTopic> unlockedDiscussions { get; private set; } = new List<DiscussionTopic>();

    Dictionary<Quest, List<Objective>> completedQuests = new Dictionary<Quest, List<Objective>>();
    Dictionary<Quest, List<Objective>> currentQuestsObjective = new Dictionary<Quest, List<Objective>>();

    List<Objective> currentlyTrackedObjectives = new List<Objective>();
    List<string> triggeredFirstTimeInteractableEvents = new List<string>();

    List<int> tutorialQueue = new List<int>();

    public Action<bool> ActivateCinematicMode;

    //Tutorial
    Tutorial currentTutorial;
    int currentTutorialPage = 0;


    const string myActionKey = "GridMenu";

    //Event
    public Action TutorialComplete;

    private void Awake()
    {
        if (!Instance)
            Instance = this;
    }

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput(myActionKey, this);
        SavingLoadingManager.Instance.BeginNewGameCinematic += BeginNewGameCinematic;
    }

    private void Start()
    {
        DialogueManager.Instance.ChoiceWithReferenceSelected += StoreChoiceReference;
    }

    private void BeginNewGameCinematic()
    {
        StartCoroutine(OpeningCinematicRoutine());
    }

    IEnumerator OpeningCinematicRoutine()
    {
        openingCinematic.ReadyCinematic();
        AudioManager.Instance.PlaySFX(SFXType.RealmTransition);
        yield return new WaitForSeconds(openingCinematicDelay);
        openingCinematic.PlayCinematic();
    }

    //Story EVents
    public void CursePlayers()
    {
        foreach(PlayerGridUnit player in PartyManager.Instance.GetAllPlayerMembersInWorld())
        {
            player.GetComponent<PlayerUnitStats>().OverrideBeingData(isCursed);
        }

        //TODO: ADD A CURSE PLAYER EVENT
        Debug.Log("Add a cursed player event to this");
    }

    public void CompleteObjective(Objective objectiveToComplete)
    {
        if (!currentQuestsObjective.ContainsKey(objectiveToComplete.quest)) { return; }

        bool removedFromTrackedObjectives = currentlyTrackedObjectives.Remove(objectiveToComplete);

        if (QuestCompleteCheck(objectiveToComplete))
        {
            //Event or whatever.

        }
        else
        {
            Quest quest = objectiveToComplete.quest;

            currentQuestsObjective[quest].Remove(objectiveToComplete);
            completedQuests[quest].Add(objectiveToComplete);

            foreach (Objective objective in GetNextObjectives(objectiveToComplete.nextObjectives))
            {
                currentQuestsObjective[quest].Add(objective);
                if (removedFromTrackedObjectives)
                    currentlyTrackedObjectives.Add(objective);
            }

            //Debug.Log("Update Active Quest");
        }

        if (removedFromTrackedObjectives)
            HUDManager.Instance.UpdateActiveQuest(currentlyTrackedObjectives);
    }

    public void BeginQuest(Quest quest)
    {
        if (currentQuestsObjective.ContainsKey(quest) || completedQuests.ContainsKey(quest)) { return; }

        currentQuestsObjective[quest] = new List<Objective>();
        completedQuests[quest] = new List<Objective>();
        
        foreach (Objective objective in GetNextObjectives(quest.startObjectives))
        {
            currentQuestsObjective[quest].Add(objective);
        }

        //Debug.Log("Beginning Quest: " + quest.title);
    }


    public void SetQuestAsActive(Quest quest)
    {
        if (!currentQuestsObjective.ContainsKey(quest)) { return; }

        currentlyTrackedObjectives.Clear();

        foreach (Objective objective in currentQuestsObjective[quest])
        {
            currentlyTrackedObjectives.Add(objective);
        }

        HUDManager.Instance.UpdateActiveQuest(currentlyTrackedObjectives);

        //Debug.Log("Set Active Quest: " + quest.title);
    }

    private bool QuestCompleteCheck(Objective currentObjective)
    {
        Quest quest = currentObjective.quest;
        bool isComplete = currentObjective.nextObjectives.Count == 0 && currentQuestsObjective[quest].Count - 1 <= 0;

        if (isComplete)
        {
            completedQuests[currentObjective.quest].Add(currentObjective);
            currentQuestsObjective.Remove(currentObjective.quest);
        }

        return isComplete;
    }


    private List<Objective> GetNextObjectives(List<Objective> objectives)
    {
        List<Objective> newObjectives = new List<Objective>();

        foreach(Objective objective in objectives)
        {
            if (objective.choiceReferenceToUnlock == ChoiceReferences.None || MadeDecision(objective.choiceReferenceToUnlock))
            {
                newObjectives.Add(objective);
            }
        }

        return newObjectives;
    }

    public void AddNewDiscussion(DiscussionTopic discussion)
    {
        if (IsExpired(discussion.expirationDate) || unlockedDiscussions.Contains(discussion)) { return; }
        unlockedDiscussions.Add(discussion);
    }

    public void DiscussionComplete(DiscussionTopic discussion)
    {
        unlockedDiscussions.Remove(discussion);
    }

    public List<DiscussionTopic> GetAvailableDiscussions(StoryCharacter discussionOwner)
    {
        return unlockedDiscussions.Where((discussion) => discussion.discussionOwner == discussionOwner).ToList();
    }

    private void UpdateDiscussions() //Called on New Day
    {
        for (int i = unlockedDiscussions.Count - 1; i >= 0; i--)
        {
            DiscussionTopic discussion = unlockedDiscussions[i];
            if (IsExpired(discussion.expirationDate))
            {
                unlockedDiscussions.RemoveAt(i);
            }
        }
    }



    private bool IsExpired(GameDate date)
    {
        DateTime dateToCheck = new DateTime(date.year, date.month, date.day);
        return CalendarManager.Instance.currentDate > dateToCheck;
    }

    private void StoreChoiceReference(ChoiceReferences choiceRef)
    {
        int choiceInt = (int)choiceRef;

        if (!StoredChoiceReferences.Contains(choiceInt))
            StoredChoiceReferences.Add(choiceInt);
    }

    //First Time Events
    public bool TriggerFirstTimeEvent(string interactableType)
    {
        if (triggeredFirstTimeInteractableEvents.Contains(interactableType))
        {
            return false; //Means it has already been triggered.
        }

        //Add to list
        triggeredFirstTimeInteractableEvents.Add(interactableType);

        //Trigger Event
        switch (interactableType)
        {
            case "MonsterChest":
                DialogueManager.Instance.PlayDialogue(firstMonsterChestDialogue, false);
                AddNewDiscussion(monsterChestDiscussion);
                break;
            case "TreasureChest":
                DialogueManager.Instance.PlayDialogue(firstTreasureChestDialogue, false);
                AddNewDiscussion(treasureChestDiscussion);
                break;
        }

        return true;
    }

    private void OnDisable()
    {
        DialogueManager.Instance.ChoiceWithReferenceSelected -= StoreChoiceReference;
        SavingLoadingManager.Instance.BeginNewGameCinematic -= BeginNewGameCinematic;
    }

    //Tutorial

    public bool PlayTutorial(int tutorialIndex)
    {
        return PlayTutorial(tutorialHeader.GetChild(tutorialIndex).GetComponent<Tutorial>());
    }

    public void PlayTutorialEvent(Tutorial tutorial)
    {
        PlayTutorial(tutorial);
    }

    public bool PlayTutorial(Tutorial tutorial)
    {
        if (tutorial.played || !enableTutorials)
            return false;

        isTutorialPlaying = true;

        ControlsManager.Instance.SwitchCurrentActionMap(this);

        //HUDManager.Instance.HideHUDs();

        currentTutorialPage = 0;
        currentTutorial = tutorial;

        if (MMTimeManager.Instance.CurrentTimeScale != 0)
        {
            GameManager.Instance.FreezeGame();
            isHudActive = HUDManager.Instance.IsHUDEnabled();
        }
            
        UpdateTutorial(0);
        currentTutorial.Fade(true);

        return true;
    }

    private void UpdateTutorial(int indexChange)
    {
        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        currentTutorialPage = Mathf.Clamp(currentTutorialPage + indexChange, 0, currentTutorial.pageHeader.childCount - 1);

        foreach(Transform page in currentTutorial.pageHeader)
        {
            page.gameObject.SetActive(page.GetSiblingIndex() == currentTutorialPage);
        }
    }

    private void ExitTutorial()
    {
        currentTutorial.played = true;

        if (tutorialQueue.Count > 0)
        {
            currentTutorial.Fade(false);

            PlayTutorial(tutorialQueue[0]);
            tutorialQueue.RemoveAt(0);
            return;
        }

        if(isHudActive)
            HUDManager.Instance.ShowActiveHud();

        currentTutorial.Fade(false);
        MMTimeManager.Instance.SetTimeScaleTo(1);

        AudioManager.Instance.PlaySFX(SFXType.TabBack);

        if (currentTutorial.newActionMapName != "")
        {
            ControlsManager.Instance.SwitchCurrentActionMap(currentTutorial.newActionMapName);
        }
        else
        {
            ControlsManager.Instance.RevertToPreviousControls();
        }

        isTutorialPlaying = false;

        TutorialComplete?.Invoke();
    }

    public void AddTutorialToQueue(int index)
    {
        if(!tutorialQueue.Contains(index) && !tutorialHeader.GetChild(index).GetComponent<Tutorial>().played)
            tutorialQueue.Add(index);
    }


    public bool MadeDecision(ChoiceReferences choiceReference)
    {
        return StoredChoiceReferences.Contains((int)choiceReference);
    }

    private PlayerStateMachine GetPlayerStateMachine()
    {
        return PlayerSpawnerManager.Instance.GetPlayerStateMachine();
    }

    //Input
    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleL" || context.action.name == "CycleR")
            {
                int indexChange = context.action.name == "CycleR" ? 1 : -1;

                UpdateTutorial(indexChange);
            }
        }
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            UpdateTutorial(1);
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            ExitTutorial();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCycle;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }



    //Saving
    [System.Serializable]
    public class StoryState
    {
        //Date
        public int currentDay;
        public int currentMonth;
        public int currentYear;
        public Period currentPeriod;

        //Story
        public bool isCursed;

        public Dictionary<string, List<string>> completedQuests = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> currentQuestsObjective = new Dictionary<string, List<string>>();
        public List<string> currentlyTrackedObjectives = new List<string>();

        //Discussion
        public List<string> discussionTopics = new List<string>();
        public List<int> choiceReferences = new List<int>();

        //Events
        public List<string> triggeredInteractEvents = new List<string>();
    }


    public object CaptureState()
    {
        //storyState.currentDay = currentDate.Day;
        //storyState.currentMonth = currentDate.Month;
        //storyState.currentYear = currentDate.Year;

        //storyState.currentPeriod = currentPeriod;
        storyState.isCursed = isCursed;

        storyState.discussionTopics = unlockedDiscussions.ConvertAll((topic) => topic.name);

        storyState.completedQuests = completedQuests.ToDictionary(k => k.Key.name, k => k.Value.ConvertAll((objective) => objective.GetObjectiveID()));
        storyState.currentQuestsObjective = currentQuestsObjective.ToDictionary(k => k.Key.name, k => k.Value.ConvertAll((objective) => objective.GetObjectiveID()));

        storyState.currentlyTrackedObjectives = currentlyTrackedObjectives.ConvertAll((objective) => objective.GetObjectiveID());

        storyState.choiceReferences = StoredChoiceReferences;
        storyState.triggeredInteractEvents = triggeredFirstTimeInteractableEvents;

        return SerializationUtility.SerializeValue(storyState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if(state == null) 
        {
            //NewGameSetup();
            return; 
        }

        byte[] bytes = state as byte[];
        storyState = SerializationUtility.DeserializeValue<StoryState>(bytes, DataFormat.Binary);

        //Restore Date
        //currentDate = new DateTime(storyState.currentYear, storyState.currentMonth, storyState.currentDay);
        //currentPeriod = storyState.currentPeriod;

        //Restore Story Data
        isCursed = storyState.isCursed;

        //Restore Quests & Objectives
        TheCache.Instance.PopulateQuestDict(storyState.completedQuests, ref completedQuests);
        TheCache.Instance.PopulateQuestDict(storyState.currentQuestsObjective, ref currentQuestsObjective);

        currentlyTrackedObjectives = TheCache.Instance.GetObjectivesByID(storyState.currentlyTrackedObjectives);

        //Update Quest Hud
        HUDManager.Instance.UpdateActiveQuest(currentlyTrackedObjectives);

        //Restore Discussion
        unlockedDiscussions = TheCache.Instance.GetDiscussionsByName(storyState.discussionTopics);


        //Restore Choice References
        StoredChoiceReferences = storyState.choiceReferences;

        if(unlockedDiscussions.Count > 0)
        {
           // Debug.Log(unlockedDiscussions[0].name);
        }

        //Restore events
        triggeredFirstTimeInteractableEvents = storyState.triggeredInteractEvents;

        //Curse Player
        CursePlayers();
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
