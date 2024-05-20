using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryPopupUI : MonoBehaviour
{
    [Header("Count")]
    [SerializeField] TextMeshProUGUI popupTitle;
    [SerializeField] TextMeshProUGUI count;
    [Space(10)]
    [SerializeField] TextMeshProUGUI totalPrice;
    [Header("Inventory")]
    [SerializeField] Image currentInventoryPotrait;
    [SerializeField] Image rightPotrait;
    [SerializeField] Image leftPortait;
    [Space(20)]
    [SerializeField] TextMeshProUGUI currentInventoryName;
    [SerializeField] TextMeshProUGUI currentWeight;
    [SerializeField] TextMeshProUGUI maxWeight;
    [Header("Control Headers")]
    [SerializeField] List<Transform> controlsHeaders = new List<Transform>();

    //Lists
    List<PlayerGridUnit> allPartyMembers = new List<PlayerGridUnit>();

    //Indices
    int selectedInventoryIndex = 0;

    //Caches
    Color overburdenedWeightColor;
    Color defaultWeightColor;

    private void Awake()
    {
        foreach (Transform controlHeader in controlsHeaders)
        {
            ControlsManager.Instance.AddControlHeader(controlHeader);
        }
    }

    public void Setup(List<PlayerGridUnit> allPlayers, string title)
    {
        allPartyMembers = new List<PlayerGridUnit>(allPlayers);
        popupTitle.text = title;

        overburdenedWeightColor = LootUI.Instance.overburdenedWeightColor;
        defaultWeightColor = LootUI.Instance.defaultWeightColor;
    }

    public void UpdateInventoryUI(int currentIndex)
    {
        selectedInventoryIndex = currentIndex;

        currentInventoryName.text = GetSelectedInventory().unitName;
        currentInventoryPotrait.sprite = GetSelectedInventory().portrait;

        rightPotrait.sprite = GetNextPlayerInInventoryList(1).portrait;
        leftPortait.sprite = GetNextPlayerInInventoryList(-1).portrait;

        UpdateInventoryWeight();
    }

    public void UpdateCount(int currentCount, int stock, float totalCost = 0)
    {
        count.text = currentCount.ToString() +  "<color=\"white\">/" + stock.ToString() + " </color>";

        if (totalPrice)
            totalPrice.text = totalCost.ToString();
    }

    private void UpdateInventoryWeight()
    {
        float weight = InventoryManager.Instance.GetCurrentWeight(GetSelectedInventory());
        float capacity = InventoryManager.Instance.GetWeightCapacity(GetSelectedInventory());

        currentWeight.text = weight.ToString();
        maxWeight.text = "/" + capacity.ToString();

        currentWeight.color = weight > capacity ? overburdenedWeightColor : defaultWeightColor;
    }

    //Helper Methods

    private PlayerGridUnit GetNextPlayerInInventoryList(int indexChange)
    {
        int newIndex;

        if (selectedInventoryIndex + indexChange >= allPartyMembers.Count)
        {
            newIndex = 0;
        }
        else if (selectedInventoryIndex + indexChange < 0)
        {
            newIndex = allPartyMembers.Count - 1;
        }
        else
        {
            newIndex = selectedInventoryIndex + indexChange;
        }

        return allPartyMembers[newIndex];
    }

    private PlayerGridUnit GetSelectedInventory()
    {
        return allPartyMembers[selectedInventoryIndex];
    }
}
