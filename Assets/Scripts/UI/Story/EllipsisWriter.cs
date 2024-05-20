using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EllipsisWriter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textArea;
    [SerializeField] float waitTime = 0.25f;
    [Space(10)]
    [SerializeField] string startString = "... ";
    [SerializeField] int maxFullstopsBeforeSpace = 3;

    private void OnEnable()
    {
        StartCoroutine(TypeSentence());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator TypeSentence()
    {
        int counter = 0;
        textArea.text = startString;

        while (enabled)
        {
            textArea.text = textArea.text + ".";

            counter++;

            if(counter == maxFullstopsBeforeSpace)
            {
                textArea.text = textArea.text + " ";
                counter = 0;
            }

            yield return new WaitForSeconds(waitTime);
        }
    }
}
