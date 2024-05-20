using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class HealthUIData
{
    [Header("Potrait")]
    public Image potrait;
    [Header("Health")]
    public TextMeshProUGUI HPTitle;
    public Image HPBar;
    public TextMeshProUGUI HPValue;
    [Header("Stamina")]
    public TextMeshProUGUI SPTitle;
    public Image SPBar;
    public TextMeshProUGUI SPValue;
    [Header("Status Effect")]
    public Transform statusEffectsHeader;
    [Header("Fired Up!")]
    public Image firedUpMeter;
    public GameObject firedUpFilledCircle;
    public GameObject firedUpSun;
}

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("HUDs")]
    [SerializeField] FantasyRoamHUD fantasyRoamHud;
    [SerializeField] FantasyCombatHUD fantasyCombatHud;
    [Header("Colors")]
    public Color HPColor;
    public Color SPColor;
    [Space(10)]
    public Color damageColour; //FF4C44
    public Color healColour; //A6F57C
    [Space(10)]
    public Color SPLossColor;
    [Header("General Animations")]
    public float healthBarAnimationTime = 0.75f;
    [Header("Actors Setup")]
    [SerializeField] Transform actorHeader;
    [Space(5)]
    [SerializeField] List<Transform> actorSpawnWeaponTransforms;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        FantasyCombatManager.Instance.CombatEnded += OnCombatEnd;
        SavingLoadingManager.Instance.DataAndSceneLoadComplete += RoamHUDSetup;
    }

    private void Start()
    {
        RoamHUDSetup();
        fantasyRoamHud.gameObject.SetActive(!FantasyCombatManager.Instance.InCombat());
    }

    private void RoamHUDSetup()
    {
        fantasyRoamHud.SetPartyHealthData(PartyData.Instance.GetActivePlayerParty());
    }

    //Actors

    public void ActivateUIPhotoshoot(bool activate)
    {
        FantasyCombatManager.Instance.ActivatePhotoshootSet(activate);
        actorHeader.gameObject.SetActive(activate);
    }

    public void UpdateModel(PlayerGridUnit selectedUnit)
    {
        foreach (Transform child in actorHeader)
        {
            child.gameObject.SetActive(child.name.ToLower() == selectedUnit.unitName.ToLower());
            if (!child.gameObject.activeSelf) { continue; }

            int index = child.GetSiblingIndex();

            Animator animator = child.GetComponent<Animator>();
            int layerIndex = animator.GetLayerIndex(selectedUnit.unitName);

            animator.SetLayerWeight(layerIndex, 1);

            Transform spawnTransform = actorSpawnWeaponTransforms[index];
            selectedUnit.stats.SpawnWeaponInModel(spawnTransform);
        }
    }




    //Combat
    public void OnCombatBegin(List<PlayerGridUnit> playerParty)
    {
        fantasyRoamHud.gameObject.SetActive(false);
        fantasyCombatHud.SetPartyHealthData(playerParty);
    }

    private void OnCombatEnd(BattleResult battleResult, IBattleTrigger battleTrigger)
    {
        if (battleResult == BattleResult.Victory || battleResult == BattleResult.Fled)
        {
            fantasyRoamHud.SetPartyHealthData(PartyData.Instance.GetActivePlayerParty());
        }
    }

    public void HideHUDs()
    {
        fantasyRoamHud.gameObject.SetActive(false);
        fantasyCombatHud.gameObject.SetActive(false);
    }

    public void ShowActiveHud()
    {
        if (CinematicManager.Instance.isCinematicPlaying) { return; }

        bool inCombat = FantasyCombatManager.Instance.InCombat();

        fantasyCombatHud.gameObject.SetActive(inCombat);
        fantasyRoamHud.gameObject.SetActive(!inCombat);
    }

    public void EnableRoamHUD() //Called by ControlsManager. Because anytime we switch to Player controls, we want HUD.
    {
        fantasyRoamHud.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        FantasyCombatManager.Instance.CombatEnded -= OnCombatEnd;
        SavingLoadingManager.Instance.DataAndSceneLoadComplete -= RoamHUDSetup;
    }

    //Quests
    public void UpdateActiveQuest(List<Objective> objectives)
    {
        fantasyRoamHud.UpdateObjectives(objectives);
    }

    //Fantasy Combat Hud Exclusive
    public void UpdateSelectedSkill(string skillName)
    {
        fantasyCombatHud.SelectedSkill(skillName);
    }

    public void UpdateTurnOrderNames(List<GridUnit> selectedUnits)
    {
        fantasyCombatHud.UpdateTurnOrderNames(selectedUnits);
    }

    public void UpdateChargeNotification(List<string> chargedItemNames)
    {
        fantasyCombatHud.DisplayChargedNotification(chargedItemNames);
    }

    public void UpdateBlessing(Blessing blessing, int turnsRemaining)
    {
        fantasyCombatHud.UpdateBlessing(blessing, turnsRemaining);
    }

    //Update Health
    public void UpdateUnitHealth(PlayerGridUnit unit)
    {
        BaseHUD hudToCall = FantasyCombatManager.Instance.InCombat() ? fantasyCombatHud : fantasyRoamHud;
        hudToCall.UpdateUnitHealth(unit);
    }

    public void UpdateUnitSP(PlayerGridUnit unit)
    {
        BaseHUD hudToCall = FantasyCombatManager.Instance.InCombat() ? fantasyCombatHud : fantasyRoamHud;
        hudToCall.UpdateUnitSP(unit);
    }

    public void UpdateUnitFP(PlayerGridUnit unit)
    {
        BaseHUD hudToCall = FantasyCombatManager.Instance.InCombat() ? fantasyCombatHud : fantasyRoamHud;
        hudToCall.UpdateUnitFP(unit);
    }

    public void ShowFiredUpSun(PlayerGridUnit unit, bool show)
    {
        BaseHUD hudToCall = FantasyCombatManager.Instance.InCombat() ? fantasyCombatHud : fantasyRoamHud;
        hudToCall.ShowFiredUpSun(unit, show);
    }

    public List<GameObject> SpawnStatusEffectUI(StatusEffectData effectData, PlayerGridUnit affectedUnit)
    {
        List<GameObject> prefabList = new List<GameObject>();
        bool addToCombatHud = true;

        if (effectData.canBeAppliedOutsideCombat)
        {
            //Ensure Affected Unit on Player Party Before Adding
            if (PartyData.Instance.GetActivePlayerParty().Contains(affectedUnit))
            {
                GameObject roamHudPrefab = Instantiate(effectData.effectVisualPrefab, fantasyRoamHud.GetPlayerStatusEffectHeader(affectedUnit));
                prefabList.Add(roamHudPrefab);
            }
            else
            {
                addToCombatHud = false;
            }
        }

        if (addToCombatHud)
        {
            GameObject combatHudPrefab = Instantiate(effectData.effectVisualPrefab, fantasyCombatHud.GetPlayerStatusEffectHeader(affectedUnit));
            prefabList.Add(combatHudPrefab);
        }

        return prefabList;
    }

    public bool IsHUDEnabled()
    {
        return fantasyCombatHud.gameObject.activeInHierarchy || fantasyRoamHud.gameObject.activeInHierarchy;
    }


}
