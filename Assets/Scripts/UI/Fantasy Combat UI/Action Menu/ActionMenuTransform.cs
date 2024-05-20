using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionMenuTransform : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] float horizontalOffset = 0.5f;
    [SerializeField] float minYPos = 0.5f;

    //Variables
    float camMinDistance = 5f;
    float camMaxDistance = 10f;
    float startingYPos;

    //Cache
    Transform camTransform;
    Vector3 myPosForDistance;
    Vector3 camPosForDistance;

    private void Awake()
    {
        startingYPos = transform.position.y;
        camTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
        transform.position = targetPos + (horizontalOffset * Camera.main.transform.right);
        transform.position = new Vector3(transform.position.x, GetNewYPos(), transform.position.z);
    }

    private float CalculateDistance()
    {
        myPosForDistance = new Vector3(transform.position.x, 0, transform.position.z);
        camPosForDistance = new Vector3(camTransform.position.x, 0, camTransform.position.z);
        return Vector3.Distance(myPosForDistance, camPosForDistance);
    }

    private float GetNewYPos()
    {
        float distanceFromMin = CalculateDistance() - camMinDistance;
        float growthConstant = (startingYPos - minYPos) / (camMaxDistance - camMinDistance);
        //Debug.Log("New Y Pos for " + transform.name + ": " + (startingYPos - (distanceFromMin * growthConstant)));
        return startingYPos - (distanceFromMin * growthConstant);

    }
}
