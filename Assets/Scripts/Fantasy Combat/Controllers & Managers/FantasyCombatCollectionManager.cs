using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System;
using AnotherRealm;

public class FantasyCombatCollectionManager : MonoBehaviour, IControls, ISaveable
{
    [Header("Components")]
    [SerializeField] PlayerUsePotion usePotion;
    [SerializeField] PlayerUsePotion passPotion;
    [Space(10)]
    [SerializeField] Transform spawnedOrbHeader;
    [Header("Effects")]
    [SerializeField] BasicPotionEffect basicPotionEffect;
    [SerializeField] BlessingEffects blessingEffect;
    [Header("Canvas")]
    [SerializeField] GameObject potionCanvas;
    [SerializeField] GameObject orbCanvas;
    [Space(5)]
    [SerializeField] GameObject skillCanvas;
    [SerializeField] GameObject tacticsCanvas;
    [Space(5)]
    [SerializeField] GameObject blessingCanvas;
    [Header("Potion UI")]
    [SerializeField] ScrollRect potionsScrollRect;
    [SerializeField] TextMeshProUGUI potionsDescription;
    [Header("Orb UI")]
    [SerializeField] ScrollRect orbScrollRect;
    [SerializeField] TextMeshProUGUI orbQuickData;
    [SerializeField] TextMeshProUGUI orbDescription;
    [SerializeField] Transform orbAOEDiagramHeader;
    [Header("BlessingUI")]
    [SerializeField] ScrollRect blessingScrollRect;
    [SerializeField] TextMeshProUGUI blessingDescription;
    [Header("Skill UI")]
    [SerializeField] ScrollRect skillScrollRect;
    [SerializeField] TextMeshProUGUI skillQuickData;
    [SerializeField] TextMeshProUGUI skillDescription;
    [SerializeField] Transform skillAOEDiagramHeader;
    [Header("Tactics")]
    [SerializeField] WeaponSwitchUI weaponSwitchUI;
    [Header("Prefabs")]
    [SerializeField] GameObject potionPrefab;
    [Space(5)]
    [SerializeField] GameObject orbPrefab;
    [SerializeField] GameObject chargingOrbPrefab;
    [Space(5)]
    [SerializeField] GameObject skillPrefab;
    [SerializeField] GameObject unaffordableSkillPrefab;
    [Space(5)]
    [SerializeField] GameObject blessingPrefab;
    [SerializeField] GameObject chargingBlessingPrefab;
    [Header("UI Pools")]
    [SerializeField] Transform affordableSkillPool;
    [SerializeField] Transform unaffordableSkillPool;
    [Space(5)]
    [SerializeField] Transform orbPool;
    [SerializeField] Transform chargingOrbPool;
    [Space(5)]
    [SerializeField] Transform blessingPool;
    [SerializeField] Transform chargingBlessingPool;
    [Header("Indices")]
    [SerializeField] int itemIconIndex = 1;
    [SerializeField] int itemHighlightedIndex = 0;
    [SerializeField] int itemNameIndex = 2;
    [SerializeField] int itemStockIndex = 3;
    [Space(10)]
    [SerializeField] int skillHighlightedIndex = 0;
    [SerializeField] int skillIconIndex = 1;
    [SerializeField] int skillNameIndex = 2;
    [SerializeField] int skillCostIndex = 3;
    [Space(10)]
    [SerializeField] int blessingHighlightedIndex = 0;
    [SerializeField] int blessingIconIndex = 1;
    [SerializeField] int blessingNameIndex = 2;
    [SerializeField] int blessingTurnIndex = 3;
    [Header("Text")]
    [SerializeField] string stockAppendText = "<size=80%>x</size> ";

    //Saving Data
    [SerializeField, HideInInspector]
    private List<ChargeableData> chargeableState = new List<ChargeableData>();
    bool isDataRestored = false;

    //Variables
    PlayerGridUnit currentPlayer;
    bool usingTacticsMenu = false;
    bool usingTacticInnerMenu = false;

    //Storage
    List<Potion> currentPotionList = new List<Potion>();
    List<Orb> currentOrbList = new List<Orb>();
    List<PlayerBaseSkill> currentSkillList = new List<PlayerBaseSkill>();
    List<Blessing> currentBlessingList = new List<Blessing>();

    Dictionary<PlayerGridUnit, List<ItemChargingData>> chargeablesOnCooldownCurrentHealth = new Dictionary<PlayerGridUnit, List<ItemChargingData>>();
    Dictionary<PlayerGridUnit, List<ItemChargingData>> chargeablesOnCooldownCurrentHealthAtBattleStart = new Dictionary<PlayerGridUnit, List<ItemChargingData>>();

    Dictionary<Orb, PlayerBaseSkill> spawnedOrbDict = new Dictionary<Orb, PlayerBaseSkill>();
    Dictionary<GameObject, Transform> poolDict = new Dictionary<GameObject, Transform>();

    //GOs
    GameObject activeCollectionCam;
    GameObject activeCollectionUI;

    //Indices
    int currentItemIndex = 0;
    int currentSkillIndex = 0;
    int currentBlessingIndex = 0;

    //Cache 
    PlayerInput playerInput;

    //Event
    public static Action<PlayerGridUnit> MenuSkillCancelled;
    public static Action<PlayerGridUnit> BlessingUsed;
    public static Action<PlayerGridUnit, float> ItemCharged;

    public class ItemChargingData
    {
        public Item chargingItem;
        public int currentHealth;

        public ItemChargingData(Item item, int currentHealth)
        {
            chargingItem = item;
            this.currentHealth = currentHealth;
        }

        public ItemChargingData(ItemChargingData dataToClone)
        {
            chargingItem = dataToClone.chargingItem;
            currentHealth = dataToClone.currentHealth;
        }
    }

    private void Awake()
    {
        playerInput = ControlsManager.Instance.GetPlayerInput();
        CleanAllCollections();
    }

    private void OnEnable()
    {
        ControlsManager.Instance.SubscribeToPlayerInput("CombatMenu", this);
        ControlsManager.Instance.SubscribeToPlayerInput("TacticsMenu", this);

        FantasyCombatManager.Instance.BattleRestarted += OnBattleRestart;
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;
        FantasyCombatManager.Instance.OnNewTurn += OnAnyUnitTurnStart;
        

        MenuSkillCancelled += OpenSkillMenu;
    }

    private void Start()
    {
        InventoryManager.Instance.ItemDiscarded += OnItemDiscarded;
        InventoryManager.Instance.ItemTransfered += OnItemTransferred;
    }

    //Item Fixing

    private void OnItemTransferred(Item item, PlayerGridUnit fromInventory, PlayerGridUnit toInventory)
    {
        if (!(item is Blessing) && !(item is Orb)) { return; }

        if (chargeablesOnCooldownCurrentHealth.ContainsKey(fromInventory))
        {
            List<ItemChargingData> chargeDatas = chargeablesOnCooldownCurrentHealth[fromInventory];
            ItemChargingData chargeData = chargeDatas.FirstOrDefault((data) => data.chargingItem == item);

            if (chargeData == null) { return; }

            chargeDatas.Remove(chargeData);

            if (!chargeablesOnCooldownCurrentHealth.ContainsKey(toInventory))
                chargeablesOnCooldownCurrentHealth[toInventory] = new List<ItemChargingData>();

            //If New Owner doesn't have item charging, then add.
            if(chargeablesOnCooldownCurrentHealth[toInventory].FirstOrDefault((data) => data.chargingItem == item) == null)
                chargeablesOnCooldownCurrentHealth[toInventory].Add(new ItemChargingData(chargeData));

            //Remove Unit From Dict.
            if (chargeDatas.Count == 0)
            {
                chargeablesOnCooldownCurrentHealth.Remove(fromInventory);
            }
        }
    }

    private void OnItemDiscarded(Item item, PlayerGridUnit inventoryOwner)
    {
        if (!(item is Blessing) || !(item is Orb)) { return; }

        if (chargeablesOnCooldownCurrentHealth.ContainsKey(inventoryOwner))
        {
            List<ItemChargingData> chargeDatas = chargeablesOnCooldownCurrentHealth[inventoryOwner];
            ItemChargingData chargeData = chargeDatas.FirstOrDefault((data) => data.chargingItem == item);

            if(chargeData != null)
                chargeDatas.Remove(chargeData);

            //Remove Unit From Dict.
            if (chargeDatas.Count == 0)
            {
                chargeablesOnCooldownCurrentHealth.Remove(inventoryOwner);
            }
        }
    }


    //Battle Prep
    public void StoreBattleStartData()
    {
        chargeablesOnCooldownCurrentHealthAtBattleStart.Clear();

        foreach(var pair in chargeablesOnCooldownCurrentHealth)
        {
            chargeablesOnCooldownCurrentHealthAtBattleStart[pair.Key] = new List<ItemChargingData>();

            foreach (ItemChargingData data in pair.Value)
            {
                chargeablesOnCooldownCurrentHealthAtBattleStart[pair.Key].Add(new ItemChargingData(data));
            }
        }
    }

    private void OnBattleRestart()
    {
        chargeablesOnCooldownCurrentHealth.Clear();

        foreach (var pair in chargeablesOnCooldownCurrentHealthAtBattleStart)
        {
            chargeablesOnCooldownCurrentHealth[pair.Key] = new List<ItemChargingData>();

            foreach (ItemChargingData data in pair.Value)
            {
                chargeablesOnCooldownCurrentHealth[pair.Key].Add(new ItemChargingData(data));
            }
        }
    }


    //Menus
    public void OpenItemMenu(PlayerGridUnit currentPlayer, bool openPotionsTab)
    {
        if (this.currentPlayer != currentPlayer)
        {
            currentItemIndex = 0;
        }

        this.currentPlayer = currentPlayer;

        if (openPotionsTab)
        {
            orbCanvas.SetActive(false);
            CreatePotionUI(currentPlayer);
            UpdateCollection(currentPlayer.collectionThinkCam, potionCanvas);
        }
        else
        {
            potionCanvas.SetActive(false);
            CreateOrbUI(currentPlayer);
            UpdateCollection(currentPlayer.collectionThinkCam, orbCanvas);
        }

        //Trigger Tutorial
        if(!StoryManager.Instance.PlayTutorial(5))
            ControlsManager.Instance.SwitchCurrentActionMap("CombatMenu");
    }

    public void OpenSkillMenu(PlayerGridUnit currentPlayer)
    {
        if (this.currentPlayer != currentPlayer)
        {
            currentSkillIndex = 0;
        }
        
        this.currentPlayer = currentPlayer;

        CreateSkillUI(currentPlayer);
        UpdateCollection(currentPlayer.collectionThinkCam, skillCanvas);
        ControlsManager.Instance.SwitchCurrentActionMap("CombatMenu");
    }

    public void OpenBlessingMenu()
    {
        currentBlessingIndex = 0;

        usingTacticsMenu = false;
        usingTacticInnerMenu = true;

        ActivateCollection(false);

        CreateBlessUI();
        UpdateCollection(currentPlayer.tacticsMenuCam, blessingCanvas);

        //Trigger Tutorial
        if(!StoryManager.Instance.PlayTutorial(6))
            ControlsManager.Instance.SwitchCurrentActionMap("CombatMenu");
    }



    private void UpdateActiveItemTab()
    {
        GameObject newCollection = potionCanvas.activeInHierarchy ? orbCanvas : potionCanvas;
        currentItemIndex = 0;

        //Play TAB SFX
        AudioManager.Instance.PlaySFX(SFXType.TabForward);

        if (newCollection == orbCanvas)
        {
            CreateOrbUI(currentPlayer);
        }
        else if (newCollection == potionCanvas)
        {
            CreatePotionUI(currentPlayer);
        }

        ActivateCollection(false);
        UpdateCollection(currentPlayer.collectionThinkCam, newCollection);

        UpdateActiveUI(0);
    }


    public void OpenTacticsMenu(PlayerGridUnit currentPlayer)
    {
        usingTacticsMenu = true;
        this.currentPlayer = currentPlayer;

        UpdateCollection(currentPlayer.tacticsMenuCam, tacticsCanvas);
        ControlsManager.Instance.SwitchCurrentActionMap("TacticsMenu");
    }



    public void OpenWeaponSwitchUI()
    {
        ActivateCollection(false);
        UpdateCollection(currentPlayer.weaponSwitchCam, weaponSwitchUI.gameObject);
        weaponSwitchUI.ActivateUI(currentPlayer, this);
    }

    public void CleanUI()
    {
        ReturnAllUIChildrenToPool();
    }

    public void OnExitWeaponSwitch(bool newWeaponEquipped)
    {
        ActivateCollection(false);

        if (newWeaponEquipped)
        {
            FantasyCombatManager.Instance.ShowHUD(true);
            usingTacticsMenu = false;

            FantasyCombatManager.Instance.TacticActivated();
        }
        else
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.TabBack);

            OpenTacticsMenu(currentPlayer);
        }
    }

    private void UpdateCollection(GameObject cam, GameObject ui)
    {
        FantasyCombatManager.Instance.ShowActionMenu(false);
        FantasyCombatManager.Instance.ShowHUD(false, false);

        activeCollectionCam = cam;
        activeCollectionUI = ui;

        ActivateCollection(true);
    }

    private void ActivateCollection(bool activate)
    {
        activeCollectionCam.SetActive(activate);
        activeCollectionUI.SetActive(activate);
    }

    private void ExitCollection(bool showActionMenu = true)
    {
        ActivateCollection(false);

        if (usingTacticInnerMenu)
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.TabBack);

            usingTacticInnerMenu = false;
            OpenTacticsMenu(currentPlayer);
            return;
        }

        if(showActionMenu) //Means Was Called Via Player Input
            AudioManager.Instance.PlaySFX(SFXType.TabBack);

        currentPlayer.SetFollowCamInheritPosition(false); //Disable inherit position so Cam slings back to behind player.

        FantasyCombatManager.Instance.ShowActionMenu(showActionMenu);
        FantasyCombatManager.Instance.ShowHUD(true);

        usingTacticsMenu = false;

        if (StoryManager.Instance.isTutorialPlaying)
        {
            StoryManager.Instance.TutorialComplete += WaitForTutorial;
            return;
        }

        ControlsManager.Instance.SwitchCurrentActionMap("FantasyCombat");
    }

    private void WaitForTutorial()
    {
        StoryManager.Instance.TutorialComplete -= WaitForTutorial;
        ControlsManager.Instance.SwitchCurrentActionMap("FantasyCombat");
    }

    //Skills
    private void UseSkill()
    {
        if (currentSkillList.Count == 0 || !skillCanvas.activeInHierarchy) { return; }

        if (FantasyCombatManager.Instance.SkillSelectedFromList(currentSkillList[currentSkillIndex]))
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

            ExitCollection(false);
        }
        else
        {
            //Play Selection Skill Denied Feedback.
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
        }
    }

    //Potions
    private void UsePotion(PlayerUsePotion potionSkill)
    {
        if (currentPotionList.Count == 0 || !potionCanvas.activeInHierarchy) { return; }

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

        potionSkill.Setup(currentPlayer, GetPotionEffect(), this);

        if (FantasyCombatManager.Instance.SkillSelectedFromList(potionSkill))
        {
            ExitCollection(false);
        }
    }

    private BasicPotionEffect GetPotionEffect()
    {
        Potion selectedPotion = currentPotionList[currentItemIndex];

        if (selectedPotion.isSpecialPotion)
        {
            BasicPotionEffect specialPotionEffect = selectedPotion.specialPotionPrefab.GetComponent<BasicPotionEffect>();
            specialPotionEffect.potionData = selectedPotion;

            return specialPotionEffect;
        }
        else
        {
            //Basic Potion
            basicPotionEffect.potionData = selectedPotion;
            return basicPotionEffect;
        }
    }

    //Orbs
    private void UseOrb()
    {
        if (currentOrbList.Count == 0 || !orbCanvas.activeInHierarchy) { return; }

        Orb selectedOrb = currentOrbList[currentItemIndex];
        if (IsCharging(selectedOrb, currentPlayer)) //Check if charging
        {
            //Play Selection denied Feedback
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return;
        }

        PlayerBaseSkill spawnedOrb = SetSpawnedOrb(selectedOrb);

        spawnedOrb.GetComponent<IOrb>().Setup(spawnedOrb, currentPlayer, this);

        if (FantasyCombatManager.Instance.SkillSelectedFromList(spawnedOrb))
        {
            //Play SFX
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
            ExitCollection(false);
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
        }
    }

    private PlayerBaseSkill SetSpawnedOrb(Orb orb)
    {
        if (spawnedOrbDict.ContainsKey(orb))
        {
            return spawnedOrbDict[orb];
        }

        //Create New Orb Prefab & Add to Dict for optimization.
        GameObject newOrb = Instantiate(orb.orbSkillPrefab, spawnedOrbHeader);
        PlayerBaseSkill orbSkill = newOrb.GetComponent<PlayerBaseSkill>();

        spawnedOrbDict[orb] = orbSkill;
        return orbSkill;
    }

    //Blessings
    public void ActivateBlessing()
    {
        if (currentBlessingList.Count == 0 || !blessingCanvas.activeInHierarchy) { return; }

        Blessing currentBlessing = currentBlessingList[currentBlessingIndex];

        if (IsCharging(currentBlessing, currentPlayer)) //Check if charging
        {
            //Play Selection denied Feedback
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return;
        }

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);

        ActivateCollection(false);
        FantasyCombatManager.Instance.ShowHUD(true);

        usingTacticsMenu = false;
        usingTacticInnerMenu = false;

        if (blessingEffect.activeBlessing)
            blessingEffect.ResetEffect();

        blessingEffect.activeBlessing = currentBlessing;
        blessingEffect.ActivateEffect(currentPlayer);
        //FantasyCombatManager.Instance.TacticActivated(); NOW CALLED VIA ACTIVATE EFFECT.
    }




    //EVENTS
    private void OnAnyUnitTurnStart(CharacterGridUnit actingUnit, int turnNumber)
    {
        PlayerGridUnit player = actingUnit as PlayerGridUnit;
        List<string> fullyChargedItems = new List<string>();

        if (player && chargeablesOnCooldownCurrentHealth.ContainsKey(player))
        {
            List<ItemChargingData> chargeData = chargeablesOnCooldownCurrentHealth[player];

            //Heal All Items On Cooldown
            for(int i = chargeData.Count -1; i >= 0; i--)
            {
                ItemChargingData currentChargeData = chargeData.ElementAt(i);

                int healAmount = TheCalculator.Instance.CalculateRawHeal(player, out bool isCrit);

                currentChargeData.currentHealth = currentChargeData.currentHealth + healAmount;

                int itemMaxHealth = 100;

                Blessing blessing = currentChargeData.chargingItem as Blessing;
                Orb orb = currentChargeData.chargingItem as Orb;

                if (blessing)
                {
                    itemMaxHealth = blessing.GetMaxHealth();
                }
                else if (orb)
                {
                    itemMaxHealth = orb.GetMaxHealth();
                }

                float normalizedHealth = (float)currentChargeData.currentHealth / itemMaxHealth;
                ItemCharged?.Invoke(player, normalizedHealth);

                if(currentChargeData.currentHealth >= itemMaxHealth) // Means Full Charged
                {
                    fullyChargedItems.Add(currentChargeData.chargingItem.itemName);
                    chargeData.Remove(currentChargeData);
                }
            }

            //Remove Unit From Dict.
            if(chargeablesOnCooldownCurrentHealth[player].Count == 0)
            {
                chargeablesOnCooldownCurrentHealth.Remove(player);
            }

            if (fullyChargedItems.Count > 0)
                HUDManager.Instance.UpdateChargeNotification(fullyChargedItems);
        }
    }

    //Charging

    public void ChargeAllChargeables(CharacterGridUnit owner)
    {
        PlayerGridUnit player = owner as PlayerGridUnit;

        if (player && chargeablesOnCooldownCurrentHealth.ContainsKey(player))
        {
            foreach (var item in chargeablesOnCooldownCurrentHealth[player])
            {
                ItemCharged?.Invoke(player, 1);
            }

            chargeablesOnCooldownCurrentHealth[player].Clear();
            chargeablesOnCooldownCurrentHealth.Remove(player);
        }   
    }



    //Begin Cooldowns
    public void BeginBlessingCooldown(Blessing blessing, PlayerGridUnit blesser)
    {
        if (!chargeablesOnCooldownCurrentHealth.ContainsKey(blesser))
        {
            chargeablesOnCooldownCurrentHealth[blesser] = new List<ItemChargingData>();
        }

        chargeablesOnCooldownCurrentHealth[blesser].Add(new ItemChargingData(blessing, 0));
    }

    public void BeginOrbCooldown(Orb orb, PlayerGridUnit owner)
    {
        if (!chargeablesOnCooldownCurrentHealth.ContainsKey(owner))
        {
            chargeablesOnCooldownCurrentHealth[owner] = new List<ItemChargingData>();
        }

        chargeablesOnCooldownCurrentHealth[owner].Add(new ItemChargingData(orb, 0));
    }


    private void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        //Reset Blessings
        if (blessingEffect.activeBlessing)
        {
            blessingEffect.ResetEffect();
            HUDManager.Instance.UpdateBlessing(null, 0);
        }

        CleanUI();
    }


    private void OnDisable()
    {
        FantasyCombatManager.Instance.BattleRestarted -= OnBattleRestart;
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
        FantasyCombatManager.Instance.OnNewTurn -= OnAnyUnitTurnStart;

        InventoryManager.Instance.ItemDiscarded -= OnItemDiscarded;
        InventoryManager.Instance.ItemTransfered -= OnItemTransferred;

        MenuSkillCancelled -= OpenSkillMenu;
    }



    //Getters

    private float GetChargeableNormalizedHealth(Item item, PlayerGridUnit player)
    {
        float currentHealth = chargeablesOnCooldownCurrentHealth[player].First((data) => data.chargingItem == item).currentHealth;

        Blessing blessing = item as Blessing;
        Orb orb = item as Orb;

        float maxHealth = 100;

        if (blessing)
        {
            maxHealth = blessing.GetMaxHealth();
        }
        else if (orb)
        {
            maxHealth = orb.GetMaxHealth();
        }

        return currentHealth / maxHealth;
    }

    private bool IsCharging(Item item, PlayerGridUnit itemOwner)
    {
        return chargeablesOnCooldownCurrentHealth.ContainsKey(itemOwner) && chargeablesOnCooldownCurrentHealth[itemOwner].Any((data) => data.chargingItem == item);
    }

    private int GetOrbMaxCooldown(Orb orb, PlayerGridUnit player)
    {
        int maxHealth = orb.GetMaxHealth();
        return Mathf.CeilToInt((float)maxHealth / TheCalculator.Instance.CalculateHealAmount(player.stats.Wisdom));
    }

    //UI Update
    private void UpdateActiveUI(int indexChange)
    {
        if(potionCanvas.activeInHierarchy)
        {
            UpdatePotionUI(indexChange);
        }
        else if (skillCanvas.activeInHierarchy)
        {
            UpdateSkillUI(indexChange);
        }
        else if (blessingCanvas.activeInHierarchy)
        {
            UpdateBlessingUI(indexChange);
        }
        else if (orbCanvas.activeInHierarchy)
        {
            UpdateOrbUI(indexChange);
        }
    }



    //POTIONS UI
    private void CreatePotionUI(PlayerGridUnit player)
    {
        currentPotionList = InventoryManager.Instance.GetItemInventory<Potion>(currentPlayer);
        DeactiveHeaderChildren(potionsScrollRect.content);

        for (int index = 0; index < currentPotionList.Count; index++)
        {
            Potion potion = currentPotionList[index];

            GameObject potionGO;

            if (index < potionsScrollRect.content.childCount)
            {
                potionGO = potionsScrollRect.content.GetChild(index).gameObject;
            }
            else
            {
                potionGO = Instantiate(potionPrefab, potionsScrollRect.content);
            }

            //SetIcon
            foreach (Transform icon in potionGO.transform.GetChild(itemIconIndex))
            {
                icon.gameObject.SetActive(icon.GetSiblingIndex() == CombatFunctions.GetPotionIconIndex(potion.potionIcon));
            }
            //Update Name
            potionGO.transform.GetChild(itemNameIndex).GetComponent<TextMeshProUGUI>().text = potion.itemName;
            //Update Stock
            potionGO.transform.GetChild(itemStockIndex).GetComponent<TextMeshProUGUI>().text = stockAppendText + InventoryManager.Instance.GetItemCount(potion, player).ToString();

            potionGO.SetActive(true);
        }

        UpdatePotionUI(0);
    }

    private void UpdatePotionUI(int indexChange)
    {
        if (currentPotionList.Count == 0) 
        {
            potionsDescription.text = "";
            return; 
        }

        UpdateIndex(indexChange, currentItemIndex, out currentItemIndex, currentPotionList.Count);

        //Play SFX
        if (indexChange != 0 && currentPotionList.Count > 1)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        foreach(Transform child in potionsScrollRect.content)
        {
            child.GetChild(itemHighlightedIndex).gameObject.SetActive(child.GetSiblingIndex() == currentItemIndex);
        }

        potionsDescription.text = currentPotionList[currentItemIndex].description;

        //Update Scrollview
        CombatFunctions.VerticalScrollToHighlighted(potionsScrollRect.content.GetChild(currentItemIndex).transform as RectTransform, potionsScrollRect, currentItemIndex, currentPotionList.Count);
    }

    //ORB UI

    private void CreateOrbUI(PlayerGridUnit player)
    {
        if (orbScrollRect.content.childCount > 0) { return; }

        currentOrbList = InventoryManager.Instance.GetItemInventory<Orb>(player);

        foreach (Orb orb in currentOrbList)
        {
            PlayerBaseSkill orbSkillData = orb.orbSkillPrefab.GetComponent<PlayerBaseSkill>();
            bool isCharging = IsCharging(orb, player);

            GameObject orbGO = FetchFromPool(isCharging ? chargingOrbPool : orbPool, orbScrollRect, isCharging ? chargingOrbPrefab : orbPrefab);

            //SetIcon
            foreach (Transform icon in orbGO.transform.GetChild(itemIconIndex))
            {
                icon.gameObject.SetActive(icon.GetSiblingIndex() == orbSkillData.GetSkillIndex());
            }

            //Update Name
            orbGO.transform.GetChild(itemNameIndex).GetComponent<TextMeshProUGUI>().text = orb.itemName;

            //Update Cooldown
            if (isCharging)
            {
                Image bar = orbGO.transform.GetChild(blessingTurnIndex).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>();
                bar.fillAmount = GetChargeableNormalizedHealth(orb, player);
            }
            else
            {
                orbGO.transform.GetChild(blessingTurnIndex).GetChild(0).GetComponent<TextMeshProUGUI>().text = GetOrbMaxCooldown(orb, player).ToString();
            }
        }

        UpdateOrbUI(0);
    }



    private void UpdateOrbUI(int indexChange)
    {
        if (currentOrbList.Count == 0) 
        {
            if(orbAOEDiagramHeader.childCount > 0)
                Destroy(orbAOEDiagramHeader.GetChild(0).gameObject);

            orbQuickData.text = "";
            orbDescription.text = "";
            return; 
        }

        UpdateIndex(indexChange, currentItemIndex, out currentItemIndex, currentOrbList.Count);

        //Play SFX
        if (indexChange != 0 && currentOrbList.Count > 1)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        Orb currentOrb = currentOrbList[currentItemIndex];
        PlayerBaseSkill orbSkillData = currentOrb.orbSkillPrefab.GetComponent<PlayerBaseSkill>();

        foreach (Transform child in orbScrollRect.content)
        {
            child.GetChild(itemHighlightedIndex).gameObject.SetActive(child.GetSiblingIndex() == currentItemIndex);
        }

        //Update Information
        orbQuickData.text = orbSkillData.quickData;
        orbDescription.text = orbSkillData.description;

        //Update AOE Diagram
        foreach (Transform child in orbAOEDiagramHeader)
        {
            Destroy(child.gameObject);
        }

        Instantiate(orbSkillData.aoeDiagram, orbAOEDiagramHeader);

        //Update Scrollview
        CombatFunctions.VerticalScrollToHighlighted(orbScrollRect.content.GetChild(currentItemIndex).transform as RectTransform, orbScrollRect, currentItemIndex, currentOrbList.Count);
    }


    //BLESSING UI
    private void CreateBlessUI()
    {
        if (blessingScrollRect.content.childCount > 0) { return; }

        currentBlessingList = InventoryManager.Instance.GetItemInventory<Blessing>(currentPlayer);

        foreach (Blessing blessing in currentBlessingList)
        {
            bool isCharging = IsCharging(blessing, currentPlayer);

            GameObject blessingGO = FetchFromPool(isCharging ? chargingBlessingPool : blessingPool, blessingScrollRect, isCharging ? chargingBlessingPrefab : blessingPrefab);

            //Set Icon Color
            blessingGO.transform.GetChild(blessingIconIndex).GetComponent<Image>().color = blessing.iconColor;

            //Update Name
            blessingGO.transform.GetChild(blessingNameIndex).GetComponent<TextMeshProUGUI>().text = blessing.itemName;

            //Update Turns
            if (isCharging)
            {
                Image bar = blessingGO.transform.GetChild(blessingTurnIndex).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>();
                bar.fillAmount = GetChargeableNormalizedHealth(blessing, currentPlayer);
            }
            else
            {
                blessingGO.transform.GetChild(blessingTurnIndex).GetChild(0).GetComponent<TextMeshProUGUI>().text = blessing.duration.ToString();
            }
            
        }

        UpdateBlessingUI(0);
    }

    private void UpdateBlessingUI(int indexChange)
    {
        if (currentBlessingList.Count == 0) 
        {
            blessingDescription.text = "";
            return; 
        }

        UpdateIndex(indexChange, currentBlessingIndex, out currentBlessingIndex, currentBlessingList.Count);

        //Play SFX
        if (indexChange != 0 && currentBlessingList.Count > 1)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        foreach (Transform child in blessingScrollRect.content)
        {
            child.GetChild(blessingHighlightedIndex).gameObject.SetActive(child.GetSiblingIndex() == currentBlessingIndex);
        }

        blessingDescription.text = currentBlessingList[currentBlessingIndex].description;

        //Update Scrollview
        CombatFunctions.VerticalScrollToHighlighted(blessingScrollRect.content.GetChild(currentBlessingIndex).transform as RectTransform, blessingScrollRect, currentBlessingIndex, currentBlessingList.Count);
    }

    //SKILL UI
    private void CreateSkillUI(PlayerGridUnit player)
    {
        if (skillScrollRect.content.childCount > 0) { return; }

        currentSkillList = CombatSkillManager.Instance.GetPlayerSpawnedSkills(player);
        currentSkillList = currentSkillList.Where((skill) => skill.GetSkillData().category == SkillCategory.Skill).ToList();

        foreach (PlayerBaseSkill skill in currentSkillList)
        {
            bool canAfford = skill.CanAffordSkill();

            GameObject skillGO = FetchFromPool(canAfford ? affordableSkillPool : unaffordableSkillPool, skillScrollRect, canAfford ? skillPrefab : unaffordableSkillPrefab);

            //SetIcon
            foreach (Transform icon in skillGO.transform.GetChild(skillIconIndex))
            {
                icon.gameObject.SetActive(icon.GetSiblingIndex() == skill.GetSkillIndex());
            }

            //Update Name
            skillGO.transform.GetChild(skillNameIndex).GetComponent<TextMeshProUGUI>().text = skill.skillName;

            int costTypeIndex = CombatFunctions.GetSkillCostTypeIndex(skill.costType);

            //Update Cost
            foreach (Transform costType in skillGO.transform.GetChild(skillCostIndex))
            {
                bool isCostType = costType.GetSiblingIndex() == costTypeIndex;
                costType.gameObject.SetActive(isCostType);

                if (isCostType)
                {
                    costType.GetComponent<TextMeshProUGUI>().text = skill.GetCost().ToString();
                }
            }
        }

        UpdateSkillUI(0);
    }

    private void UpdateSkillUI(int indexChange)
    {
        if (currentSkillList.Count == 0) { return; }

        UpdateIndex(indexChange, currentSkillIndex, out currentSkillIndex, currentSkillList.Count);

        //Play SFX
        if (indexChange != 0 && currentSkillList.Count > 1)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        foreach (Transform child in skillScrollRect.content)
        {
            child.GetChild(skillHighlightedIndex).gameObject.SetActive(child.GetSiblingIndex() == currentSkillIndex);
        }

        skillQuickData.text = currentSkillList[currentSkillIndex].quickData;
        skillDescription.text = currentSkillList[currentSkillIndex].description;

        //Update AOE Diagram
        Destroy(skillAOEDiagramHeader.GetChild(0).gameObject);
        Instantiate(currentSkillList[currentSkillIndex].aoeDiagram, skillAOEDiagramHeader);

        //Update Scrollview
        CombatFunctions.VerticalScrollToHighlighted(skillScrollRect.content.GetChild(currentSkillIndex).transform as RectTransform, skillScrollRect, currentSkillIndex, currentSkillList.Count);
    }




    private void UpdateIndex(int indexChange, int currentIndex, out int IndexToChange, int listCount)
    {
        int newIndex;

        if (currentIndex + indexChange >= listCount)
        {
            newIndex = 0;
        }
        else if (currentIndex + indexChange < 0)
        {
            newIndex = listCount - 1;
        }
        else
        {
            newIndex = currentIndex + indexChange;
        }

        IndexToChange  = newIndex;
    }


    //UI Helper Methods

    private GameObject FetchFromPool(Transform poolHeader, ScrollRect listScrollRect, GameObject prefab)
    {
        GameObject poolUI;

        if (poolHeader.childCount == 0)
        {
            //Spawn New One
            poolUI = Instantiate(prefab, poolHeader);
        }
        else
        {
            poolUI = poolHeader.GetChild(0).gameObject;
        }

        if (!poolDict.ContainsKey(poolUI))
        {
            poolDict[poolUI] = poolHeader;
        }

        poolUI.transform.parent = listScrollRect.content;
        poolUI.transform.localScale = Vector3.one;
        return poolUI;
    }

    private void ReturnAllUIChildrenToPool()
    {
        ReturnChildrenToPool(potionsScrollRect.content);
        ReturnChildrenToPool(skillScrollRect.content);
        ReturnChildrenToPool(blessingScrollRect.content);
        ReturnChildrenToPool(orbScrollRect.content);
    }

    private void CleanAllCollections()
    {
        ClearHeader(potionsScrollRect.content);
        ClearHeader(skillScrollRect.content);
        ClearHeader(blessingScrollRect.content);
        ClearHeader(orbScrollRect.content);
    }


    private void ClearHeader(Transform header)
    {
        foreach (Transform child in header)
        {
            Destroy(child.gameObject);
        }
    }

    private void ReturnChildrenToPool(Transform scrollRectContent)
    {
        for (int index = scrollRectContent.childCount - 1; index >= 0; index--)
        {
            GameObject GO = scrollRectContent.GetChild(index).gameObject;
            if (poolDict.ContainsKey(GO))
            {
                GO.transform.parent = poolDict[GO];
            }
        }
    }

    private void DeactiveHeaderChildren(Transform header)
    {
        foreach (Transform child in header)
        {
            //Deactive Child
            child.gameObject.SetActive(false);
        }
    }


    //Inputs
    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed && (potionCanvas.activeInHierarchy || orbCanvas.activeInHierarchy))
        {
            if (context.action.name == "CycleR" || context.action.name == "CycleL")
            {
                UpdateActiveItemTab();
            }
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU")
            {
                UpdateActiveUI(-1);
            }
            else if (context.action.name == "ScrollD")
            {
                UpdateActiveUI(1);
            }
        }
    }

    private void OnUse(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "UseP")
            {
                UsePotion(usePotion);
            }
            else if (context.action.name == "Use")
            {
                if (skillCanvas.activeInHierarchy)
                {
                    UseSkill();
                }
                else if(blessingCanvas.activeInHierarchy)
                {
                    ActivateBlessing();
                }
                else if (orbCanvas.activeInHierarchy)
                {
                    UseOrb();
                }
            }
        }
    }

    private void OnPass(InputAction.CallbackContext context)
    {
        if (context.action.name != "Pass") { return; }

        if (context.performed)
        {
            UsePotion(passPotion);
        }
    }

    private void OnTactics(InputAction.CallbackContext context)
    {
        if (context.performed && usingTacticsMenu)
        {
            if(context.action.name == "Tactics1")
            {
                //Play SFX
                AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

                //Open Blessing Menu
                OpenBlessingMenu();
            }
            else if(context.action.name == "Tactics2")
            { 
                //Play SFX
                AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

                //Open Change Weapon Menu
                OpenWeaponSwitchUI();
            }
            else if (context.action.name == "Tactics3")
            {
                //Open Swap Member Menu
            }
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            ExitCollection();
        }
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            if (usingTacticsMenu)
            {
                playerInput.onActionTriggered += OnTactics;
            }
            else
            {
                playerInput.onActionTriggered += OnUse;
                playerInput.onActionTriggered += OnCycle;
                playerInput.onActionTriggered += OnPass;
                playerInput.onActionTriggered += OnScroll;
            }

            playerInput.onActionTriggered += OnCancel;
        }
        else
        {
            if (usingTacticsMenu)
            {
                playerInput.onActionTriggered -= OnTactics;
            }
            else
            {
                playerInput.onActionTriggered -= OnUse;
                playerInput.onActionTriggered -= OnCycle;
                playerInput.onActionTriggered -= OnPass;
                playerInput.onActionTriggered -= OnScroll;
            }

            playerInput.onActionTriggered -= OnCancel;
        }
    }

    private void OnDestroy()
    {
        ControlsManager.Instance.RemoveIControls(this);
    }

    //Saving
    [System.Serializable]
    public class ChargeableData
    {
        public string playerName;
        public string chargeableID;
        public int chargeableHealth;
    }


    public object CaptureState()
    {
        chargeableState.Clear();

        foreach(var item in chargeablesOnCooldownCurrentHealth)
        {
            foreach (ItemChargingData chargingData in item.Value)
            {
                ChargeableData data = new ChargeableData();

                data.playerName = item.Key.unitName;
                data.chargeableID = chargingData.chargingItem.GetID();
                data.chargeableHealth = chargingData.currentHealth;

                chargeableState.Add(data);
            }
        }

        return SerializationUtility.SerializeValue(chargeableState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null)
        {
            return;
        }

        byte[] bytes = state as byte[];
        chargeableState = SerializationUtility.DeserializeValue<List<ChargeableData>>(bytes, DataFormat.Binary);

        chargeablesOnCooldownCurrentHealth.Clear();

        foreach (ChargeableData chargeableData in chargeableState)
        {
            PlayerGridUnit player = PartyManager.Instance.GetPlayerUnitViaName(chargeableData.playerName);

            if (!chargeablesOnCooldownCurrentHealth.ContainsKey(player))
            {
                chargeablesOnCooldownCurrentHealth[player] = new List<ItemChargingData>();
            }

            chargeablesOnCooldownCurrentHealth[player].Add(new ItemChargingData(TheCache.Instance.GetItemByID(chargeableData.chargeableID), chargeableData.chargeableHealth));

            //Debug.Log("Restoring: " + TheCache.Instance.GetItemByID(chargeableData.chargeableID) + "to health: " + chargeableData.chargeableHealth);
        }
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }
}
