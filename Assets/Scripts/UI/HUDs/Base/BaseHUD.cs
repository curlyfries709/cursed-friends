using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseHUD : MonoBehaviour
{
    [Header("HUD Party Health")]
    [SerializeField] Transform healthUIHeader;
    [Space(5)]
    [SerializeField] protected HUDHealthUI[] unitHealthUIData = new HUDHealthUI[4];

    protected List<PlayerGridUnit> playerPartyMembers = new List<PlayerGridUnit>();

    //Health Data
    public void SetPartyHealthData(List<PlayerGridUnit> playerParty)
    {
        playerPartyMembers = playerParty;

        for (int i = 0; i < 4; i++)
        {
            if (i >= playerPartyMembers.Count)
            {
                healthUIHeader.GetChild(i).gameObject.SetActive(false);
                continue;
            }

            unitHealthUIData[i].SetData(playerPartyMembers[i]);
            healthUIHeader.GetChild(i).gameObject.SetActive(true);
        }
    }

    public Transform GetPlayerStatusEffectHeader(PlayerGridUnit unit)
    {
        HUDHealthUI healthUI = unitHealthUIData[playerPartyMembers.IndexOf(unit)];
        return healthUI.GetStatusEffectHeader();
    }

    public void UpdateUnitHealth(PlayerGridUnit unit)
    {
        HUDHealthUI healthUI = unitHealthUIData[playerPartyMembers.IndexOf(unit)];
        healthUI.UpdateHealth(unit);
    }

    public void UpdateUnitSP(PlayerGridUnit unit)
    {
        HUDHealthUI healthUI = unitHealthUIData[playerPartyMembers.IndexOf(unit)];
        healthUI.UpdateSP(unit);
    }

    public void UpdateUnitFP(PlayerGridUnit unit)
    {
        HUDHealthUI healthUI = unitHealthUIData[playerPartyMembers.IndexOf(unit)];
        healthUI.UpdateFP(unit);
    }

    public void ShowFiredUpSun(PlayerGridUnit unit, bool show)
    {
        HUDHealthUI healthUI = unitHealthUIData[playerPartyMembers.IndexOf(unit)];
        healthUI.ShowFiredUpSun(show);
    }
}
