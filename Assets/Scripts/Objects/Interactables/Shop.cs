using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using Sirenix.Serialization;

public class Shop : Interact, ISaveable
{
    [Title("Interaction Triggers")]
    [SerializeField] CinematicTrigger cinematicTrigger;
    [SerializeField] Dialogue dialogueToPlay;
    //[SerializeField] Dialogue defaultDialogue;
    [Title("Shop")]
    public bool isFantasyShop;
    [Tooltip("The First UI that should be displayed when shop opens")]
    [SerializeField] ShopFrontUI shopFrontUI;
    [SerializeField] AudioClip shopMusic;
    [Space(10)]
    [Range(1, 6)]
    [SerializeField] float enchantmentMultiplier = 3;
    [Space(5)]
    [SerializeField] int disenchantmentBaseCost = 300;
    [SerializeField] int disenchantmentCostIncreasePerEnchantment = 200;
    [Title("Discussion")]
    [SerializeField] DiscussionUI discussionUI;
    [Title("Stock")]
    [ListDrawerSettings(Expanded = true)]
    [SerializeField] List<ShopStockCollection> stockByLevel;

    //Saving Data
    [SerializeField, HideInInspector]
    private ShopState shopState = new ShopState();
    bool isDataRestored = false;

    Dictionary<Item, int> dailyStock = new Dictionary<Item, int>();
    List<Item> oldStockList = new List<Item>();
    [HideInInspector] public bool visitedBuySection = false;

    bool subscribedToDialogueEnd = false;


    private void OnEnable()
    {
        Victory.PlayerLevelledUp += OnPlayerLevelledUp;
        SavingLoadingManager.Instance.NewRealmEntered += OnNewRealmEntered;
    }

    private void OnNewRealmEntered(RealmType newRealm)
    {
        if((isFantasyShop && newRealm == RealmType.Fantasy) ||
            !isFantasyShop && newRealm == RealmType.Modern)
        {
            SetDailyStock(PartyManager.Instance.GetLeaderLevel());
        }
    }

    //Interaction
    public override void HandleInteraction(bool inCombat)
    {
        if (inCombat)
        {
            BeginCombatInteraction(new List<GridUnit>());
            BeginShopping();
        }
        else
        {
            HandleOutsideCombatInteraction();
        }
    }

    private void HandleOutsideCombatInteraction()
    {
        if (cinematicTrigger && cinematicTrigger.playOnInteraction)
        {
            if (cinematicTrigger.PlayCinematic())
            {
                cinematicTrigger = null;
            }
            else
            {
                DiscussOrShop();
            }
        }
        else if (dialogueToPlay)
        {
            PlayDialogue();
        }
        else
        {
            DiscussOrShop();
        }
    }

    private void PlayDialogue()
    {
        DialogueManager.Instance.DialogueEnded += DiscussOrShop;
        DialogueManager.Instance.PlayDialogue(dialogueToPlay, false);

        subscribedToDialogueEnd = true;
        dialogueToPlay = null;
    }

    public void DiscussOrShop()
    {
        if (subscribedToDialogueEnd)
        {
            DialogueManager.Instance.DialogueEnded -= DiscussOrShop;
            subscribedToDialogueEnd = false;
        }
            
        //if (discussionUI && discussionUI.HasDiscussions())
        if(discussionUI) //AUTO ENABLED DISCUSSION UI TO TEST CARD GAMEPLAY, REVERT TO CODE ABOVE.
        {
            discussionUI.gameObject.SetActive(true);
        }
        else
        {
            BeginShopping();
        }
    }
    //Cinematic
    public void SetCinematicToPlay(CinematicTrigger cinematicTrigger)
    {
        this.cinematicTrigger = cinematicTrigger;
    }

    //Stock Managing 
    public bool PurchaseItem(Item item, int count, PlayerGridUnit selectedInventory)
    {
        float cost = item.buyPrice * count;

        if (InventoryManager.Instance.TrySpendCoin(cost, isFantasyShop))
        {
            //Add To Inventory
            InventoryManager.Instance.AddMultipleToInventory(selectedInventory, item, count);

            //Update daily Stock
            dailyStock[item] = dailyStock[item] - count;
            return true;
        }

        return false;
    }

    public void SellItem(Item item, int count, PlayerGridUnit selectedInventory)
    {
        float cost = item.GetSellPrice() * count;

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.ShopCoin);

        InventoryManager.Instance.RemoveMultipleFromInventory(selectedInventory, item, count);
        InventoryManager.Instance.EarnCoin(cost, isFantasyShop);
    }

    public void SellAll(Item item)
    {
        float cost = 0;

        foreach (PlayerGridUnit player in GetPlayersAtShop())
        {
            int count = InventoryManager.Instance.GetItemCount(item, player) - GetEquippedCount(item, player);
            cost = cost + (count * item.GetSellPrice());
            InventoryManager.Instance.RemoveMultipleFromInventory(player, item, count);
        }

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.ShopCoin);

        InventoryManager.Instance.EarnCoin(cost, isFantasyShop);
    }

    public float CalculateEnchantmentPrice(Enchantment enchantment)
    {
        return enchantment.buyPrice * enchantmentMultiplier;
    }

    public float CalculateDisenchantmentPrice(Item equipment)
    {
        int numOfEnchantments = equipment is Weapon ? (equipment as Weapon).infusedEnchantments.Count + (equipment as Weapon).embeddedEnchantments.Count
            : (equipment as Armour).infusedEnchantments.Count + (equipment as Armour).embeddedEnchantments.Count;

        return disenchantmentBaseCost  + ((numOfEnchantments - 1)* disenchantmentCostIncreasePerEnchantment);
    }

    public int GetEquippedCount(Item item, PlayerGridUnit player)
    {
        Weapon weapon = item as Weapon;
        Armour armour = item as Armour;

        if (weapon && player.stats.IsEquipped(weapon))
        {
            return 1;
        }
        else if (armour && player.stats.IsEquipped(armour))
        {
            return 1;
        }

        return 0;
    }

    LevelUpResult OnPlayerLevelledUp(PartyMemberData player, int newLevel)
    {
        //check it is leader
        if (!PartyManager.Instance.IsLeader(player))
        {
            return null;
        }

        SetDailyStock(newLevel);
        return null;
    }

    private void SetDailyStock(int leaderLevel)
    {
        foreach (ShopStockCollection collection in stockByLevel)
        {
            foreach (ShopItem stock in collection.stockCollection)
            {
                if (leaderLevel < collection.levelToAddStockToShop || leaderLevel >= collection.levelToStopShowingStock)
                {
                    dailyStock.Remove(stock.item);
                    continue;
                }
                else if (!dailyStock.ContainsKey(stock.item)) //If Key not found, Set to Original Value.
                {
                    dailyStock[stock.item] = stock.dailyStockCount;
                }
            }
        }
    }

    private void SetOldStockList()
    {
        oldStockList.Clear();

        foreach (var item in dailyStock)
        {
            oldStockList.Add(item.Key);
        }
    }

    private void RefreshDailyStock(GameDate gameDate)//Called on New Day Event
    {
        dailyStock.Clear();
 
        int leaderLevel = PartyManager.Instance.GetLeaderLevel();

        foreach (ShopStockCollection collection in stockByLevel)
        {
            if (leaderLevel < collection.levelToAddStockToShop || leaderLevel >= collection.levelToStopShowingStock) { continue; }

            foreach (ShopItem stock in collection.stockCollection)
            {
                dailyStock[stock.item] = stock.dailyStockCount;
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        Victory.PlayerLevelledUp -= OnPlayerLevelledUp;
        SavingLoadingManager.Instance.NewRealmEntered -= OnNewRealmEntered;
    }

    //Shopping Handy Methods
    public void BeginShopping()
    {
        if (discussionUI)
            discussionUI.gameObject.SetActive(false);

        visitedBuySection = false;

        //Play Music
        AudioManager.Instance.PlayCustomMusic(shopMusic);
        AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

        HUDManager.Instance.HideHUDs();
        shopFrontUI.BeginShopping();
    }

    public List<PlayerGridUnit> GetPlayersAtShop()
    {
        List<PlayerGridUnit> playersAtShop = new List<PlayerGridUnit>();

        if (FantasyCombatManager.Instance.InCombat())
        {
            playersAtShop.Add(FantasyCombatManager.Instance.GetActiveUnit() as PlayerGridUnit);
        }
        else
        {
            playersAtShop = new List<PlayerGridUnit>(PartyManager.Instance.GetAllPlayerMembersInWorld());
        }

        return playersAtShop;
    }

    public void ReturnToShopFront()
    {
        shopFrontUI.BeginShopping();
    }

    public void ShoppingComplete()
    {
        if(visitedBuySection)
            SetOldStockList();

        bool inCombat = FantasyCombatManager.Instance.InCombat();

        if (inCombat)
        {
            //Play Battle Music
            AudioManager.Instance.PlayBattleMusic(FantasyCombatManager.Instance.battleTrigger);

            //Interaction Complete.
            CombatInteractionComplete();
        }
        else
        {
            if (cinematicTrigger && !cinematicTrigger.playOnInteraction && cinematicTrigger.PlayCinematic())
            {
                cinematicTrigger = null;
                return;
            }

            //Return To Game.
            AudioManager.Instance.PlayMusic(MusicType.Roam);
            ControlsManager.Instance.SwitchCurrentActionMap("Player");
        }
    }

    public bool IsItemNew(Item item)
    {
        return !oldStockList.Contains(item);
    }

    public Dictionary<Item, int> AvailableStock()
    {
        return dailyStock;
    }

    //Saving
    [System.Serializable]
    public class ShopState
    {
        //Stock
        public Dictionary<string, int> dailyStock = new Dictionary<string, int>();
        public List<string> oldStockList = new List<string>();

        public Vector3 position;
        public Quaternion rotation;
    }

    public object CaptureState()
    {
        shopState.dailyStock = dailyStock.ToDictionary(k => k.Key.GetID(), k => k.Value);
        shopState.oldStockList = oldStockList.ConvertAll((item) => item.GetID());

        shopState.position = transform.position;
        shopState.rotation = transform.rotation;

        return SerializationUtility.SerializeValue(shopState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        isDataRestored = true;

        if (state == null)
        {
            SetDailyStock(1); //Player Would Be Level 1 when this called. As This called during new game.
            return;
        }

        byte[] bytes = state as byte[];

        shopState = SerializationUtility.DeserializeValue<ShopState>(bytes, DataFormat.Binary);
        dailyStock = shopState.dailyStock.ToDictionary(k => TheCache.Instance.GetItemByID(k.Key), k => k.Value);
        oldStockList = TheCache.Instance.GetItemsById(shopState.oldStockList);

        /*transform.position = shopState.position;
        transform.rotation = shopState.rotation;

        if(TryGetComponent(out DynamicObstacle dynamicObstacle))
        {

        }*/
    }

    public bool IsDataRestored()
    {
        return isDataRestored;
    }

}
