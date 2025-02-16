using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseUI : MonoBehaviour
{
    //Cache
    protected PlayerInput playerInput;

    protected virtual void Awake()
    {
        playerInput = ControlsManager.Instance.GetPlayerInput();
    }

    public virtual void OnCursorTriggerEnter(TriggerComponentUI enteredComponent) { }
    public virtual void OnCursorTriggerExit(TriggerComponentUI exitedComponent) { }

    public virtual void Activate(bool activate)
    {
        gameObject.SetActive(activate);
    }

    protected void MoveCursor(Transform cursorTransform, Vector2 moveVector)
    {
        //Move Cursor
        cursorTransform.position += new Vector3(moveVector.x, moveVector.y, 0);

        //Clamp Cursor.
        float clampedXPos = Mathf.Clamp(cursorTransform.position.x, Camera.main.ViewportToScreenPoint(Vector3.zero).x, Camera.main.ViewportToScreenPoint(Vector3.one).x);
        float clampedYPos = Mathf.Clamp(cursorTransform.position.y, Camera.main.ViewportToScreenPoint(Vector3.zero).y, Camera.main.ViewportToScreenPoint(Vector3.one).y);

        cursorTransform.position = new Vector3(clampedXPos, clampedYPos, 0);
    }

}
