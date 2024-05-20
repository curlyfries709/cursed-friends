using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class FantasyCombatHUD : BaseHUD
{
    [Header("Headers")]
    [SerializeField] Transform turnOrderHeader;
    [SerializeField] Transform chargeNotificationHeader;
    [Header("Selected Skill")]
    [SerializeField] FadeUI selectedSkillArea;
    [SerializeField] TextMeshProUGUI skillText;
    [Header("Turn Order")]
    [SerializeField] TextMeshProUGUI activeUnitDescription;
    [SerializeField] Sprite allyBorder;
    [SerializeField] Sprite enemyBorder;
    [Header("Blessing")]
    [SerializeField] GameObject blessingArea;
    [Space(5)]
    [SerializeField] TextMeshProUGUI blessingRemainingTurns;
    [SerializeField] TextMeshProUGUI blessingNickname;
    [SerializeField] TextMeshProUGUI blessingEffectDescription;
    [Header("Animations")]
    [SerializeField] float nameBannerAnimationTime = 0.25f;
    [Space(5)]
    [SerializeField] float chargeNotifDisplayTime = 0.5f;

    List<CharacterGridUnit> currentTurnOrderList = new List<CharacterGridUnit>();
    bool chargeNotificationRunning = false;

    private void Awake()
    {
        //Instance = this;
        selectedSkillArea.Fade(false);

        foreach (Transform child in turnOrderHeader)
        {
            if (child.GetSiblingIndex() == 0)
                continue;

            Transform bannerTransform = child.GetChild(2);
            bannerTransform.localScale = new Vector3(0, 1, 1);
            bannerTransform.gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        Flee.UnitFled += OnUnitFledBattle;
    }

    public void SelectedSkill(string skillName)
    {
        if(skillName == "")
        {
            selectedSkillArea.Fade(false);
        }
        else
        {
            skillText.text = skillName;
            selectedSkillArea.Fade(true);
        }
    }

    private void OnUnitFledBattle(CharacterGridUnit unit)
    {
        PlayerGridUnit player = unit as PlayerGridUnit;

        if (!player) { return; }

        foreach (HUDHealthUI healthData in unitHealthUIData)
        {
            if (healthData.GetUnit() == player)
            {
                healthData.gameObject.SetActive(false);
                return;
            }
        }

        playerPartyMembers.Remove(player);
    }


    public void UpdateTurnOrderNames(List<GridUnit> selectedUnits)
    {
        foreach(CharacterGridUnit character in currentTurnOrderList)
        {
            int index = currentTurnOrderList.IndexOf(character);

            if (index == 0) //No Banner for first unit so simply continue.
                continue;

            if (index >= turnOrderHeader.childCount)
                break;

            Transform bannerTransform = turnOrderHeader.GetChild(index).GetChild(2);

            if (!(character is PlayerGridUnit) && selectedUnits.Contains(character))
            {
                bannerTransform.GetChild(0).GetComponent<TextMeshProUGUI>().text = EnemyDatabase.Instance.GetEnemyDisplayName(character, character.stats.data);
                bannerTransform.DOScaleX(1, nameBannerAnimationTime);
            }
            else if(bannerTransform.localScale.x > 0)
            {
                bannerTransform.DOScaleX(0, nameBannerAnimationTime);
            }
        }
    }


    public void UpdateTurnOrder(List<CharacterGridUnit> turnOrderList, Dictionary<CharacterGridUnit, float> unitATs)
    {
        currentTurnOrderList = turnOrderList;

        for (int i = 0; i < turnOrderHeader.childCount; i++)
        {
            if (i >= turnOrderList.Count) 
            {
                turnOrderHeader.GetChild(i).gameObject.SetActive(false);
                continue;
            }

            CharacterGridUnit currentUnit = turnOrderList[i];
            Transform currentChild = turnOrderHeader.GetChild(i);
            currentChild.GetComponent<Image>().sprite = currentUnit.portrait;

            currentChild.gameObject.SetActive(true);

            if (i == 0) 
            {
                string displayName = currentUnit is PlayerGridUnit ? currentUnit.unitName : EnemyDatabase.Instance.GetEnemyDisplayName(currentUnit, currentUnit.stats.data);
                activeUnitDescription.text = displayName + "\n" + "Lvl " + currentUnit.stats.level.ToString() + " " + currentUnit.stats.data.race.ToString();
                continue; 
            }

            Transform borderTransform = currentChild.GetChild(0);
 
            if (currentUnit is PlayerGridUnit)
            {
                borderTransform.GetComponent<Image>().sprite = allyBorder;
            }
            else
            {
                borderTransform.GetComponent<Image>().sprite = enemyBorder;
            }

            if(i > 0 && currentUnit == turnOrderList[0])
            {
                currentChild.GetChild(1).gameObject.SetActive(true);
            }
            else
            {
                currentChild.GetChild(1).gameObject.SetActive(false);
            }
 
        }
    }
    //Blessing
    public void UpdateBlessing(Blessing blessing, int turnsRemaining)
    {
        blessingArea.SetActive(turnsRemaining > 0 && blessing);

        if (turnsRemaining <= 0){ return;}

        blessingRemainingTurns.text = turnsRemaining.ToString();
        blessingNickname.text = blessing.nickname;
        blessingEffectDescription.text = blessing.hudDescription;
    }
    
    //Charge Item
    public void DisplayChargedNotification(List<string> itemNames)
    {
        foreach(string item in itemNames)
        {
            int index = itemNames.IndexOf(item);

            if (index >= chargeNotificationHeader.childCount)
                break;

            Transform chargeTransform = chargeNotificationHeader.GetChild(index);
            chargeTransform.GetChild(0).GetComponent<TextMeshProUGUI>().text = item;

            chargeTransform.GetComponent<FadeUI>().Fade(true);
        }

        if(!chargeNotificationRunning)
            StartCoroutine(ChargeNotificationRoutine());
    }

    IEnumerator ChargeNotificationRoutine()
    {
        chargeNotificationRunning = true;

        yield return new WaitForSeconds(chargeNotifDisplayTime);

        foreach(Transform child in chargeNotificationHeader)
        {
            child.GetComponent<FadeUI>().Fade(false);
        }

        chargeNotificationRunning = false;
    }

    private void OnDisable()
    {
        Flee.UnitFled -= OnUnitFledBattle;

        StopAllCoroutines();
        
        foreach (Transform child in chargeNotificationHeader)
        {
            child.GetComponent<FadeUI>().Fade(false);
        }

        chargeNotificationRunning = false;
    }

}
