using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;

public class ShopConvertUI : BaseShopUI
{
    [Header("Convert UI")]
    [SerializeField] TextMeshProUGUI poundCountText;
    [SerializeField] TextMeshProUGUI calculatedGoldText;

    int setPoundCount = 0;


    private void OnConfirm()
    {
        if (setPoundCount == 0) 
        {
            AudioManager.Instance.PlaySFX(SFXType.ActionDenied);
            return; 
        }

        //Spend
        InventoryManager.Instance.TrySpendCoin(setPoundCount, false);
        InventoryManager.Instance.EarnCoin(InventoryManager.Instance.ConvertPoundToGold(setPoundCount), true);

        //Play SFX
        AudioManager.Instance.PlaySFX(SFXType.ShopCoin);

        UpdateCoinCount();
        ResetData();
        UpdateConversionData(0);
    }


    protected override void UpdateAllUI()
    {
        SetCoinCount();
        UpdateConversionData(0);
    }

    private void UpdateConversionData(int change)
    {
        int poundChange = change * -1;

        int maxPoundCount = Mathf.FloorToInt(InventoryManager.Instance.modernMoney);

        PlayPopupScrollSFX(poundChange, setPoundCount, 0, maxPoundCount);

        setPoundCount = Mathf.Clamp(setPoundCount + poundChange, 0, maxPoundCount);

        poundCountText.text = "£" + setPoundCount.ToString() + "<color=\"white\">/"+ maxPoundCount +"</color>";
        calculatedGoldText.text = InventoryManager.Instance.ConvertPoundToGold(setPoundCount).ToString();
    }



    IEnumerator UpdateConversionDataContinious(int change)
    {
        while (holdingScroll)
        {
            UpdateConversionData(change);
            yield return new WaitForSeconds(timeBetweenUpdateOnHold);
        }
    }


    //INput

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.action.name == "ScrollU" || context.action.name == "ScrollD")
            {
                int indexChange = context.action.name == "ScrollD" ? 1 : -1;
                UpdateConversionData(indexChange);
            }
        }
    }

    private void OnScrollHold(InputAction.CallbackContext context)
    {
        if(context.action.name == "HoldU" || context.action.name == "HoldD")
        {
            if (context.performed)
            {
                int indexChange = context.action.name == "HoldD" ? 1 : -1;
                BeginHoldScrollRoutine(UpdateConversionData, indexChange);
            }
            else if (context.canceled)
            {
                StopScrollHoldRoutine(); 
            }
        }
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (context.action.name != "Select") { return; }

        if (context.performed)
        {
            OnConfirm();
        }
    }


    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.action.name != "Cancel") { return; }

        if (context.performed)
        {
            ReturnToShopFront();
        }
    }



    public override void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnScrollHold;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered += OnSelect;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScroll;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnScrollHold;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnCancel;
            ControlsManager.Instance.GetPlayerInput().onActionTriggered -= OnSelect;
        }
    }

    protected override void ResetData()
    {
        setPoundCount = 0;
        holdingScroll = false;


        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
    }
}
