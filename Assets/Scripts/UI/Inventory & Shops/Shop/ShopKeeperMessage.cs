using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopKeeperMessage : MonoBehaviour
{
    [SerializeField] MessageType messageType;
    [SerializeField] TextMeshProUGUI messageText;
    public enum MessageType
    {
        Default,
        Broke,
        SoldOut,
        Owned,
        Inexperienced,
        Confirm,
        EmptyCategory,
        BoughtItem,
        WeaponOnly,
        ArmourOnly,
        SlotsFull,
        NoSlots,
        CantDisenchant,
        InfusedEnchantment
    }

    public bool DisplayMessage(Item selectedItem, Shop shop, PlayerGridUnit playerToCompare, BaseShopUI shopUI, bool confirmingPurchase, bool purchaseMade)
    {
        switch (messageType)
        {
            case MessageType.BoughtItem:
                return selectedItem && purchaseMade;
            case MessageType.EmptyCategory:
                return !selectedItem;
            case MessageType.NoSlots:
                return !InventoryManager.Instance.HasEnchantmentSlots(selectedItem);
            case MessageType.SlotsFull:
                return !InventoryManager.Instance.CanEnchantEquipment(null, selectedItem);
            case MessageType.CantDisenchant:
                return !InventoryManager.Instance.CanDisenchantEquipment(selectedItem);
            case MessageType.InfusedEnchantment:
                return selectedItem is InfusedEnchantment;
            case MessageType.WeaponOnly:
                return shopUI.selectedEquipment && shopUI.selectedEquipment is Armour && !InventoryManager.Instance.CanEnchantEquipment(selectedItem as Enchantment, shopUI.selectedEquipment);
            case MessageType.ArmourOnly:
                return shopUI.selectedEquipment && shopUI.selectedEquipment is Weapon && !InventoryManager.Instance.CanEnchantEquipment(selectedItem as Enchantment, shopUI.selectedEquipment);
            case MessageType.Broke:
                if(shopUI is ShopBuyUI)
                {
                    return selectedItem && selectedItem.buyPrice > (shop.isFantasyShop ? InventoryManager.Instance.fantasyMoney : InventoryManager.Instance.modernMoney);
                }
                else if(shopUI is ShopEnchantUI)
                {
                    return selectedItem && shop.CalculateEnchantmentPrice(selectedItem as Enchantment) > (shop.isFantasyShop ? InventoryManager.Instance.fantasyMoney : InventoryManager.Instance.modernMoney);
                }
                return false;
            case MessageType.SoldOut:
                return selectedItem && shop.AvailableStock()[selectedItem] == 0;
            case MessageType.Owned:
                if (!selectedItem)
                    return false;

                int numOwned = InventoryManager.Instance.GetItemCountAcrossAllInventories(selectedItem);
                if (numOwned > 0)
                    messageText.text = "You currently possess <color=white>"+ numOwned.ToString() +"</color> of these.";

                return numOwned > 0;
            case MessageType.Inexperienced:
                return selectedItem && selectedItem.requiredLevel > playerToCompare.stats.level;
            case MessageType.Confirm:
                return selectedItem && confirmingPurchase;
            default:
                return true;
        }
    }


}
