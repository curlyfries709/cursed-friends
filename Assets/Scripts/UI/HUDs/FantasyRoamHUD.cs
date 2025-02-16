using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FantasyRoamHUD : BaseHUD
{
    [Header("Quest Area")]
    [SerializeField] TextMeshProUGUI questTitle;
    [SerializeField] Transform objectiveHeader;
    [Space(10)]
    [SerializeField] int objectiveTextIndex = 1;
    [Header("Tool Area")]
    [SerializeField] GameObject toolArea;
    [Space(10)]
    [SerializeField] Image toolIcon;
    [SerializeField] TextMeshProUGUI toolName;
    [SerializeField] TextMeshProUGUI toolCount;

    private void OnEnable()
    {
        SetPartyHealthData(PartyManager.Instance.GetActivePlayerParty());    
    }

    public void UpdateToolArea(Tool tool, int count)
    {
        toolArea.SetActive(tool);
        if (!tool) { return; }

        toolIcon.sprite = tool.UIIcon;
        toolName.text = tool.itemName;
        toolCount.text = count.ToString();
    }

    public void UpdateObjectives(List<Objective> objectives)
    {
        bool activateQuestTitle = objectives.Count > 0;
        questTitle.transform.parent.gameObject.SetActive(activateQuestTitle);

        if (activateQuestTitle)
            questTitle.text = objectives[0].quest.title;

        foreach(Transform child in objectiveHeader)
        {
            int index = child.GetSiblingIndex();
            bool activate = index < objectives.Count;

            child.gameObject.SetActive(activate);

            if (!activate) { continue; }

            child.GetChild(objectiveTextIndex).GetComponent<TextMeshProUGUI>().text = objectives[index].description;
        }
    }
}
