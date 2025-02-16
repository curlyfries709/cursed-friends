
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TriggerComponentUI : MonoBehaviour
{
    [SerializeField] BaseUI myUIOwner;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Cursor"))
        {
            myUIOwner.OnCursorTriggerEnter(this);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.CompareTag("Cursor"))
        {
            myUIOwner.OnCursorTriggerExit(this);
        }
    }
}
