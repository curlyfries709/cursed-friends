using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInventorySelector 
{
    public void OnItemSelected(Item selectedItem);
    public bool CanSelectItem(Item item);
    public bool CanSwitchToAnotherInventory();
    public bool CanSwitchInventoryCategory();
    public void OnCancel();
}
