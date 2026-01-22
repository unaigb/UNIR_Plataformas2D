using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioSource audioSourceGlobal;
    public AudioClip playSound;
    public AudioClip goSound;
    public AudioClip exitSound;
    public AudioSource backgroundMusic;
    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1.5f;
    public float musicFadeDuration = 1.5f;
    public TMP_Text loadingText;
    public GameObject optionsMenu;
    public GameObject controlsMenu;
    public GameObject mainMenu;

    private void Start()
    {
        loadingText.gameObject.SetActive(false);
    }

    public void PlayGame()
    {
        PlaySound(playSound);
        StartCoroutine(TransitionToLoadingScene());
    }

    public void OpenOptions()
    {
        PlaySound(goSound);
        optionsMenu.gameObject.SetActive(true);
        mainMenu.gameObject.SetActive(false);
    }

    public void OpenControls()
    {
        PlaySound(goSound);
        controlsMenu.gameObject.SetActive(true);
        mainMenu.gameObject.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (!audioSourceGlobal.isPlaying)
        {
            Debug.Log($"Reproduciendo sonido: {clip.name}");
            audioSourceGlobal.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("AudioSource o AudioClip no asignados.");
        }
    }

    private IEnumerator TransitionToLoadingScene()
    {
        yield return StartCoroutine(Fade(0));

        SceneManager.LoadScene("Level1");
    }

    private IEnumerator FadeOutMusic()
    {
        float startVolume = backgroundMusic.volume;

        for (float t = 0; t < musicFadeDuration; t += Time.deltaTime)
        {
            backgroundMusic.volume = Mathf.Lerp(startVolume, 0, t / musicFadeDuration);
            yield return null;
        }

        backgroundMusic.volume = 0;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvas.alpha;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            yield return null;
        }
        loadingText.gameObject.SetActive(true);
        fadeCanvas.alpha = targetAlpha;
    }

    public void ExitGame()
    {
        PlaySound(exitSound);
        Debug.Log("EXIT!"); 
        Application.Quit();
    }
}
