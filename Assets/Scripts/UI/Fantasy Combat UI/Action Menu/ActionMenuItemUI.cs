using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMenuItemUI : MonoBehaviour
{
    [SerializeField] Transform itemPosition;
    [Space(5)]
    [SerializeField] Transform controlsHeader;

    private void Awake()
    {
        ControlsManager.Instance.AddControlHeader(controlsHeader);
    }

    void Update()
    {
        SetPosition();
    }

    private void SetPosition()
    {
        Vector3 pos = Camera.main.WorldToScreenPoint(itemPosition.position);
        transform.position = new Vector3(pos.x, pos.y);
    }
}
