using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI powerText;
    [Header("Health")]
    [SerializeField] Color healthyColor;
    [SerializeField] Color damagedColor;
    [Space(5)]
    [SerializeField] Transform healthBarsHeader;


    public void SetBaseCardData(Card card)
    {
        SetCardName(card.itemName);
        SetPower(card.basePower);
        SetMaxHealth(card.baseHealth);
        SetHealth(card.baseHealth);
    }


    public void SetCardName(string cardName)
    {
        nameText.text = cardName;
    }

    public void SetPower(int power)
    {
        powerText.text = power.ToString();
    }

    public void SetHealth(int health)
    {
        foreach(Transform bar in healthBarsHeader)
        {
            int barIndex = bar.GetSiblingIndex();

            if (bar.gameObject.activeInHierarchy)
            {
                bar.GetComponent<Image>().color = barIndex < health ? healthyColor : damagedColor;
            }
        }
    }

    public void SetMaxHealth(int maxHealth)
    {
        foreach (Transform bar in healthBarsHeader)
        {
            int barIndex = bar.GetSiblingIndex();

            bar.gameObject.SetActive(barIndex < maxHealth);
        }
    }
}
