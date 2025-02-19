
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;
using System;
using Sirenix.Serialization;

public class EnemyPartialData
{
    public string enemyName;
    public Race race;
    public RaceType type = RaceType.Unknown;

    public List<ElementAffinity> knownElementAffinities = new List<ElementAffinity>();

    public List<string> knownDroppedItems = new List<string>();
    public List<string> otherKnownAffinities = new List<string>();

    public EnemyPartialData() { }

    public EnemyPartialData(EnemyPartialData dataToCopy)
    {
        enemyName = dataToCopy.enemyName;
        race = dataToCopy.race;
        type = dataToCopy.type;

        knownElementAffinities = new List<ElementAffinity>(dataToCopy.knownElementAffinities);

        knownDroppedItems = new List<string>(dataToCopy.knownDroppedItems);
        otherKnownAffinities = new List<string>(dataToCopy.otherKnownAffinities);
    }
}

public class EnemyDatabase : MonoBehaviour, IControls, ISaveable
{
    public static EnemyDatabase Instance { get; private set; }
    [Header("Values")]
    [SerializeField] int numOfMaterialAffinities = 3;
    [SerializeField] int numOfElementalAffinities = 6;
    [Header("GameObject")]
    [SerializeField] FadeUI analysisUI;
    [SerializeField] Transform modelHeader;
    [SerializeField] GameObject zeroItemAffinityHeader;
    [Header("Basic UI Data")]
    [SerializeField] TextMeshProUGUI analysisName;
    [SerializeField] TextMeshProUGUI analysisLevel;
    [SerializeField] TextMeshProUGUI analysisRace;
    [SerializeField] TextMeshProUGUI analysisType;
    [Space(5)]
    [SerializeField] List<Transform> controlHeaders;
    [Header("Extra Affinity Data")]
    [SerializeField] Transform itemAffinityHeader;
    [SerializeField] Transform ingridientsHeader;
    [Header("Affinity Header")]
    [SerializeField] Transform silverAffinity;
    [SerializeField] Transform goldAffinity;
    [SerializeField] Transform ironAffinity;
    [Space(10)]
    [SerializeField] Transform fireAffinity;
    [SerializeField] Transform iceAffinity;
    [SerializeField] Transform airAffinity;
    [SerializeField] Transform earthAffinity;
    [SerializeField] Transform holyAffinity;
    [SerializeField] Transform curseAffinity;
    [Header("Indices")]
    [SerializeField] int unknownIndex = 0;
    [SerializeField] int immuneIndex = 1;
    [SerializeField] int absorbIndex = 2;
    [SerializeField] int resistIndex = 3;
    [SerializeField] int reflectIndex = 4;
    [SerializeField] int weakIndex = 5;
    [Space(10)]
    [SerializeField] int itemAffinityIndexPadding = 1;

    //Saving Data
    [SerializeField, HideInInspector]
    private EnemyDataState enemyDataState = new EnemyDataState();
    bool isDataRestored = false;

    //Event
    public Action AllEnemyAffinitiesUnlocked;

    //Cache
    PlayerInput playerInput;
    GameObject currentCam;

    //Indices
    int enemyToAnalyseIndex = 0;

    //Databases
    Dictionary<string, BeingData> raceEncyclopedia = new Dictionary<string, BeingData>();
    Dictionary<string, EnemyPartialData> enemyPartialDataDB = new Dictionary<string, EnemyPartialData>();

    List<CharacterGridUnit> enemyCombatantsFilteredByUniqueType = new List<CharacterGridUnit>();

    //Storage
    Dictionary<string, EnemyPartialData> enemyPartialDataAtBattleStart = new Dictionary<string, EnemyPartialData>();
    Dictionary<int, string> enemyDisplayNames = new Dictionary<int, string>();
    List<CharacterGridUnit> allCombatChars = new List<CharacterGridUnit>();


    private void Awake()
    {
        if (!Instance)
            Instance = this;

        playerInput = ControlsManager.Instance.GetPlayerInput();

        foreach (Transform controlHeader in controlHeaders)
        {
            ControlsManager.Instance.AddControlHeader(controlHeader);
        }
    }

    private void OnEnable()
    {
        FantasyHealth.CharacterUnitKOed += OnUnitKO;
        FantasyCombatManager.Instance.BattleRestarted += OnBattleRestart;

        List<PlayerGridUnit> playersFound = FindObjectsOfType<PlayerGridUnit>().ToList();

        foreach(PlayerGridUnit player in playersFound)
        {
            AddNewEncyclopediaEntry(player.stats.data);
        }

        ControlsManager.Instance.SubscribeToPlayerInput("Analysis", this);
    }



    private void OnUnitKO(CharacterGridUnit unit)
    {
        if(unit is PlayerGridUnit) { return; }

        if (enemyCombatantsFilteredByUniqueType.Contains(unit))
        {
            //Remake List.
            UpdateEnemyCombatants(FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true));
            UpdateAnalyseIndex(0);
        }
    }

    public void ShowAnalysisUI(bool show)
    {
        analysisUI.Fade(show);
        FantasyCombatManager.Instance.ActiveUnitObscureCustomPass(!show);
        //FantasyCombatManager.Instance.ActivateCurrentActiveCam(!show);

        if (show)
        {
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            UpdateAnalyseUI();

            FantasyCombatManager.Instance.ShowHUD(false, false);
            FantasyCombatManager.Instance.ShowActionMenu(false);
            ControlsManager.Instance.SwitchCurrentActionMap("Analysis");
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);

            FantasyCombatManager.Instance.ResetUnitsToShow();
            currentCam.SetActive(false);
            FantasyCombatManager.Instance.ShowHUD(true);
            FantasyCombatManager.Instance.ShowActionMenu(true);
            ControlsManager.Instance.SwitchCurrentActionMap("FantasyCombat");
        }
    }

    public void UpdateEnemyData(BeingData enemyData, AttackData damageData)
    {
        string key = enemyData.Key();

        //Full Data unlocked so nothing to update
        if (raceEncyclopedia.ContainsKey(key)) { return; }

        EnemyPartialData partialData = enemyPartialDataDB[key];


        if(damageData.attackElement != Element.None)
        {
            ElementAffinity elementAffinity = new ElementAffinity();

            elementAffinity.element = damageData.attackElement;
            elementAffinity.affinity = GetAffinity(damageData.attackElement, enemyData);

            if (!partialData.knownElementAffinities.Any((item) => item.element == damageData.attackElement))
                partialData.knownElementAffinities.Add(elementAffinity);
        }

        if (damageData.attackIngredient)
        {
            if(!partialData.otherKnownAffinities.Contains(damageData.attackIngredient.itemName))
                partialData.otherKnownAffinities.Add(damageData.attackIngredient.itemName);
        }

        //Unlock Type when all Affinities unlocked.
        if (partialData.knownElementAffinities.Distinct().Count() == numOfElementalAffinities)
        {
            partialData.type = enemyData.raceType;
            AllEnemyAffinitiesUnlocked?.Invoke();
        }
    }

    public void UpdateEnemyDrops(BeingData enemyData, Item droppedItem)
    {
        //No Need to update if full data unlocked
        if (raceEncyclopedia.ContainsKey(enemyData.Key())) { return; }

        List<string> knownDrops = enemyPartialDataDB[enemyData.Key()].knownDroppedItems;

        if (!knownDrops.Contains(droppedItem.itemName))
        {
            knownDrops.Add(droppedItem.itemName);
        }
    }

    //Called On Combat Begin or when new unit Intialized.
    public void NewEnemyEncountered(CharacterGridUnit enemy)
    {
        string dbKey = enemy.stats.data.Key();

        if (!enemyPartialDataDB.ContainsKey(dbKey))
        {
            enemyPartialDataDB[dbKey] = new EnemyPartialData();
            enemyPartialDataDB[dbKey].race = enemy.stats.data.race;
        }
    }

    //Used by books or others when full profile unlocked
    public void AddNewEncyclopediaEntry(BeingData profile)
    {
        if (raceEncyclopedia.ContainsKey(profile.Key())) { return; }

        AllEnemyAffinitiesUnlocked?.Invoke();
        raceEncyclopedia[profile.Key()] = profile;
    }

    //Data Storage
    public void StoreBattleStartData()
    {
        enemyPartialDataAtBattleStart.Clear();

        foreach (var data in enemyPartialDataDB)
        {
            enemyPartialDataAtBattleStart[data.Key] = new EnemyPartialData(data.Value);
        }
    }

    private void OnBattleRestart()
    {
        enemyPartialDataDB.Clear();

        foreach (var data in enemyPartialDataAtBattleStart)
        {
            enemyPartialDataDB[data.Key] = new EnemyPartialData(data.Value);
        }
    }

    //Analysis
    public void UpdateEnemyCombatants(List<CharacterGridUnit> enemyCombatants)
    {
        enemyCombatantsFilteredByUniqueType.Clear();
        List<string> addedTypes = new List<string>();

        foreach(CharacterGridUnit enemy in enemyCombatants)
        {
            string key = enemy.stats.data.Key();

            if (!addedTypes.Contains(key))
            {
                addedTypes.Add(key);
                enemyCombatantsFilteredByUniqueType.Add(enemy);
            }
        }
    }

    public void UpdateAnalyseIndex(int indexChange)
    {
        int newIndex;

        if (enemyToAnalyseIndex + indexChange >= enemyCombatantsFilteredByUniqueType.Count())
        {
            newIndex = 0;
        }
        else if (enemyToAnalyseIndex + indexChange < 0)
        {
            newIndex = enemyCombatantsFilteredByUniqueType.Count() - 1;
        }
        else
        {
            newIndex = enemyToAnalyseIndex + indexChange;
        }

        enemyToAnalyseIndex = newIndex;

        if(enemyCombatantsFilteredByUniqueType.Count() == 0) { return; }

        CharacterGridUnit enemy = enemyCombatantsFilteredByUniqueType[enemyToAnalyseIndex];

        if (analysisUI.gameObject.activeInHierarchy)
        {
            if (indexChange != 0)
                AudioManager.Instance.PlaySFX(SFXType.TabForward);

            UpdateAnalyseUI();
        }
    }

    public void SetEnemyDisplayNames(bool isCombatBegin) //Called On Combat Begin by Fantasy Combat Manager & When New Unit joins battle.
    {
        if (isCombatBegin)
        {
            UpdateAnalyseIndex(0);
            enemyDisplayNames.Clear();
        }

        allCombatChars = new List<CharacterGridUnit>(FantasyCombatManager.Instance.GetAllCharacterCombatUnits(true));

        foreach (CharacterGridUnit unit in allCombatChars)
        {
            if (unit is PlayerGridUnit) { continue; }

            int listIndex = allCombatChars.IndexOf(unit);

            if (enemyDisplayNames.ContainsKey(listIndex)) { continue; } //Means Unit is already added to battle.

            string displayName;
            BeingData profile = unit.stats.data;

            if (profile.race == Race.Monster && !raceEncyclopedia.ContainsKey(profile.Key()))
            {
                displayName = profile.lockedName;
            }
            else
            {
                displayName = unit.unitName;
            }

            //Check if Duplicates
            List<CharacterGridUnit> unitsWithSameName = allCombatChars.Where((e) => e.unitName == displayName).ToList();

            if (unitsWithSameName.Count > 1)
            {
                int letterIndex = unitsWithSameName.IndexOf(unit);

                // Letter ASCII code is 65(A) to 90(Z)
                displayName = displayName + " " + ((char)(letterIndex + 65));

                enemyDisplayNames[listIndex] = displayName;
            }

        }
    }


    public string GetEnemyDisplayName(GridUnit unit, BeingData profile)
    {
        CharacterGridUnit targetChar = unit as CharacterGridUnit;

        if (!targetChar)
            return unit.unitName;

        string displayName;
        int listIndex = allCombatChars.IndexOf(targetChar);

        if (enemyDisplayNames.ContainsKey(listIndex))
        {
            return enemyDisplayNames[listIndex];
        }

        if (profile.race == Race.Monster && !raceEncyclopedia.ContainsKey(profile.Key()))
        {
            displayName = profile.lockedName;
        }
        else
        {
            displayName = unit.unitName;
        }

        return displayName;
    }
    
    private void UpdateAnalyseUI()
    {
        List<GridUnit> unitsToShow = new List<GridUnit>();

        CharacterGridUnit enemy = enemyCombatantsFilteredByUniqueType[enemyToAnalyseIndex];
        EnemyUnitStats enemyStats = enemy.stats as EnemyUnitStats;

        EnemyPartialData partialData = enemyPartialDataDB[enemyStats.data.Key()];
        bool dataUnlocked = raceEncyclopedia.ContainsKey(enemyStats.data.Key());

        unitsToShow.Add(enemy);

        //Lists
        List<ElementAffinity> elementAffinities = dataUnlocked ? enemyStats.data.elementAffinities : partialData.knownElementAffinities;
        //List<ItemAffinity> itemAffinities = dataUnlocked ? enemyStats.data.itemAffinities : partialData.otherKnownAffinities;
        //List<Item> droppredItems = dataUnlocked ? enemyStats.data.droppedIngridients : partialData.knownDroppedItems;


        //Activate Cam
        FantasyCombatManager.Instance.SetUnitsToShow(unitsToShow);

        if (currentCam)
            currentCam.SetActive(false);

        currentCam = enemy.analysisCam;
        currentCam.SetActive(true);

        //Update Basic Data
        analysisName.text = GetEnemyDisplayName(enemy, enemyStats.data);
        analysisLevel.text = enemyStats.level.ToString();
        analysisRace.text = enemyStats.data.race.ToString();
        analysisType.text = dataUnlocked ? enemyStats.data.raceType.ToString() : GetRaceType(partialData.type);

        ResetAllAffinityUI(dataUnlocked);

        //Update Element Affinities
        foreach (ElementAffinity affinity in elementAffinities)
        {
            switch (affinity.element)
            {
                case Element.Fire:
                    UpdateAffinity(fireAffinity, affinity.affinity, false);
                    break;
                case Element.Ice:
                    UpdateAffinity(iceAffinity, affinity.affinity, false);
                    break;
                case Element.Air:
                    UpdateAffinity(airAffinity, affinity.affinity, false);
                    break;
                case Element.Earth:
                    UpdateAffinity(earthAffinity, affinity.affinity, false);
                    break;
                case Element.Holy:
                    UpdateAffinity(holyAffinity, affinity.affinity, false);
                    break;
                case Element.Curse:
                    UpdateAffinity(curseAffinity, affinity.affinity, false);
                    break;
                case Element.Silver:
                    UpdateAffinity(silverAffinity, affinity.affinity, false);
                    break;
                case Element.Steel:
                    UpdateAffinity(ironAffinity, affinity.affinity, false);
                    break;
                case Element.Gold:
                    UpdateAffinity(goldAffinity, affinity.affinity, false);
                    break;
                case Element.Lightning:
                    Debug.Log("LIGHTNING HAS NOT BEEN IMPLEMENTED");
                    break;
            }
        }
        //Update Item Affinties
        int extraAffinityCount = enemyStats.data.itemAffinities.Count;

        zeroItemAffinityHeader.SetActive(extraAffinityCount == 0);

        if(extraAffinityCount > 0)
        {
            itemAffinityHeader.gameObject.SetActive(true);

            for (int i = 0; i < itemAffinityHeader.childCount; i++)
            {
                if (i < extraAffinityCount)
                {
                    Transform currentChild = itemAffinityHeader.GetChild(i);
                    currentChild.gameObject.SetActive(true);

                    bool isitemKnown = dataUnlocked || partialData.otherKnownAffinities.Contains(enemyStats.data.itemAffinities[i].item.itemName);

                    currentChild.GetChild(0).GetComponent<TextMeshProUGUI>().text = isitemKnown ? enemyStats.data.itemAffinities[i].item.itemName : "???";
                    UpdateAffinity(currentChild, enemyStats.data.itemAffinities[i].affinity, true);

                    currentChild.GetChild(0).gameObject.SetActive(true);
                }
                else
                {
                    itemAffinityHeader.GetChild(i).gameObject.SetActive(false);
                }
            }
        }

        //Update Dropped items
        for (int i = 0; i < ingridientsHeader.childCount; i++)
        {
            if (i < enemyStats.data.droppedIngridients.Count)
            {
                Transform currentChild = ingridientsHeader.GetChild(i);
                currentChild.gameObject.SetActive(true);

                bool isitemKnown = dataUnlocked || partialData.knownDroppedItems.Contains(enemyStats.data.droppedIngridients[i].item.itemName);

                currentChild.GetChild(0).gameObject.SetActive(!isitemKnown);
                currentChild.GetChild(1).gameObject.SetActive(isitemKnown);

                if(isitemKnown)
                    currentChild.GetChild(1).GetComponent<TextMeshProUGUI>().text = enemyStats.data.droppedIngridients[i].item.itemName;
            }
            else
            {
                ingridientsHeader.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        FantasyHealth.CharacterUnitKOed -= OnUnitKO;
        FantasyCombatManager.Instance.BattleRestarted -= OnBattleRestart;
    }

    public bool IsAffinityUnlocked(CharacterGridUnit target, Element attackElement)
    {
        if (target is PlayerGridUnit || raceEncyclopedia.ContainsKey(target.stats.data.Key()))
        {
            return true;
        }

        UnitStats enemyStats = target.stats;
        EnemyPartialData partialData = enemyPartialDataDB[enemyStats.data.Key()];

        //Lists
        List<ElementAffinity> elementAffinities = partialData.knownElementAffinities;

        if (attackElement != Element.None)
        {
            return elementAffinities.Where((affinity) => affinity.element == attackElement).Count() > 0;
        }

        return true;
    }

    //UI Helper Methods
    private string GetRaceType(RaceType raceType)
    {
        if (raceType == RaceType.Unknown)
        {
            return "???";
        }

        return raceType.ToString();
    }

    private void ResetAllAffinityUI(bool dataUnlocked)
    {
        ResetAffinityUI(silverAffinity, dataUnlocked);
        ResetAffinityUI(goldAffinity, dataUnlocked);
        ResetAffinityUI(ironAffinity, dataUnlocked);

        ResetAffinityUI(fireAffinity, dataUnlocked);
        ResetAffinityUI(iceAffinity, dataUnlocked);
        ResetAffinityUI(airAffinity, dataUnlocked);
        ResetAffinityUI(earthAffinity, dataUnlocked);
        ResetAffinityUI(holyAffinity, dataUnlocked);
        ResetAffinityUI(curseAffinity, dataUnlocked);
    }

    private void ResetAffinityUI(Transform affinityHeader, bool defaultToNoAffinity)
    {
        int childIndex = unknownIndex;

        if (defaultToNoAffinity)
        {
            childIndex = -1;
        }

        foreach (Transform child in affinityHeader)
        {
            child.gameObject.SetActive(child.GetSiblingIndex() == childIndex);
        }
    }

    private void UpdateAffinity(Transform affinityHeader, Affinity affinity, bool usePadding)
    {
        int childIndex = -1;

        switch (affinity)
        {
            case Affinity.Absorb:
                childIndex = absorbIndex;
                break;
            case Affinity.Immune:
                childIndex = immuneIndex;
                break;
            case Affinity.Resist:
                childIndex = resistIndex;
                break;
            case Affinity.Reflect:
                childIndex = reflectIndex;
                break;
            case Affinity.Weak:
                childIndex = weakIndex;
                break;
        }

        if (usePadding)
        {
            childIndex = childIndex + itemAffinityIndexPadding;
        }


        foreach(Transform child in affinityHeader)
        {
            child.gameObject.SetActive(child.GetSiblingIndex() == childIndex);
        }
    }

    //Input
    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR")
            {
                UpdateAnalyseIndex(1);
            }
            else if (context.action.name == "CycleL")
            {
                UpdateAnalyseIndex(-1);
            }
        }
    }

    private void OnExit(InputAction.CallbackContext context)
    {
        if (context.action.name != "Exit") { return; }

        if (context.performed)
        {
            ShowAnalysisUI(false);
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            playerInput.onActionTriggered += OnCycle;
            playerInput.onActionTriggered += OnExit;
        }
        else
        {
            playerInput.onActionTriggered -= OnCycle;
            playerInput.onActionTriggered -= OnExit;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

    //Get Affinity
    private Affinity GetAffinity(Element element, BeingData data)
    {
        if(data.elementAffinities.Count == 0) { return Affinity.None; }

        ElementAffinity elementAffinity = data.elementAffinities.FirstOrDefault((item) => item.element == element);

        if (elementAffinity != null)
        {
            return elementAffinity.affinity;
        }

        return Affinity.None;
    }

    private Affinity GetAffinity(Item ingridient, BeingData data)
    {
        if (data.itemAffinities.Count == 0) { return Affinity.None; }

        ItemAffinity itemAffinity = data.itemAffinities.FirstOrDefault((item) => item.item == ingridient);

        if (itemAffinity != null)
        {
            return itemAffinity.affinity;
        }

        return Affinity.None;
    }

    // SAving

    [System.Serializable]
    public class EnemyDataState
    {
        public Dictionary<string, EnemyPartialData> enemyPartialDataDB = new Dictionary<string, EnemyPartialData>();
        public List<string> raceEncyclopediaKeys = new List<string>();

    }

    public object CaptureState()
    {
        enemyDataState.enemyPartialDataDB = enemyPartialDataDB;

        enemyDataState.raceEncyclopediaKeys.Clear();

        foreach (var item in raceEncyclopedia)
        {
            enemyDataState.raceEncyclopediaKeys.Add(item.Key);
        }

        return SerializationUtility.SerializeValue(enemyDataState, DataFormat.Binary);
    }


    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null) { return; }

        byte[] bytes = state as byte[];
        enemyDataState = SerializationUtility.DeserializeValue<EnemyDataState>(bytes, DataFormat.Binary);

        //Restore
        enemyPartialDataDB = enemyDataState.enemyPartialDataDB;

        raceEncyclopedia.Clear();

        foreach(string key in enemyDataState.raceEncyclopediaKeys)
        {
            raceEncyclopedia[key] = TheCache.Instance.GetBeingDataByKey(key);
        }
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
