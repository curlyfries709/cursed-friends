using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AnotherRealm;
using System.Linq;
using System;

public class InventoryDetailUI : MonoBehaviour
{
    [Header("Detail Type")]
    [SerializeField] ItemCatergory dataDetailType;
    [Header("Common Text")]
    [SerializeField] TextMeshProUGUI itemNameText;
    [SerializeField] TextMeshProUGUI rarityText;
    [SerializeField] TextMeshProUGUI weightText;
    [SerializeField] TextMeshProUGUI levelText;
    [Space(5)]
    [SerializeField] TextMeshProUGUI categoryText;
    [Space(10)]
    [SerializeField] TextMeshProUGUI description;
    [SerializeField] TextMeshProUGUI cooldown;
    [Header("Headers")]
    [SerializeField] Transform raceRestrictionHeader;
    [SerializeField] Transform affinityHeader;
    [Header("Enchantments")]
    [SerializeField] GameObject enchantmentHeader;
    [SerializeField] List<GameObject> enchantmentEffectsPool = new List<GameObject>();
    [Header("Equipment")]
    [SerializeField] GameObject attributesTitleHeader;
    [SerializeField] Transform attributesGridHeader;
    [Header("Attributes")]
    [SerializeField] TextMeshProUGUI physAttack;
    [SerializeField] TextMeshProUGUI magAttack;
    [Space(5)]
    [SerializeField] TextMeshProUGUI armour;
    [Header("Scaling")]
    [SerializeField] TextMeshProUGUI equippedScalingGrade;
    [SerializeField] TextMeshProUGUI itemScalingGrade;
    [SerializeField] TextMeshProUGUI scalingAttribute;
    [Header("Colors")]
    [SerializeField] Color ineligibleColor;
    [Space(5)]
    [SerializeField] Color upgradeColor;
    [SerializeField] Color downgradeColor;

    const string levelPrepend = "<size=50%>Lvl</size> ";
    Color enchantmentTextColor;

    private void Awake()
    {
        if(enchantmentEffectsPool.Count > 0 && enchantmentEffectsPool[0])
        {
            GameObject effect = enchantmentEffectsPool[0];
            TextMeshProUGUI effectText = effect.transform.GetChild(effect.transform.childCount - 1).GetComponent<TextMeshProUGUI>();

            enchantmentTextColor = effectText.color;
        }
      
    }

    public void SetData(Item item, PlayerGridUnit player)
    {
        Weapon weapon = item as Weapon;
        Armour armour = item as Armour;
        Enchantment enchantment = item as Enchantment;
        //Orb
        if (weapon && dataDetailType == ItemCatergory.Weapons)
        {
            SetWeaponData(weapon, player);
        }
        else if (armour && dataDetailType == ItemCatergory.Armour)
        {
            SetArmourData(armour, player);
        }
        else if (enchantment && dataDetailType == ItemCatergory.Enchantments)
        {
            SetEnchantmentData(enchantment, player);
        }
        else if (!weapon && !armour && !enchantment && dataDetailType == ItemCatergory.Ingredients)
        {
            SetDefaultData(item, player);
        }
        else
        {
            gameObject.SetActive(false);
        }

    }

    private void SetWeaponData(Weapon weapon, PlayerGridUnit player)
    {
        gameObject.SetActive(true);

        Weapon equipped = player.stats.Weapon();
        itemNameText.text = weapon.itemName;

        //Category
        categoryText.text = weapon.category.ToString();

        foreach (Transform affinity in affinityHeader)
        {
            affinity.gameObject.SetActive(CombatFunctions.GetAffinityIndex(weapon.element, weapon.material) == affinity.GetSiblingIndex());
        }

        rarityText.text = weapon.rarity.ToString();
        rarityText.color = InventoryManager.Instance.GetRarityColor(weapon.rarity);

        weightText.text = weapon.weight.ToString();

        //Restrictions
        levelText.text = levelPrepend + weapon.requiredLevel.ToString();
        levelText.color = player.stats.level < weapon.requiredLevel ? ineligibleColor : Color.white;

        bool isScalingUpgrade = weapon.scalingPercentage >= equipped.scalingPercentage;

        //Stats
        itemScalingGrade.text = TheCalculator.Instance.GetWeaponScalingGrade(weapon);
        equippedScalingGrade.text = TheCalculator.Instance.GetWeaponScalingGrade(equipped);

        equippedScalingGrade.color = isScalingUpgrade ? upgradeColor : downgradeColor;

        Transform scalingIconHeader = itemScalingGrade.transform.GetChild(0);

        scalingIconHeader.GetChild(0).gameObject.SetActive(isScalingUpgrade);
        scalingIconHeader.GetChild(1).gameObject.SetActive(!isScalingUpgrade);

        scalingAttribute.text = weapon.scalingAttribute.ToString();

        physAttack.text = "Phy: " + weapon.basePhysAttack;
        magAttack.text = "Mag: " + weapon.baseMagAttack;

        bool isPhysUpgrade = weapon.basePhysAttack >= equipped.basePhysAttack;
        bool isMagUpgrade = weapon.baseMagAttack >= equipped.baseMagAttack;

        //Phys Differecce
        Transform physIcon = physAttack.transform.GetChild(0);

        physIcon.GetChild(0).gameObject.SetActive(isPhysUpgrade);
        physIcon.GetChild(1).gameObject.SetActive(!isPhysUpgrade);

        TextMeshProUGUI physDifference = physAttack.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        physDifference.text = Mathf.Abs(equipped.basePhysAttack - weapon.basePhysAttack).ToString();
        physDifference.color = isPhysUpgrade ? upgradeColor : downgradeColor;

        //Mag Differecce
        Transform magIcon = magAttack.transform.GetChild(0);

        magIcon.GetChild(0).gameObject.SetActive(isMagUpgrade);
        magIcon.GetChild(1).gameObject.SetActive(!isMagUpgrade);

        TextMeshProUGUI magDifference = magAttack.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        magDifference.text = Mathf.Abs(equipped.baseMagAttack - weapon.baseMagAttack).ToString();
        magDifference.color = isMagUpgrade ? upgradeColor : downgradeColor;

        UpdateAttributes(weapon, player.stats.Weapon());
        UpdateEnchantments(weapon);

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private void SetArmourData(Armour armour, PlayerGridUnit player)
    {
        gameObject.SetActive(true);

        Armour equipped = player.stats.EquippedArmour();
        itemNameText.text = armour.itemName;


        rarityText.text = armour.rarity.ToString();
        rarityText.color = InventoryManager.Instance.GetRarityColor(armour.rarity);

        weightText.text = armour.weight.ToString();

        //Restrictions
        levelText.text = levelPrepend + armour.requiredLevel.ToString();
        levelText.color = player.stats.level < armour.requiredLevel ? ineligibleColor : Color.white;

        List<string> races = new List<string>();

        foreach (Race race in armour.raceRestriction)
        {
            races.Add(race.ToString());
        }

        foreach (Transform race in raceRestrictionHeader)
        {
            race.gameObject.SetActive(PartyData.Instance.GetAllPlayerMembersInWorld().Any((player) => player.stats.data.race.ToString() == race.name));
            race.GetComponent<Image>().color = races.Contains(race.name) ? Color.white : ineligibleColor;
        }

        this.armour.text = armour.armour.ToString();

        //Armour Differecce
        Transform armourIcon = this.armour.transform.GetChild(0);
        bool isUpgrade = armour.armour >= equipped.armour;

        armourIcon.GetChild(0).gameObject.SetActive(isUpgrade);
        armourIcon.GetChild(1).gameObject.SetActive(!isUpgrade);

        TextMeshProUGUI armourDifference = this.armour.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        armourDifference.text = Mathf.Abs(equipped.armour - armour.armour).ToString();
        armourDifference.color = isUpgrade ? upgradeColor : downgradeColor;

        UpdateAttributes(armour, player.stats.EquippedArmour());
        UpdateEnchantments(armour);

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private void SetEnchantmentData(Enchantment enchantment, PlayerGridUnit player)
    {
        gameObject.SetActive(true);

        itemNameText.text = enchantment.itemName;

        rarityText.text = enchantment.rarity.ToString();
        rarityText.color = InventoryManager.Instance.GetRarityColor(enchantment.rarity);

        weightText.text = enchantment.weight.ToString();

        int iconIndex = 0;

        //Category
        switch (enchantment.equipRestriction)
        {
            case Enchantment.Restriction.Armour:
                iconIndex = 1;
                categoryText.text = "Armour";
                break;
            case Enchantment.Restriction.Weapon:
                categoryText.text = "Weapon";
                break;
            case Enchantment.Restriction.Both:
                iconIndex = 2;
                categoryText.text = "Weapon & Armour";
                break;
        }

        foreach(Transform icon in affinityHeader)
        {
            icon.gameObject.SetActive(icon.GetSiblingIndex() == iconIndex);
        }

        //Restrictions
        levelText.text = levelPrepend + enchantment.requiredLevel.ToString();
        levelText.color = player.stats.level < enchantment.requiredLevel ? ineligibleColor : Color.white;

        description.text = enchantment.description;

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private void SetDefaultData(Item item, PlayerGridUnit player)
    {
        gameObject.SetActive(true);

        itemNameText.text = item.itemName;

        rarityText.text = item.rarity.ToString();
        rarityText.color = InventoryManager.Instance.GetRarityColor(item.rarity);

        weightText.text = item.weight.ToString();

        //Restrictions
        levelText.text = levelPrepend + item.requiredLevel.ToString();
        levelText.color = player.stats.level < item.requiredLevel ? ineligibleColor : Color.white;

        description.text = item.description;

        Blessing blessing = item as Blessing;
        cooldown.gameObject.SetActive(blessing);

        if (blessing)
        {
            cooldown.text = "Duration: <size=150%>" + blessing.duration.ToString() +"</size> turns";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }



    private void UpdateAttributes(Item newItem, Item equippedItem)
    {
        Weapon weapon = newItem as Weapon;
        Armour armour = newItem as Armour;

        Weapon equippedWeapon = null;
        Armour equippedArmour = null;

        List<AttributeBonus> attributeBonuses;
        List<AttributeBonus> equippedAttributeBonuses;

        if (weapon)
        {
            equippedWeapon = equippedItem as Weapon;

            attributeBonuses = weapon.bonusAttributes;
            equippedAttributeBonuses = equippedWeapon.bonusAttributes;
        }
        else
        {
            equippedArmour = equippedItem as Armour;

            attributeBonuses = armour.bonusAttributes;
            equippedAttributeBonuses = equippedArmour.bonusAttributes;
        }

        attributesTitleHeader.SetActive(!(attributeBonuses.Count == 0 && equippedAttributeBonuses.Count == 0));

        var attributeArray = Enum.GetNames(typeof(Attribute));

        for (int i = 0; i < attributeArray.Length; i++)
        {
            Transform attributeChild = attributesGridHeader.GetChild(i);
            Transform attributeIcon = attributeChild.GetChild(0);

            TextMeshProUGUI attributeText = attributeChild.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI attributeDifference = attributeChild.GetChild(1).GetComponent<TextMeshProUGUI>();

            Attribute attribute = (Attribute)i;

            AttributeBonus attributeBonus = attributeBonuses.FirstOrDefault((item) => item.attribute == attribute);
            AttributeBonus equippedAttributeBonus = equippedAttributeBonuses.FirstOrDefault((item) => item.attribute == attribute);

            if (attributeBonus != null || equippedAttributeBonus != null)
            {
                attributeChild.gameObject.SetActive(true);

                int attributeValue = 0;
                int equippedValue = 0;

                if(attributeBonus != null)
                {
                    attributeValue = attributeBonus.attributeChange;
                }

                if (equippedAttributeBonus != null)
                {
                    equippedValue = equippedAttributeBonus.attributeChange;
                }

                bool isUpgrade = attributeValue >= equippedValue;

                attributeText.text = GetAttributePrepend(attribute) + attributeValue.ToString();

                attributeIcon.GetChild(0).gameObject.SetActive(isUpgrade);
                attributeIcon.GetChild(1).gameObject.SetActive(!isUpgrade);

                attributeDifference.text = Mathf.Abs(equippedValue - attributeValue).ToString();
                attributeDifference.color = isUpgrade ? upgradeColor : downgradeColor;

            }
            else
            {
                attributeChild.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateEnchantments(Item item)
    {
        Weapon weapon = item as Weapon;
        Armour armour = item as Armour;

        int numOfSlots;

        if (weapon)
        {
            numOfSlots = weapon.numOfEnchantmentSlots;
        }
        else
        {
            numOfSlots = armour.numOfEnchantmentSlots;
        }

        if (numOfSlots == 0)
        {
            enchantmentHeader.SetActive(false);
            foreach (GameObject effect in enchantmentEffectsPool)
            {
                effect.SetActive(false);
            }

            return;
        }

        int enchantmentCount;
        List<Enchantment> enchantments;

        if (weapon)
        {
            enchantmentCount = weapon.infusedEnchantments.Count + weapon.embeddedEnchantments.Count;
            enchantments = weapon.infusedEnchantments.Concat(weapon.embeddedEnchantments).ToList();
        }
        else
        {
            enchantmentCount = armour.infusedEnchantments.Count + armour.embeddedEnchantments.Count;
            enchantments = armour.infusedEnchantments.Concat(armour.embeddedEnchantments).ToList();
        }

        enchantmentHeader.SetActive(true);

        for (int i  = 0; i < enchantmentEffectsPool.Count; i++)
        {
            GameObject effect = enchantmentEffectsPool[i];
            TextMeshProUGUI effectText = effect.transform.GetChild(effect.transform.childCount - 1).GetComponent<TextMeshProUGUI>();

            if (i >= numOfSlots)
            {
                effect.SetActive(false);
                continue;
            }

            effect.SetActive(true);

            foreach (Transform slot in effect.transform)
            {
                //Empty 0 //Infused 1 //Fill 2
                int childIndex = 0;

                if (i < enchantmentCount)
                {
                   childIndex = enchantments[i] is InfusedEnchantment ? 1 : 2;
                }

                slot.gameObject.SetActive(slot.GetSiblingIndex() == childIndex || slot.GetSiblingIndex() == effect.transform.childCount - 1);
            }

            string effectDesc = i >= enchantmentCount ? "Empty" : enchantments[i].description;

            effectText.text = effectDesc;
            effectText.color = i >= enchantmentCount ? Color.white : enchantmentTextColor;
        }
    }

    private string GetAttributePrepend(Attribute attribute)
    {
        switch (attribute)
        {
            case Attribute.Strength:
                return "Str: ";
            case Attribute.Finesse:
                return "Fin: ";
            case Attribute.Endurance:
                return "End: ";
            case Attribute.Agility:
                return "Ag: ";
            case Attribute.Intelligence:
                return "Int: ";
            case Attribute.Wisdom:
                return "Wis: ";
            default:
                return "Chr: ";
        }
    }


}
