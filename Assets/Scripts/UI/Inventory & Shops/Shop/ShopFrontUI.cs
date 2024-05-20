using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using AnotherRealm;

public class ShopFrontUI : BaseShopUI
{
    [Header("Shop Front UI")]
    [SerializeField] Transform shopOptionsHeader;
    [Space(5)]
    [SerializeField] int craftingIndex = 3;
    [Header("Messages")]
    [SerializeField] GameObject welcomeMessage;
    [SerializeField] GameObject craftingMessage;

    int currentMenuOptionIndex = 0;
    int lastVistedMenuOptionIndex = 0;

    bool visitedBuySection = false;

    public void BeginShopping()
    {
        ActivateUI(true);
    }


    public void CraftingSelected()
    {
        welcomeMessage.SetActive(false);
        craftingMessage.SetActive(true);
    }

    private void SelectOption()
    {
        if(currentMenuOptionIndex != craftingIndex)
        {
            ActivateUI(false);
            AudioManager.Instance.PlaySFX(SFXType.CombatMenuSelect);
        }
        else
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
        }
            

        lastVistedMenuOptionIndex = currentMenuOptionIndex;
        shopOptionsHeader.GetChild(currentMenuOptionIndex).GetComponent<Button>().onClick?.Invoke();   
    }

    public void FinishShopping()
    {
        lastVistedMenuOptionIndex = 0;
        ActivateUI(false);
        myShop.ShoppingComplete();
        AudioManager.Instance.PlaySFX(SFXType.TabBack);

    }

    protected override void UpdateAllUI()
    {
        SetCoinCount();
        UpdateSelectedOption(0);
    }

    private void UpdateSelectedOption(int indexChange)
    {
        if (indexChange != 0)
            AudioManager.Instance.PlaySFX(SFXType.ScrollForward);

        //Update Index
        CombatFunctions.UpdateListIndex(indexChange, currentMenuOptionIndex, out currentMenuOptionIndex, shopOptionsHeader.childCount);

        welcomeMessage.SetActive(true);
        craftingMessage.SetActive(false);

        foreach (Transform child in shopOptionsHeader)
        {
            Transform selectedParent = child.GetChild(0);
            bool isSelected = currentMenuOptionIndex == child.GetSiblingIndex();

            selectedParent.GetChild(0).gameObject.SetActive(isSelected);
            selectedParent.GetChild(1).gameObject.SetActive(!isSelected);
        }
    }

    protected override void ResetData()
    {
        currentMenuOptionIndex = lastVistedMenuOptionIndex;
        visitedBuySection = false;
    }

    //INput

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;
                UpdateSelectedOption(indexChange);
            }
        }
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            SelectOption();
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            FinishShopping();
        }
    }



    public override void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
        }
    }


}
