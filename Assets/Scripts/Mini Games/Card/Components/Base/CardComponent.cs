using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public abstract class CardComponent : MonoBehaviour
{
    public abstract void OnHover();

    public abstract void OnSelect();

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Cursor"))
        {
            CardGameManager.Instance.OnCardComponentEnter(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Cursor"))
        {
            CardGameManager.Instance.OnCardComponentExit(this);
        }
    }


    protected void SetTooltip()
    {

    }
}
