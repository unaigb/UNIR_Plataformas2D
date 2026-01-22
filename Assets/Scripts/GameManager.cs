using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Configuración Juego")]
    public int vidasIniciales = 3;
    public int vidasActuales;
    public int puntos = 0;

    [Header("Referencias")]
    public UIController uiController;

    public AudioSource backgroundMusic;

    [Header("Inventario")]
    public bool tieneLlave = false;

    private bool juegoTerminado = false;
    public bool esPrimeraVez = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            vidasActuales = vidasIniciales;
            puntos = 0;

            if (backgroundMusic == null) backgroundMusic = GetComponent<AudioSource>();

            if (backgroundMusic != null && !backgroundMusic.isPlaying)
            {
                backgroundMusic.loop = true;
                backgroundMusic.Play();
            }
        }
        else
        {
            AudioSource audioImpostor = GetComponent<AudioSource>();
            if (audioImpostor != null) audioImpostor.Stop();

            Destroy(gameObject);
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (backgroundMusic != null)
        {
            if (!backgroundMusic.isPlaying) backgroundMusic.Play();
        }
    }

    public void SetUI(UIController ui)
    {
        uiController = ui;
        // Al conectar UI nueva, refrescamos los datos para que no salgan vacíos
        uiController.ActualizarVidas(vidasActuales);
        uiController.ActualizarPuntuacion(puntos);
    }

    // --- MÉTODOS DE ACCIÓN ---

    public void GanarPuntos(int cantidad)
    {
        puntos += cantidad;
        if (uiController != null) uiController.ActualizarPuntuacion(puntos);
    }

    public int GetVidasActuales()
    {
        return vidasActuales;
    }

    public void PerderVida()
    {
        if (juegoTerminado) return;
        vidasActuales--;
        if (uiController != null) uiController.ActualizarVidas(vidasActuales);

        if (vidasActuales <= 0) GameOver();
    }

    public void CurarVida()
    {
        if (vidasActuales < vidasIniciales)
        {
            vidasActuales++;
            if (uiController != null) uiController.ActualizarVidas(vidasActuales);
        }
    }

    public void AgarrarLlave()
    {
        tieneLlave = true;
        Debug.Log("¡Llave conseguida!");
    }

    public void ResetearLlave()
    {
        tieneLlave = false;
    }

    // --- ESTADOS DEL JUEGO ---

    public void GameOver()
    {
        juegoTerminado = true;

        if (uiController != null) uiController.MostrarGameOver();
    }

    public void VictoriaFinal()
    {
        juegoTerminado = true;

        Time.timeScale = 0f;

        if (uiController != null) uiController.MostrarVictoria();
        else
        {
            Debug.LogError("No hay UIController conectado al Manager");
        }
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        vidasActuales = vidasIniciales;
        puntos = 0;
        SceneManager.LoadScene(1);
    }

    public void SalirJuego()
    {
        Time.timeScale = 1f;
        Destroy(gameObject);
        SceneManager.LoadScene(0);
    }
}
