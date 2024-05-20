using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Equipment : MonoBehaviour
{
    [Header("Equipment")]
    [SerializeField] Weapon equippedWeapon;
    [SerializeField] Armour equippedArmour;
    [Space(10)]
    [SerializeField] bool spawnEquippedWeaponModel = false;
    [Space(10)]
    [SerializeField] Transform enchantmentHeader;
    [Header("Transforms")]
    [SerializeField] Transform weaponHeaderTransform;
    [Tooltip("Things Such as Quivers, Dual wielding weapons, etc")]
    [SerializeField] List<Transform> otherEquipmentHeaders;
    [Header("TEST")]
    [SerializeField] Enchantment equippedEnchantment;

    List<Enchantment> equippedWeaponEnchantments = new List<Enchantment>();
    List<Enchantment> equippedArmourEnchantments = new List<Enchantment>();

    CharacterGridUnit myWearer;

    private void Awake()
    {
        myWearer = GetComponentInParent<CharacterGridUnit>();

        if (FantasyCombatManager.Instance && FantasyCombatManager.Instance.CombatCinematicPlaying) { return; }

        SetEnchantments();

      /*  if (equippedEnchantment)
            InventoryManager.Instance.TryEnchantWeapon(myWearer as PlayerGridUnit, myWearer as PlayerGridUnit, equippedWeapon, equippedEnchantment);*/
    }

    private void OnEnable()
    {
        if (FantasyCombatManager.Instance && FantasyCombatManager.Instance.CombatCinematicPlaying) { return; }

        FantasyCombatManager.Instance.CombatBegun += SpawnEnchantments;
        FantasyCombatManager.Instance.CombatEnded += DestroyEnchantments;
    }

    private void Start()
    {
        if (FantasyCombatManager.Instance && FantasyCombatManager.Instance.CombatCinematicPlaying) { return; }

        SpawnWeaponModel();
    }

    public void SpawnEnchantments(BattleStarter.CombatAdvantage advantageType)
    {
        if (!FantasyCombatManager.Instance.IsUnitInBattle(myWearer)) { return; }

        //Only Spawn if in the battle.

        //Spawn Enchantment Passives
        List<Enchantment> enchantments = equippedWeaponEnchantments.Concat(equippedArmourEnchantments).ToList();
        List<EnchantmentPassiveData> enchantmentsToSpawn = new List<EnchantmentPassiveData>();

        //Total Data
        foreach(Enchantment enchantment in enchantments)
        {
            foreach (EnchantmentPassiveData passiveData in enchantment.passiveAbilities)
            {
                EnchantmentPassiveData spawnedPassiveData = enchantmentsToSpawn.FirstOrDefault((item) => item.passiveAbility.name == passiveData.passiveAbility.name);

                if (spawnedPassiveData != null)
                {
                    //Means another Copy of Ability, So Stack
                    spawnedPassiveData.passivePercentageValue = Mathf.Min(spawnedPassiveData.passivePercentageValue + passiveData.passivePercentageValue, 100);
                    spawnedPassiveData.passiveNumberValue = spawnedPassiveData.passiveNumberValue + passiveData.passiveNumberValue;
                }
                else
                {
                    EnchantmentPassiveData newData = new EnchantmentPassiveData();
                    newData.passiveAbility = passiveData.passiveAbility;
                    newData.passivePercentageValue = passiveData.passivePercentageValue;
                    newData.passiveNumberValue = passiveData.passiveNumberValue;

                    enchantmentsToSpawn.Add(newData);
                }
            }
        }

        //Spawn 
        foreach (EnchantmentPassiveData passiveData in enchantmentsToSpawn)
        {
            GameObject abilityGO = Instantiate(passiveData.passiveAbility, enchantmentHeader);
            abilityGO.GetComponent<EnchantmentEffect>().Setup(myWearer, passiveData.passivePercentageValue, passiveData.passiveNumberValue);
        }
    }

    private void DestroyEnchantments(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        //Destroy Enchantment passives
        foreach(Transform child in enchantmentHeader)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void OnDisable()
    {
        if (FantasyCombatManager.Instance && FantasyCombatManager.Instance.CombatCinematicPlaying) { return; }

        FantasyCombatManager.Instance.CombatBegun -= SpawnEnchantments;
        FantasyCombatManager.Instance.CombatEnded -= DestroyEnchantments;
    }

    //Equip Options
    public void ChangeWeapon(Weapon weapon)
    {
        equippedWeapon = weapon;

        SpawnWeaponModel();
        UpdateEnchantments();
        
    }

    public void ChangeArmour(Armour armour)
    {
        equippedArmour = armour;

        UpdateEnchantments();
    }

    private void UpdateEnchantments()
    {
        SetEnchantments();

        if (FantasyCombatManager.Instance.InCombat())
        {
            DestroyEnchantments(BattleResult.Defeat, null);
            SpawnEnchantments(BattleStarter.CombatAdvantage.Neutral);
        }
    }

    //SETTERS
    private void SpawnWeaponModel()
    {
        if (!equippedWeapon || !spawnEquippedWeaponModel) { return; }

        //Clean Header First
        foreach (Transform child in weaponHeaderTransform)
        {
            Destroy(child.gameObject);
        }

        //Instantiate
        GameObject weapon = Instantiate(equippedWeapon.modelPrefab, weaponHeaderTransform);

        //Set Pos & Rot
        weapon.transform.localPosition = weapon.transform.GetChild(1).localPosition;
        weapon.transform.localRotation = weapon.transform.GetChild(1).localRotation;

        weapon.SetActive(FantasyCombatManager.Instance.InCombat());
    }

    public void SpawnWeaponModel(Transform spawnTransform)
    {
        //Clean Header First
        foreach (Transform child in spawnTransform)
        {
            if (child.gameObject.name == equippedWeapon.name)
            {
                return;
            }
            else
            {
                Destroy(child.gameObject);
            }
        }

        //Instantiate
        GameObject weapon = Instantiate(equippedWeapon.modelPrefab, spawnTransform);

        //Set Name
        weapon.name = equippedWeapon.name;
        weapon.layer = spawnTransform.gameObject.layer;

        foreach (Transform child in weapon.transform)
        {
            child.gameObject.layer = spawnTransform.gameObject.layer;
        }

        //Set Pos & Rot
        weapon.transform.localPosition = weapon.transform.GetChild(1).localPosition;
        weapon.transform.localRotation = weapon.transform.GetChild(1).localRotation;
    }

    public void SetEnchantments()
    {
        equippedWeaponEnchantments.Clear();
        equippedArmourEnchantments.Clear();

        if (equippedWeapon)
        {
            foreach (Enchantment enchantment in equippedWeapon.infusedEnchantments.Concat(equippedWeapon.embeddedEnchantments))
            {
                equippedWeaponEnchantments.Add(enchantment);
            }
        }

        if (equippedArmour)
        {
            foreach (Enchantment enchantment in equippedArmour.infusedEnchantments.Concat(equippedArmour.embeddedEnchantments))
            {
                equippedArmourEnchantments.Add(enchantment);
            }
        }
    }

    //Get Equipment Attributes

    public int GetEquipmentAttributeBonus(Attribute attribute)
    {
        List<AttributeBonus> equipmentBonuses = new List<AttributeBonus>();

        if (equippedWeapon)
        {
            equipmentBonuses = equipmentBonuses.Concat(equippedWeapon.bonusAttributes).ToList();
        }

        if (equippedArmour)
        {
            equipmentBonuses = equipmentBonuses.Concat(equippedArmour.bonusAttributes).ToList();
        }

        int total = 0;

        foreach (AttributeBonus bonus in equipmentBonuses)
        {
            if (bonus.attribute == attribute)
            {
                total = total + bonus.attributeChange;
            }
        }

        return total;
    }

    public int GetEquipmentSubAttributeBonus(SubStats subAttribute)
    {
        List<SubStatBonus> equipmentBonuses = new List<SubStatBonus>();

        if (equippedWeapon)
        {
            foreach (InfusedEnchantment infused in equippedWeapon.infusedEnchantments)
            {
                equipmentBonuses = equipmentBonuses.Concat(infused.bonusSubAttributes).ToList();
            }
        }

        if (equippedArmour)
        {
            foreach (InfusedEnchantment infused in equippedArmour.infusedEnchantments)
            {
                equipmentBonuses = equipmentBonuses.Concat(infused.bonusSubAttributes).ToList();
            }
        }

        int total = 0;

        foreach (SubStatBonus bonus in equipmentBonuses)
        {
            if (bonus.subStat == subAttribute)
            {
                total = total + bonus.subStatChange;
            }
        }

        return total;
    }

    //Get Equipment Affinity Alteration
    public List<MaterialAffinity> GetMaterialAlteration()
    {
        List<MaterialAffinity> equipmentAlterations = new List<MaterialAffinity>();

        if (equippedArmour)
        {
            foreach(InfusedEnchantment infusedEnchantment in equippedArmour.infusedEnchantments)
            {
                equipmentAlterations = equipmentAlterations.Concat(infusedEnchantment.materialAlteration).ToList();
            }
        }

        return equipmentAlterations;
    }

    public List<ElementAffinity> GetElementAlteration()
    {
        List<ElementAffinity> equipmentAlterations = new List<ElementAffinity>();

        if (equippedArmour)
        {
            foreach (InfusedEnchantment infusedEnchantment in equippedArmour.infusedEnchantments)
            {
                equipmentAlterations = equipmentAlterations.Concat(infusedEnchantment.elementAlteration).ToList();
            }
        }

        return equipmentAlterations;
    }

    public List<StatusEffectData> GetStatusEffectImmunity()
    {
        List<StatusEffectData> nulledEffects = new List<StatusEffectData>();

        foreach (Enchantment enchantment in equippedArmourEnchantments.Concat(equippedWeaponEnchantments))
        {
            nulledEffects = nulledEffects.Concat(enchantment.statusEffectsNulled).ToList();
        }

        return nulledEffects;
    }

    public List<ChanceOfInflictingStatusEffect> GetStatusEffectsToInflict()
    {
        List<ChanceOfInflictingStatusEffect> inflictedStatusEffects = new List<ChanceOfInflictingStatusEffect>();

        foreach (Enchantment enchantment in equippedArmourEnchantments.Concat(equippedWeaponEnchantments))
        {
            inflictedStatusEffects = inflictedStatusEffects.Concat(enchantment.inflictedStatusEffects).ToList();
        }

        return inflictedStatusEffects;
    }

    public bool HasKnockbackImmunity()
    {
        foreach (Enchantment enchantment in equippedArmourEnchantments.Concat(equippedWeaponEnchantments))
        {
            if (enchantment.knockbackImmunity)
                return true;
        }

        return false;
    }

    public void AdjustWearerHealth()
    {
        if(myWearer)
            myWearer.Health().AdjustCurrentVitals();
    }

    //Getters


    public bool IsEquipped(Item item)
    {
        Weapon weapon = item as Weapon;
        Armour armour = item as Armour;
        Enchantment enchantment = item as Enchantment;

        if (weapon)
            return IsEquipped(weapon);
        if (armour)
            return IsEquipped(armour);
        if (enchantment)
            return IsEquipped(enchantment);

        return false;
    }

    /*public int EquippedEnchantmentCount(Enchantment enchantment)
    {
        if (!IsEquipped(enchantment)) { return 0; }

        return equippedArmourEnchantments.Where((item) => item == enchantment).Count() + equippedWeaponEnchantments.Where((item) => item == enchantment).Count();
    }*/

    private bool IsEquipped(Weapon weapon)
    {
        return weapon && equippedWeapon == weapon;
    }

    private bool IsEquipped(Armour armour)
    {
        return armour && equippedArmour == armour;
    }

    private bool IsEquipped(Enchantment enchantment)
    {
        return enchantment && (equippedArmourEnchantments.Contains(enchantment) || equippedWeaponEnchantments.Contains(enchantment));
    }

    public Weapon Weapon()
    {
        return equippedWeapon;
    }

    public Armour Armour()
    {
        return equippedArmour;
    }

    //Models
    public List<Transform> GetEquipmentHeaders()
    {
        List<Transform> headers = new List<Transform>();

        headers.Add(weaponHeaderTransform);

        foreach(Transform header in otherEquipmentHeaders)
        {
            headers.Add(header);
        }

        return headers;
    }

    public Transform GetMainWeaponHeader()
    {
        return weaponHeaderTransform;
    }
}
