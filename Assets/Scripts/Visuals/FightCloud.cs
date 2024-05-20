using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightCloud : MonoBehaviour
{
    [Header("Transforms")]
    [SerializeField] Transform textHeader;
    [SerializeField] Transform destinationHeader;
    [Header("Timers")]
    [SerializeField] float timeBetweenTexts = 0.5f;

    void Start()
    {
        StartCoroutine(CloudTextRoutine());
    }


    IEnumerator CloudTextRoutine()
    {
        while (enabled)
        {
            Transform chosenText = GetRandomText();
            Transform chosenDestination = GetRandomDestination();

            TextAnimation textAnimation = chosenText.GetComponent<TextAnimation>();

            textAnimation.destination = chosenDestination;
            chosenText.localRotation = chosenDestination.localRotation;

            chosenText.gameObject.SetActive(true);

            yield return new WaitForSeconds(timeBetweenTexts);
        }
    }

    private Transform GetRandomText()
    {
        int randNum = Random.Range(0, textHeader.childCount);

        while (textHeader.GetChild(randNum).gameObject.activeInHierarchy)
        {
            randNum = Random.Range(0, textHeader.childCount);
        }

        return textHeader.GetChild(randNum);
    }

    private Transform GetRandomDestination()
    {
        List<Transform> usedDestinations = new List<Transform>();

        foreach (Transform text in textHeader)
        {
            if (text.gameObject.activeInHierarchy)
            {
                usedDestinations.Add(text.GetComponent<TextAnimation>().destination);
            }
        }

        int randNum = Random.Range(0, destinationHeader.childCount);

        while (usedDestinations.Contains(destinationHeader.GetChild(randNum)))
        {
            randNum = Random.Range(0, destinationHeader.childCount);
        }

        return destinationHeader.GetChild(randNum);
    }

}
