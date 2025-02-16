using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowableTool : BaseTool, IControls
{
    [Header("Throw Data")]
    [Range(1, 60)]
    [SerializeField] float throwAngleInDegrees = 30;
    [Space(10)]
    [SerializeField] float defaultAutoTargetDistance = 10f;
    [SerializeField] float maxThrowDistance = 15f;
    [Space(10)]
    [SerializeField] float moveTargetSpeed = 0.5f;
    [Space(10)]
    [SerializeField] float aimTimeScale = 0.3f;
    [Header("GameObject")]
    [SerializeField] GameObject objectToThrow;
    [Header("Transforms")]
    [SerializeField] Transform targetPoint;
    [SerializeField] Transform rotatedForwardTransform;

    protected bool isAiming = false;
    const string myActionMap = "Aim";

    Transform throwOriginTransform;
    Transform spawnablePool;

    public override void Activate()
    {
        base.Activate();

        EnableAiming(true);

        throwOriginTransform = PlayerSpawnerManager.Instance.GetPlayerStateMachine().GetThrowOrigin();

        //Reset target Position
        ResetTargetPositionToDefault();

        ControlsManager.Instance.SubscribeToPlayerInput(myActionMap, this);
        ControlsManager.Instance.SwitchCurrentActionMap(this);
    }

    public override void Deactivate()
    {
        base.Deactivate();
        EnableAiming(false);

        ControlsManager.Instance.RemoveIControls(this);
        ControlsManager.Instance.RevertToPreviousControls();
    }

    public override void Use()
    {
        Activate();    
    }

    public override void CancelUse()
    {
        Deactivate();
    }

    protected void EnableAiming(bool enable)
    {
        isAiming = enable;
        RoamToolsManager.Instance.EnableAimTrajectoryUI(isAiming, this);
        MMTimeManager.Instance.SetTimeScaleTo(isAiming ? aimTimeScale : 1);
    }

    public override void TriggerAction()
    {
        if (!isAiming) { return; }

        Throw();
    }

    private void MoveTarget(Vector2 inputMoveVector)
    {
        Vector3 moveDir = new Vector3(inputMoveVector.x, 0, inputMoveVector.y).normalized;
        Vector3 newTargetPos = targetPoint.position + (moveDir * moveTargetSpeed);

        Vector3 origin = throwOriginTransform.position;
        Vector3 directionFromOriginToNewPos = newTargetPos - origin;

        //Clamp the distance. 
        targetPoint.position = origin + Vector3.ClampMagnitude(directionFromOriginToNewPos, maxThrowDistance);
    }

    public Vector3 GetVelocityToReachDestination()
    {
        Vector3 startPos = throwOriginTransform.position;
        Vector3 endPos = targetPoint.position;
        float gravity = Physics.gravity.y;

        Vector3 startToEnd = endPos - startPos;
        Vector3 startToEndDir = startToEnd.normalized;

        float arcParam = Mathf.Clamp(throwAngleInDegrees * Mathf.Deg2Rad, 0 , 1);
        arcParam = 1 - arcParam;

        Vector3 LaunchDir = Vector3.Lerp(Vector3.up, startToEndDir, arcParam).normalized;
        rotatedForwardTransform.forward = LaunchDir;

        float angle = rotatedForwardTransform.rotation.eulerAngles.x * Mathf.Deg2Rad;

        Vector3 startPosYZero = startPos;
        startPosYZero.y = 0;

        Vector3 endPosYZero = endPos;
        endPosYZero.y = 0;

        float Dx = Vector3.Distance(startPosYZero, endPosYZero);
        float Dy = startToEnd.y;

        //v = sqrt( g * Dx^2/ ((dx tan(angle) + Dy) * 2 * cos(angle)^2)
        float NumeratorInsideSqrt = (gravity * Mathf.Pow(Dx, 2));
        float DenominatorInsideSqrt = ((Dx * Mathf.Tan(angle) + Dy) * 2 * Mathf.Pow(Mathf.Cos(angle), 2));
        float insideSqrt = NumeratorInsideSqrt / DenominatorInsideSqrt;

        if(insideSqrt >= 0)
        {
            float speed = Mathf.Sqrt(insideSqrt);
            return LaunchDir * speed;
        }

        Debug.Log("THROW VELOCITY COULDN'T BE CALCULATED");
        return Vector3.zero;
    }

    public void Throw()
    {
        if (!isAiming) { return; }

        //Hide UI
        RoamToolsManager.Instance.EnableAimTrajectoryUI(false, this);

        //Spawn The Object to throw 
        GameObject spawnable = GetSpawnableFromPool();

        //Set Position
        spawnable.transform.position = throwOriginTransform.position;
        spawnable.transform.rotation = throwOriginTransform.rotation;

        //Get Rigidbody and throw.
        Rigidbody rigidbody = GetAndSetupRigidbody(spawnable);

        //Activate
        spawnable.SetActive(true);

        //Throw
        rigidbody.AddForce(GetVelocityToReachDestination(), ForceMode.VelocityChange);

        //Use Complete
        OnUseComplete(true);
    }

    private GameObject GetSpawnableFromPool()
    {
        if(!spawnablePool)
            spawnablePool = RoamToolsManager.Instance.GetPoolForSpawnables(GetData());

        foreach (Transform child in spawnablePool)
        {
            if (!child.gameObject.activeInHierarchy)
            {
                return child.gameObject;
            }
        }

        GameObject spawned = Instantiate(objectToThrow, spawnablePool);
        spawned.SetActive(false);

        return spawned;
    }

    private Rigidbody GetAndSetupRigidbody(GameObject rigidbodyGameObject)
    {
        Rigidbody rigidBody = rigidbodyGameObject.GetComponent<Rigidbody>();

        rigidBody.drag = 0;
        rigidBody.isKinematic = false;
        rigidBody.useGravity = true;

        return rigidBody;
    }

    private void ResetTargetPositionToDefault()
    {
        Vector3 defaultPosition = throwOriginTransform.position + (throwOriginTransform.forward * defaultAutoTargetDistance);
        targetPoint.position = defaultPosition;
    }

    public Vector3 GetTargetPosition()
    {
        return targetPoint.position;
    }

    public Transform GetThrowOrigin()
    {
        return throwOriginTransform;
    }

    //INPUT

    private void OnThrowTriggered(InputAction.CallbackContext context)
    {
        Throw();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveTarget(context.ReadValue<Vector2>());
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        CancelUse();
    }

    public void ListenToInput(bool listen)
    {
        if (listen)
        {
            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Move").performed += OnMove;
            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Move").canceled += OnMove;

            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Throw").performed += OnThrowTriggered;
            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Cancel").performed += OnCancel;
        }
        else
        {
            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Move").performed -= OnMove;
            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Move").canceled -= OnMove;

            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Throw").performed -= OnThrowTriggered;
            ControlsManager.Instance.GetPlayerInput().actions.FindAction("Cancel").performed -= OnCancel;
        }
    }
}
