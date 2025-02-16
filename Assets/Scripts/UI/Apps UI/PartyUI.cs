using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using AnotherRealm;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class PartyUI : MonoBehaviour, IControls
{
    [Header("Attribute Help")]
    [SerializeField] FadeUI attributeHelpArea;
    [Header("Values")]
    [SerializeField] float defaultPotraitSize = 70;
    [SerializeField] float selectedPotraitSize = 100;
    [Space(5)]
    [SerializeField] float animTime = 0.25f;
    [Header("Selected Area")]
    [SerializeField] TextMeshProUGUI selectedName;
    [Space(5)]
    [SerializeField] List<Image> potraits;
    [Header("Title Section")]
    [SerializeField] TextMeshProUGUI level;
    [SerializeField] TextMeshProUGUI race;
    [SerializeField] TextMeshProUGUI type;
    [Header("Vitals")]
    [SerializeField] RectTransform vitalsListHeader;
    [SerializeField] Image hpBar;
    [SerializeField] TextMeshProUGUI hpText;
    [Space(5)]
    [SerializeField] Image spBar;
    [SerializeField] TextMeshProUGUI spText;
    [Space(5)]
    [SerializeField] Image fpBar;
    [SerializeField] TextMeshProUGUI fpText;
    [Header("Experience")]
    [SerializeField] Image xpBar;
    [SerializeField] TextMeshProUGUI currentXPText;
    [SerializeField] TextMeshProUGUI nextXPText;
    [Header("Attributes")]
    [SerializeField] GameObject attributeSubtitle;
    [Space(5)]
    [SerializeField] TextMeshProUGUI strengthValue;
    [SerializeField] TextMeshProUGUI physValue;
    [SerializeField] TextMeshProUGUI weightValue;
    [Space(5)]
    [SerializeField] TextMeshProUGUI finesseValue;
    [SerializeField] TextMeshProUGUI techniqueValue;
    [SerializeField] TextMeshProUGUI evasionValue;
    [Space(5)]
    [SerializeField] TextMeshProUGUI enduranceValue;
    [SerializeField] TextMeshProUGUI vitalityValue;
    [SerializeField] TextMeshProUGUI staminaValue;
    [Space(5)]
    [SerializeField] TextMeshProUGUI agilityValue;
    [SerializeField] TextMeshProUGUI speedValue;
    [SerializeField] TextMeshProUGUI moveValue;
    [Space(5)]
    [SerializeField] TextMeshProUGUI intelligenceValue;
    [SerializeField] TextMeshProUGUI magValue;
    [SerializeField] TextMeshProUGUI memoryValue;
    [Space(5)]
    [SerializeField] TextMeshProUGUI wisdomValue;
    [SerializeField] TextMeshProUGUI healValue;
    [SerializeField] TextMeshProUGUI SeDurationValue;
    [SerializeField] TextMeshProUGUI scrollDurationValue;
    [Space(5)]
    [SerializeField] TextMeshProUGUI charismaValue;
    [SerializeField] TextMeshProUGUI critValue;
    [SerializeField] TextMeshProUGUI SeInflictValue;
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
    [Header("Controls")]
    [SerializeField] List<Transform> controlHeader;

    int currentSeletedUnit = 0;
    bool attributeHelpActive = false;

    private void Awake()
    {
        foreach(Transform header in controlHeader)
        {
            ControlsManager.Instance.AddControlHeader(header);
        }

        ControlsManager.Instance.SubscribeToPlayerInput("Menu", this);
    }
    public void ActivatePartyUI(bool activate)
    {
        if (activate)
        {
            AudioManager.Instance.PlaySFX(SFXType.OpenCombatMenu);

            attributeHelpActive = false;
            attributeHelpArea.Fade(false);

            currentSeletedUnit = 0;
            BuildPotraits();
            UpdateSelectedUnit(0);
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.TabBack);
        }

        HUDManager.Instance.ActivateUIPhotoshoot(activate);

        PhoneMenu.Instance.OpenApp(activate);
        gameObject.SetActive(activate);

        if(activate)
            ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    private void UpdateSelectedUnit(int indexChange)
    {
        if(indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.TabForward);

        CombatFunctions.UpdateListIndex(indexChange, currentSeletedUnit, out currentSeletedUnit, PartyManager.Instance.GetAllPlayerMembersInWorld().Count);
        BuildUI();
    }

    private void BuildUI()
    {
        //PlayerUnitStats selectedPlayerStats = PartyData.Instance.GetAllPartyMemberStats()[currentSeletedUnit];
        PlayerGridUnit selectedPlayer = PartyManager.Instance.GetAllPlayerMembersInWorld()[currentSeletedUnit];
        selectedName.text = selectedPlayer.unitName;

        UpdateSelectedPotrait();
        HUDManager.Instance.UpdateModel(selectedPlayer);

        //Set Title Data
        int currentLevel = selectedPlayer.stats.level;

        level.text = currentLevel.ToString();
        race.text = selectedPlayer.stats.data.race.ToString();
        type.text = selectedPlayer.stats.data.raceType.ToString();

        //Set Vitals
        hpText.text = selectedPlayer.Health().currentHealth + "/" + selectedPlayer.stats.Vitality;
        hpBar.fillAmount = selectedPlayer.Health().GetHealthNormalized();

        spText.text = selectedPlayer.Health().currentSP + "/" + selectedPlayer.stats.Stamina;
        spBar.fillAmount = selectedPlayer.Health().GetStaminaNormalized();

        fpText.text = selectedPlayer.Health().currentFP + "/" + selectedPlayer.Health().MaxFP();
        fpBar.fillAmount = selectedPlayer.Health().GetFPNormalized();

        //Set Experience
        int nextLevelBenchmark = ProgressionManager.Instance.GetNextLevelBenchmark(currentLevel);
        int currentXP = ProgressionManager.Instance.GetCurrentXP(selectedPlayer.unitName);

        currentXPText.text = currentXP.ToString();
        nextXPText.text = (nextLevelBenchmark - currentXP).ToString();
        xpBar.fillAmount = ((float)currentXP / nextLevelBenchmark);

        UpdateAttribute(selectedPlayer);
        UpdateAffinities(selectedPlayer);

        //LayoutRebuilder.ForceRebuildLayoutImmediate(vitalsListHeader);
    }

    private void UpdateAffinities(PlayerGridUnit selectedPlayer)
    {
        BeingData data = selectedPlayer.stats.data;

        ResetAllAffinityUI();

        //Update Element Affinities
        foreach (ElementAffinity affinity in data.elementAffinities)
        {
            switch (affinity.element)
            {
                case Element.Silver:
                    UpdateAffinity(silverAffinity, affinity.affinity);
                    break;
                case Element.Gold:
                    UpdateAffinity(goldAffinity, affinity.affinity);
                    break;
                case Element.Steel:
                    UpdateAffinity(ironAffinity, affinity.affinity);
                    break;
                case Element.Fire:
                    UpdateAffinity(fireAffinity, affinity.affinity);
                    break;
                case Element.Ice:
                    UpdateAffinity(iceAffinity, affinity.affinity);
                    break;
                case Element.Air:
                    UpdateAffinity(airAffinity, affinity.affinity);
                    break;
                case Element.Earth:
                    UpdateAffinity(earthAffinity, affinity.affinity);
                    break;
                case Element.Holy:
                    UpdateAffinity(holyAffinity, affinity.affinity);
                    break;
                case Element.Curse:
                    UpdateAffinity(curseAffinity, affinity.affinity);
                    break;
            }
        }
    }

    private void ResetAllAffinityUI()
    {
        ResetAffinityUI(silverAffinity);
        ResetAffinityUI(goldAffinity);
        ResetAffinityUI(ironAffinity);

        ResetAffinityUI(fireAffinity);
        ResetAffinityUI(iceAffinity);
        ResetAffinityUI(airAffinity);
        ResetAffinityUI(earthAffinity);
        ResetAffinityUI(holyAffinity);
        ResetAffinityUI(curseAffinity);
    }

    private void ResetAffinityUI(Transform affinityHeader)
    {
        foreach (Transform child in affinityHeader)
        {
            child.gameObject.SetActive(false);
        }
    }

    private void UpdateAffinity(Transform affinityHeader, Affinity affinity)
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
                childIndex = reflectIndex;
                break;
            case Affinity.Reflect:
                childIndex = reflectIndex;
                break;
            case Affinity.Weak:
                childIndex = weakIndex;
                break;
        }

        foreach (Transform child in affinityHeader)
        {
            child.gameObject.SetActive(child.GetSiblingIndex() == childIndex);
        }
    }

    private void UpdateAttribute(PlayerGridUnit selectedPlayer)
    {
        attributeSubtitle.SetActive(false);

        string prepend = "<color=white>";
        string append = "</color>";

        //Strength
        strengthValue.text = prepend + selectedPlayer.stats.Strength.ToString() + append;
        physValue.text = prepend + selectedPlayer.stats.PhysAttack.ToString() + append;
        weightValue.text = prepend + selectedPlayer.stats.InventoryWeight.ToString() + append;

        //Finesse
        finesseValue.text = prepend + selectedPlayer.stats.Finesse.ToString() + append;
        techniqueValue.text = prepend + selectedPlayer.stats.Technique.ToString() + append;
        evasionValue.text = prepend + selectedPlayer.stats.Evasion.ToString() + append;

        //Endurance
        enduranceValue.text = prepend + selectedPlayer.stats.Endurance.ToString() + append;
        vitalityValue.text = prepend + selectedPlayer.stats.Vitality.ToString() + append;
        staminaValue.text = prepend + selectedPlayer.stats.Stamina.ToString() + append;

        //Agility
        agilityValue.text = prepend + selectedPlayer.stats.Agility.ToString() + append;
        speedValue.text = prepend + selectedPlayer.stats.Speed.ToString() + append;
        moveValue.text = prepend + selectedPlayer.MoveRange().ToString() + append;

        //Intelligence
        intelligenceValue.text = prepend + selectedPlayer.stats.Intelligence.ToString() + append;
        magValue.text = prepend + selectedPlayer.stats.MagAttack.ToString() + append;
        memoryValue.text = prepend + selectedPlayer.stats.Memory.ToString() + append;

        //Wisdom
        wisdomValue.text = prepend + selectedPlayer.stats.Wisdom.ToString() + append;
        healValue.text = prepend + selectedPlayer.stats.HealEfficacy.ToString() + append;
        SeDurationValue.text = prepend + selectedPlayer.stats.SEDuration.ToString() + append;
        //scrollDurationValue.text = prepend + selectedPlayer.stats.ScrollDuration.ToString() + append;

        //Charisma
        charismaValue.text = prepend + selectedPlayer.stats.Charisma.ToString() + append;
        critValue.text = prepend + selectedPlayer.stats.CritChance.ToString() + append;
        SeInflictValue.text = prepend + selectedPlayer.stats.StatusEffectInflictChance.ToString() + append;
    }

    private void UpdateSelectedPotrait()
    {
        foreach (Image potrait in potraits)
        {
            if (!potrait.gameObject.activeSelf) { continue; }

            float size = potrait.transform.GetSiblingIndex() == currentSeletedUnit ? selectedPotraitSize : defaultPotraitSize;
            potrait.rectTransform.DOSizeDelta(new Vector2(size, size), animTime);
        }
    }

    private void BuildPotraits()
    {
        foreach(Image potrait in potraits)
        {
            int index = potrait.transform.GetSiblingIndex();
            potrait.gameObject.SetActive(index < PartyManager.Instance.GetAllPlayerMembersInWorld().Count);

            if (!potrait.gameObject.activeSelf) { continue; }

            PlayerGridUnit selectedPlayer = PartyManager.Instance.GetAllPlayerMembersInWorld()[index];
            potrait.sprite = selectedPlayer.portrait;
        }
    }


    private void OnCycle(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "CycleR")
            {
                UpdateSelectedUnit(1);
            }
            else if (context.action.name == "CycleL")
            {
                UpdateSelectedUnit(-1);
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            if (attributeHelpActive)
            {
                AudioManager.Instance.PlaySFX(SFXType.TabBack);

                attributeHelpActive = false;
                attributeHelpArea.Fade(attributeHelpActive);
            }
            else
            {
                ActivatePartyUI(false);
            }
        }
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            attributeHelpActive = !attributeHelpActive;
            attributeHelpArea.Fade(attributeHelpActive);

            if (attributeHelpActive)
            {
                AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
            }
            else
            {
                AudioManager.Instance.PlaySFX(SFXType.TabBack);
            }
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
}
