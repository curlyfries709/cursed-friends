using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FantasyRoamHUD : BaseHUD
{
    [Header("Quest Area")]
    [SerializeField] TextMeshProUGUI questTitle;
    [SerializeField] Transform objectiveHeader;
    [Space(10)]
    [SerializeField] int objectiveTextIndex = 1;

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
