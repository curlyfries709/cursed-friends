using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AnotherRealm;
using System.Linq;

public class InventoryItemUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] Image icon;
    [Space(5)]
    [SerializeField] GameObject ineligibleTint;
    [Header("Headers")]
    [SerializeField] Transform bgHeader;
    [SerializeField] Transform topBarHeader;
    [Space(5)]
    [SerializeField] Transform affinityHeader;
    [SerializeField] Transform potionHeader;
    [SerializeField] Transform enchantmentSlotsHeader;
    [Space(5)]
    [SerializeField] RectTransform selectedMenuTransform;
    [Header("Texts")]
    [SerializeField] TextMeshProUGUI nameText;
    [Space(5)]
    [SerializeField] TextMeshProUGUI weightText;
    [SerializeField] TextMeshProUGUI countText;

    public void SetSelected(Item item, bool isSelected)
    {
        foreach (Transform child in bgHeader)
        {
            child.gameObject.SetActive(false);
        }

        ActivateBG(item, isSelected);
    }


    public void UpdateData(Item item, PlayerGridUnit owner, bool isSelected)
    {
        int count = InventoryManager.Instance.GetItemCount(item, owner);

        nameText.text = item.itemName;
        weightText.text = (item.weight * count).ToString("F1");
        icon.sprite = item.UIIcon;

        countText.text = count.ToString();

        foreach (Transform child in bgHeader)
        {
            child.gameObject.SetActive(false);     
        }

        foreach(Transform child in topBarHeader)
        {
            child.gameObject.SetActive(false);
        }

        ineligibleTint.SetActive(false);

        ActivateBG(item, isSelected);

        UpdateTopBar(item, owner);

        UpdateEnchanttmentSlots(item);
    }

    public void SetIneligible(Shop shop, Item item, Item selectedEquipment, bool isEnchantment) //Used by Shops
    {
        Enchantment enchantment = item as Enchantment;

        //Enchantment only Invalid if cannot afford it && Can be enchanted.
        if (enchantment && isEnchantment)
        {
            float price = shop.CalculateEnchantmentPrice(enchantment);
            bool broke = !InventoryManager.Instance.CanAfford(price, shop.isFantasyShop);

            ineligibleTint.SetActive(broke || !InventoryManager.Instance.CanEnchantEquipment(enchantment, selectedEquipment));     
            return;
        }

        Weapon weapon = item as Weapon;
        Armour armour = item as Armour;

        if(!weapon && !armour)
        {
            ineligibleTint.SetActive(false);
            return;
        }

        //Means Its either weapon or armour
        if (isEnchantment)
        {
            ineligibleTint.SetActive(!InventoryManager.Instance.CanEnchantEquipment(null, item));
        }
        else
        {
            ineligibleTint.SetActive(!InventoryManager.Instance.CanDisenchantEquipment(item));
        }
    }

    private void UpdateTopBar(Item item, PlayerGridUnit owner)
    {
        ActivateEquippedIcon(item, owner);

        Weapon weapon = item as Weapon;
        Potion potion = item as Potion;

        if(weapon)
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(topBarHeader as RectTransform);
    }

    private void UpdateEnchanttmentSlots(Item item)
    {
        Weapon weapon = item as Weapon;
        Armour armour = item as Armour;

        int numOfSlots = 0;

        if(weapon || armour)
        {
            numOfSlots = weapon ? weapon.numOfEnchantmentSlots : armour.numOfEnchantmentSlots;
        }

        bool shouldActivate = (weapon || armour) && numOfSlots > 0;

        enchantmentSlotsHeader.gameObject.SetActive(shouldActivate);
        if(!shouldActivate) { return; }

        List<Enchantment> enchantments = weapon ? weapon.infusedEnchantments.Concat(weapon.embeddedEnchantments).ToList() : armour.infusedEnchantments.Concat(armour.embeddedEnchantments).ToList();

        foreach (Transform child in enchantmentSlotsHeader)
        {
            int childIndex = child.GetSiblingIndex();
            child.gameObject.SetActive(childIndex < numOfSlots);

            if (childIndex >= numOfSlots){ continue; }

            bool isEmptySlot = childIndex >= enchantments.Count;

            child.GetChild(0).gameObject.SetActive(isEmptySlot); //Empty: 0
            child.GetChild(1).gameObject.SetActive(!isEmptySlot && enchantments[childIndex] is InfusedEnchantment); //Infused: 1
            child.GetChild(2).gameObject.SetActive(!isEmptySlot && !(enchantments[childIndex] is InfusedEnchantment)); // Fill: 2
        }
    }

    private void ActivateEquippedIcon(Item item, PlayerGridUnit player)
    {
        bool activate = false;

        switch (item.itemCatergory)
        {
            case ItemCatergory.Tools:
                //player.stats
                break;
            default:
                activate = player.stats.IsEquipped(item);
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
