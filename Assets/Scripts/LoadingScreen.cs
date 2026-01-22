using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    public CanvasGroup fadeCanvas;
    public Canvas startCanvas;
    public float fadeDuration = 1f;
    public float loadingTime = 1.5f;
    public TMP_Text loadingText;

    public GameObject jugadorPrefab;
    public Transform spawnJugador;

    private bool isGameReady = false;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.esPrimeraVez)
            {
                startCanvas.gameObject.SetActive(false);
                loadingText.gameObject.SetActive(false);
                
                gameObject.SetActive(false);

                StartGame();

                return;
            }
            else
            {
                startCanvas.gameObject.SetActive(false);
                loadingText.gameObject.SetActive(true);
                StartCoroutine(LoadGameScene());

                GameManager.Instance.esPrimeraVez = false;
            }
        }
    }

    private IEnumerator LoadGameScene()
    {
        fadeCanvas.alpha = 1; // Pantalla completamente opaca
        yield return new WaitForSeconds(loadingTime); // Tiempo de espera para cargar
        yield return StartCoroutine(Fade(0)); // Realiza el fade-out
        isGameReady = true; // El juego está listo para iniciar
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvas.alpha;
        loadingText.gameObject.SetActive(false); // Desactiva el texto de "loading"

        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float normalizedTime = t / fadeDuration;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
        startCanvas.gameObject.SetActive(true);
    }

    void Update()
    {
        if (isGameReady)
        {
            bool teclaPulsada = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

            if (teclaPulsada)
            {
                StartGame();
            }
        }
    }

    private void StartGame()
    {
        startCanvas.gameObject.SetActive(false);

        Instantiate(jugadorPrefab, spawnJugador.position, Quaternion.identity); // Genera el jugador en la posición de spawn

        gameObject.SetActive(false); // Desactiva el objeto LoadingScreen (desaparece la pantalla de carga)
    }
}