
using UnityEngine;
using DG.Tweening;

public class CinematicMusicControl : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    private void OnEnable()
    {
        CinematicManager.Instance.CinematicEnded += OnCinematicEnd;
        FadeAudio(true);
    }

    private void OnCinematicEnd()
    {
        FadeAudio(false);
    }


    private void FadeAudio(bool fadeIn)
    {
        if (fadeIn)
        {
            audioSource.volume = 0;
            audioSource.Play();
            audioSource.DOFade(1, 0.25f);
        }
        else
        {
            audioSource.DOFade(0, 0.25f).OnComplete(audioSource.Stop);
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        CinematicManager.Instance.CinematicEnded -= OnCinematicEnd;
        FadeAudio(false);
    }
}
