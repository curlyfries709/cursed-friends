using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;


public class LoadingScreen : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI loadingText;
    [SerializeField] FadeUI[] imageFaders;
    [Header("Timers")]
    [SerializeField] float textAnimLength = 0.25f;
    [SerializeField] float imageDisplayTime = 1f;

    string textPrepend = "Loading";
    string textAppend = "";

    FadeUI currentFader = null;
    int currentIndex = 0;
    bool goingBackwards = false;

    private void OnEnable()
    {
        StartCoroutine(TextRoutine());
        StartCoroutine(ImageRoutine());
    }

    IEnumerator ImageRoutine()
    {
        while (true)
        {
            List<int> validFaders = Enumerable.Range(0, imageFaders.Length).ToList();
            if (currentFader)
            {
                currentFader.Fade(false);
                validFaders.Remove(currentIndex);
            }

            int randomIndex = validFaders[Random.Range(0, validFaders.Count)];
            imageFaders[randomIndex].Fade(true);
            currentIndex = randomIndex;
            currentFader = imageFaders[randomIndex];
            yield return new WaitForSecondsRealtime(imageDisplayTime);
        }
    }

    IEnumerator TextRoutine()
    {
        int maxEllipses = 3;
        
        while (true)
        {
            loadingText.text = textPrepend + textAppend;
            yield return new WaitForSecondsRealtime(textAnimLength);
            if (textAppend.Length == maxEllipses)
            {
                goingBackwards = true;
            }
            else if (textAppend.Length == 0)
            {
                goingBackwards = false;
            }
            
            if (goingBackwards)
            {
                textAppend = textAppend.Remove(textAppend.Length - 1, 1);
            }
            else
            {
                textAppend = textAppend + ".";
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        currentFader?.gameObject.SetActive(false);
        currentFader = null;
    }
}
