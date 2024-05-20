using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AnotherRealm;
using System.Linq;

public class ShopItemUI : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] Image icon;
    [SerializeField] GameObject newItemBadge;
    [Space(5)]
    [SerializeField] GameObject ineligibleTint;
    [Header("Headers")]
    [SerializeField] Transform bgHeader;
    [Space(5)]
    [SerializeField] Transform topBarHeader;
    [SerializeField] Transform bottomBarHeader;
    [Space(5)]
    [SerializeField] Transform affinityHeader;
    [SerializeField] Transform potionHeader;
    [SerializeField] Transform enchantmentSlotsHeader;
    [Space(5)]
    [SerializeField] RectTransform selectedMenuTransform;
    [Header("Texts")]
    [SerializeField] TextMeshProUGUI nameText;
    [Space(5)]
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] TextMeshProUGUI countText;

    public void SetSelected(Item item, bool isSelected)
    {
        foreach (Transform child in bgHeader)
        {
            child.gameObject.SetActive(false);
        }

        ActivateBG(item, isSelected);
    }

    public void UpdateData(Item item, int count, bool isSelected, Shop shop, bool isBuying)
    {
        nameText.text = item.itemName;
        priceText.text = (isBuying ? item.buyPrice : item.GetSellPrice()).ToString();
        icon.sprite = item.UIIcon;

        countText.text = count.ToString();

        foreach (Transform child in bgHeader)
        {
            child.gameObject.SetActive(false);
        }

        foreach (Transform child in topBarHeader)
        {
            child.gameObject.SetActive(false);
        }

        ActivateBG(item, isSelected);

        UpdateTopBar(item, shop.GetPlayersAtShop());

        UpdateBottomBar(count, isBuying);

        UpdateEnchanttmentSlots(item);

        newItemBadge.SetActive(shop.IsItemNew(item) && isBuying);

        float moneyToCompareTo = shop.isFantasyShop ? InventoryManager.Instance.fantasyMoney : InventoryManager.Instance.modernMoney;
        ineligibleTint.SetActive((isBuying && count == 0) || (isBuying && item.buyPrice > moneyToCompareTo));
    }


    private void UpdateEnchanttmentSlots(Item item)
    {
        Weapon weapon = item as Weapon;
        Armour armour = item as Armour;

        int numOfSlots = 0;

        if (weapon || armour)
        {
            numOfSlots = weapon ? weapon.numOfEnchantmentSlots : armour.numOfEnchantmentSlots;
        }

        bool shouldActivate = (weapon || armour) && numOfSlots > 0;

        enchantmentSlotsHeader.gameObject.SetActive(shouldActivate);
        if (!shouldActivate) { return; }

        List<Enchantment> enchantments = weapon ? weapon.infusedEnchantments.Concat(weapon.embeddedEnchantments).ToList() : armour.infusedEnchantments.Concat(armour.embeddedEnchantments).ToList();

        foreach (Transform child in enchantmentSlotsHeader)
        {
            int childIndex = child.GetSiblingIndex();
            child.gameObject.SetActive(childIndex < numOfSlots);

            if (childIndex >= numOfSlots) { continue; }

            bool isEmptySlot = childIndex >= enchantments.Count;

            child.GetChild(0).gameObject.SetActive(isEmptySlot); //Empty: 0
            child.GetChild(1).gameObject.SetActive(!isEmptySlot && enchantments[childIndex] is InfusedEnchantment); //Infused: 1
            child.GetChild(2).gameObject.SetActive(!isEmptySlot && !(enchantments[childIndex] is InfusedEnchantment)); // Fill: 2
        }
    }


    private void UpdateBottomBar(int count, bool isBuying)
    {
        bottomBarHeader.GetChild(0).gameObject.SetActive(count > 0 || !isBuying); //In Stock
        bottomBarHeader.GetChild(1).gameObject.SetActive(count == 0 && isBuying); //Out Of Stock
    }

    private void UpdateTopBar(Item item, List<PlayerGridUnit> playersAtShop)
    {
        ActivateEquippedIcon(item, playersAtShop);

        Weapon weapon = item as Weapon;
        Potion potion = item as Potion;

        if (weapon)
        {
            affinityHeader.gameObject.SetActive(true);
            foreach (Transform child in affinityHeader)
            {
                child.gameObject.SetActive(CombatFunctions.GetAffinityIndex(weapon.element, weapon.material) == child.GetSiblingIndex());
            }
        }

        if (potion)
        {
            potionHeader.gameObject.SetActive(true);

            foreach (Transform child in potionHeader)
            {
                child.gameObject.SetActive(CombatFunctions.GetPotionIconIndex(potion.potionIcon) == child.GetSiblingIndex());
            }
        }
    }

    private void ActivateEquippedIcon(Item item, List<PlayerGridUnit> playersAtShop)
    {
        bool activate = false;

        switch (item.itemCatergory)
        {
            case ItemCatergory.Tools:
                //player.stats
                break;
            default:
                activate = playersAtShop.Any((player) => player.stats.IsEquipped(item));
                break;
        }

        topBarHeader.GetChild(0).gameObject.SetActive(activate);
    }

    private void ActivateBG(Item item, bool isSelected)
    {
        if (isSelected)
        {
            bgHeader.GetChild(0).gameObject.SetActive(true);
            return;
        }

        switch (item.rarity)
        {
            case Rarity.Common:
                bgHeader.GetChild(1).gameObject.SetActive(true);
                break;
            case Rarity.Legendary:
                bgHeader.GetChild(3).gameObject.SetActive(true);
                break;
            default:
                bgHeader.GetChild(2).GetComponent<Image>().color = InventoryManager.Instance.GetRarityColor(item.rarity);
                bgHeader.GetChild(2).gameObject.SetActive(true);
                break;

        }

    }

    public RectTransform GetMenuTransform()
    {
        return selectedMenuTransform;
    }
}
